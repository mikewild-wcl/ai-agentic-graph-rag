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

    //TODO: Remove parameterless constructor when upgrading to .NET 10.0
    //Added as a workaround for failures when binding configuration to record types
    //https://stackoverflow.com/questions/64933022/can-i-use-c-sharp-9-records-as-ioptions
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    public AzureOpenAISettings() : this(default, default, default, default) { }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

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