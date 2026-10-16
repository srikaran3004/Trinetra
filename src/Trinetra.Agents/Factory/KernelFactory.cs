using Microsoft.SemanticKernel;
using Trinetra.Agents.Configuration;

namespace Trinetra.Agents.Factory;

public interface IKernelFactory
{
    Kernel CreateKernel(AgentConfiguration config, Action<IKernelBuilder>? configureBuilder = null);
}

public class SemanticKernelFactory : IKernelFactory
{
    public Kernel CreateKernel(AgentConfiguration config, Action<IKernelBuilder>? configureBuilder = null)
    {
        var builder = Kernel.CreateBuilder();

        // Allow caller to inject chat completions or plugins
        configureBuilder?.Invoke(builder);

        return builder.Build();
    }
}
