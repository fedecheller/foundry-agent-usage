namespace FoundryAgentUsage;

public class FoundryAgentSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public int MaxCompletionTokens { get; set; } = 800;
}
