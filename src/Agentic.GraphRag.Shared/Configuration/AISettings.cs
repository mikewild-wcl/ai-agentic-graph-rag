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

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    //Added as a workaround for failures when binding configuration to record types
    //https://stackoverflow.com/questions/64933022/can-i-use-c-sharp-9-records-as-ioptions
    public AISettings() : this(default, default, default) { }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

    public string? EmbeddingDeploymentName { get; init; }

    public string? EmbeddingModel { get; init; }

    public string? Endpoint { get; init; } //Optional endpoint to override default for the provider

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay =>
        $$"""
        Provider = {{Provider}}, 
        Deployment = {{DeploymentName}}, 
        Model = {{Model}}, 
        Embedding Deployment = {{EmbeddingDeploymentName}},
        Embedding Model = {{EmbeddingModel}}
        """;
}
