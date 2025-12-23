using System.Diagnostics;

namespace Agentic.GraphRag.Shared.Configuration;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public record class AISettings(
    AIProvider Provider,
    string DeploymentName,
    string Model,
    string EmbeddingDeploymentName,
    string EmbeddingModel,
    int? Timeout = 120)
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay =>
        $$"""
        Deployment = {{DeploymentName}}, 
        Embedding Deployment = {{EmbeddingDeploymentName}}
        """;
}

