using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Agentic.GraphRag.Shared.Configuration;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public record class AISettings(
    [Required]
    AIProvider Provider,
    string DeploymentName,
    string Model,
    int? Timeout = 120)
{
    public const string SectionName = "AI";

    public string? EmbeddingDeploymentName { get; init; }

    public string? EmbeddingModel { get; init; }

    public string? Endpoint { get; init; } //Optional endpoint to override default for the provider

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay =>
        $$"""
        Provider = {{Provider}}, 
        Deployment = {{DeploymentName}}, 
        Model = {{Model}}, 
        Embedding Model = {{EmbeddingModel}}
        """;
}
