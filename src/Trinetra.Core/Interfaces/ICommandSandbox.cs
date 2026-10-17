using System;
using System.Threading;
using System.Threading.Tasks;

namespace Trinetra.Core.Interfaces;

public class CommandOutput
{
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public bool TimedOut { get; set; }
    public bool IsSuccess => ExitCode == 0 && !TimedOut;
    public string? ErrorMessage { get; set; }
}

public interface ICommandSandbox
{
    Task<CommandOutput> ExecuteCommandAsync(
        string command,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
