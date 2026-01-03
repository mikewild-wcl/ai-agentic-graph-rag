using System.Diagnostics;

namespace Agentic.GraphRag.Application.Settings;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public record AzureOpenAISettings(
    string ApiKey,
    string Endpoint,
    string DeploymentName,
    string EmbeddingDeploymentName)
{
    public const string SectionName = "AzureOpenAI";

    public string? ModelId { get; init; }

    public string? EmbeddingModelId { get; init; }

    public int Timeout { get; init; } = 30; // Default timeout in seconds

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay =>
        $$"""
        Endpoint = {{{Endpoint}}}, 
        Deployment = {{DeploymentName}}, 
        Embedding Deployment = {{EmbeddingDeploymentName}}
        """;
}