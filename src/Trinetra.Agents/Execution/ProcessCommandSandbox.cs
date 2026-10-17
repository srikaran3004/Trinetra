using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Trinetra.Core.Interfaces;

namespace Trinetra.Agents.Execution;

public class ProcessCommandSandbox : ICommandSandbox
{
    private readonly ILogger<ProcessCommandSandbox> _logger;

    public ProcessCommandSandbox(ILogger<ProcessCommandSandbox> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CommandOutput> ExecuteCommandAsync(
        string command,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return new CommandOutput
            {
                ExitCode = -1,
                ErrorMessage = "Command was null or empty."
            };
        }

        var sw = Stopwatch.StartNew();
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var fileName = isWindows ? "cmd.exe" : "/bin/sh";
        var arguments = isWindows ? $"/c \"{command}\"" : $"-c \"{command}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) stdoutBuilder.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) stderrBuilder.AppendLine(e.Data);
        };

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(timeout);

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);

            sw.Stop();
            return new CommandOutput
            {
                ExitCode = process.ExitCode,
                StandardOutput = stdoutBuilder.ToString().TrimEnd(),
                StandardError = stderrBuilder.ToString().TrimEnd(),
                DurationMs = (int)sw.ElapsedMilliseconds,
                TimedOut = false
            };
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            var isTimeout = !cancellationToken.IsCancellationRequested && linkedCts.IsCancellationRequested;

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to kill timed-out/canceled process");
            }

            return new CommandOutput
            {
                ExitCode = -1,
                StandardOutput = stdoutBuilder.ToString().TrimEnd(),
                StandardError = stderrBuilder.ToString().TrimEnd(),
                DurationMs = (int)sw.ElapsedMilliseconds,
                TimedOut = isTimeout,
                ErrorMessage = isTimeout ? $"Command timed out after {timeout.TotalSeconds:F0}s" : "Command execution canceled."
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Exception running command in sandbox: {Command}", command);

            return new CommandOutput
            {
                ExitCode = -1,
                StandardOutput = stdoutBuilder.ToString().TrimEnd(),
                StandardError = stderrBuilder.ToString().TrimEnd(),
                DurationMs = (int)sw.ElapsedMilliseconds,
                TimedOut = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
