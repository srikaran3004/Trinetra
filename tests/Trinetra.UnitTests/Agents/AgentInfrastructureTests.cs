using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;
using Trinetra.Agents;
using Trinetra.Agents.Configuration;
using Trinetra.Agents.Factory;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Xunit;

namespace Trinetra.UnitTests.Agents;

public class TestAgent : AgentBase
{
    public TestAgent(
        AgentConfiguration configuration,
        Kernel kernel,
        ILogger logger,
        IAuditEventWriter? auditWriter = null)
        : base("TestAgent", configuration, kernel, logger, auditWriter)
    {
    }
}

public class SamplePlugin
{
    [KernelFunction("sample_function")]
    public string SampleFunction(string input) => $"Processed: {input}";
}

public class AgentInfrastructureTests
{
    [Fact]
    public void AiSettings_ShouldLoadConfiguration_AndReturnAgentConfig()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Ai:ModelId", "claude-sonnet-4-5"},
            {"Ai:AnthropicApiKey", "test-api-key"},
            {"Ai:EmbeddingModelId", "text-embedding-3-small"},
            {"Ai:Agents:IncidentCoordinator:ModelId", "claude-sonnet-4-5"},
            {"Ai:Agents:IncidentCoordinator:Temperature", "0.3"},
            {"Ai:Agents:IncidentCoordinator:MaxTokens", "2000"},
            {"Ai:Agents:IncidentCoordinator:TimeoutSeconds", "300"},
            {"Ai:Agents:LogAnalyzer:ModelId", "claude-sonnet-4-5"},
            {"Ai:Agents:LogAnalyzer:Temperature", "0.2"},
            {"Ai:Agents:LogAnalyzer:MaxTokens", "3000"},
            {"Ai:Agents:LogAnalyzer:TimeoutSeconds", "120"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var aiSettings = new AiSettings();
        configuration.GetSection(AiSettings.SectionName).Bind(aiSettings);

        aiSettings.ModelId.Should().Be("claude-sonnet-4-5");
        aiSettings.AnthropicApiKey.Should().Be("test-api-key");

        var coordConfig = aiSettings.GetAgentConfig("IncidentCoordinator");
        coordConfig.Should().NotBeNull();
        coordConfig.ModelId.Should().Be("claude-sonnet-4-5");
        coordConfig.Temperature.Should().Be(0.3);
        coordConfig.MaxTokens.Should().Be(2000);
        coordConfig.TimeoutSeconds.Should().Be(300);

        var logConfig = aiSettings.GetAgentConfig("LogAnalyzer");
        logConfig.Should().NotBeNull();
        logConfig.Temperature.Should().Be(0.2);
        logConfig.MaxTokens.Should().Be(3000);

        // Fallback for unregistered agent inherits global Ai settings
        var unknownConfig = aiSettings.GetAgentConfig("UnknownAgent");
        unknownConfig.Should().NotBeNull();
        unknownConfig.ModelId.Should().Be("claude-sonnet-4-5");
        unknownConfig.ApiKey.Should().Be("test-api-key");
    }

    [Fact]
    public void SemanticKernelFactory_ShouldCreateKernel_Successfully()
    {
        var factory = new SemanticKernelFactory();
        var config = new AgentConfiguration
        {
            ModelId = "claude-sonnet-4-5",
            Temperature = 0.2
        };

        var kernel = factory.CreateKernel(config);
        kernel.Should().NotBeNull();
        kernel.Plugins.Should().NotBeNull();
    }

    [Fact]
    public void AgentBase_RegisterPlugin_ShouldAddPluginToKernel()
    {
        var factory = new SemanticKernelFactory();
        var config = new AgentConfiguration();
        var kernel = factory.CreateKernel(config);
        var mockLogger = new Mock<ILogger>();

        var agent = new TestAgent(config, kernel, mockLogger.Object);
        var plugin = new SamplePlugin();

        agent.RegisterPlugin(plugin, "SamplePlugin");

        agent.Kernel.Plugins.Should().Contain(p => p.Name == "SamplePlugin");
        agent.Kernel.Plugins["SamplePlugin"].Should().Contain(f => f.Name == "sample_function");
    }

    [Fact]
    public async Task AgentBase_RecordAuditEventAsync_ShouldWriteEvent_WithCorrectProperties()
    {
        var factory = new SemanticKernelFactory();
        var config = new AgentConfiguration();
        var kernel = factory.CreateKernel(config);
        var mockLogger = new Mock<ILogger>();
        var mockAuditWriter = new Mock<IAuditEventWriter>();

        IncidentEvent? capturedEvent = null;
        mockAuditWriter
            .Setup(w => w.WriteEventAsync(It.IsAny<IncidentEvent>(), It.IsAny<CancellationToken>()))
            .Callback<IncidentEvent, CancellationToken>((ev, ct) => capturedEvent = ev)
            .ReturnsAsync((IncidentEvent ev, CancellationToken ct) => ev);

        var agent = new TestAgent(config, kernel, mockLogger.Object, mockAuditWriter.Object);
        var incidentId = Guid.NewGuid();

        var result = await agent.RecordAuditEventAsync(
            incidentId: incidentId,
            eventType: "investigation_started",
            actionDescription: "Starting parallel analysis",
            toolCall: "query_logs",
            toolParams: new { service = "auth-api", limit = 100 },
            toolResult: "Found 5 error logs",
            executionStatus: "success",
            executionTimeMs: 145);

        result.Should().NotBeNull();
        capturedEvent.Should().NotBeNull();
        capturedEvent!.IncidentId.Should().Be(incidentId);
        capturedEvent.AgentName.Should().Be("TestAgent");
        capturedEvent.EventType.Should().Be("investigation_started");
        capturedEvent.ActionDescription.Should().Be("Starting parallel analysis");
        capturedEvent.ToolCall.Should().Be("query_logs");
        capturedEvent.ToolParams.Should().Contain("auth-api");
        capturedEvent.ToolResult.Should().Be("Found 5 error logs");
        capturedEvent.ExecutionStatus.Should().Be("success");
        capturedEvent.ExecutionTimeMs.Should().Be(145);
        capturedEvent.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
