namespace Trinetra.Agents.Configuration;

public class AgentConfiguration
{
    public string ModelId { get; set; } = "claude-sonnet-4-5";
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 2000;
    public int TimeoutSeconds { get; set; } = 300;
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
}

public class AiSettings
{
    public const string SectionName = "Ai";

    public string ModelId { get; set; } = "claude-sonnet-4-5";
    public string AnthropicApiKey { get; set; } = string.Empty;
    public string EmbeddingModelId { get; set; } = "text-embedding-3-small";
    public string? Endpoint { get; set; }

    public Dictionary<string, AgentConfiguration> Agents { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public AgentConfiguration GetAgentConfig(string agentName)
    {
        if (Agents.TryGetValue(agentName, out var config))
        {
            if (string.IsNullOrWhiteSpace(config.ModelId)) config.ModelId = ModelId;
            if (string.IsNullOrWhiteSpace(config.ApiKey)) config.ApiKey = AnthropicApiKey;
            if (string.IsNullOrWhiteSpace(config.Endpoint)) config.Endpoint = Endpoint;
            return config;
        }

        return new AgentConfiguration
        {
            ModelId = ModelId,
            ApiKey = AnthropicApiKey,
            Endpoint = Endpoint
        };
    }
}
