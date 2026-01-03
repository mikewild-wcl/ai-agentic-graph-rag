using System.Diagnostics;

namespace Agentic.GraphRag.Application.Settings;

[DebuggerDisplay($"Connection = {{{nameof(Connection)}}}, User = {{{nameof(User)},nq}}")]
public record GraphDatabaseSettings(
    Uri Connection,
    string User,
    string Password)
{
    public const string SectionName = "GraphDatabase";
    
    public const string DefaultDb = "neo4j";

    public string Provider { get; init; } = "neo4j";

    public string MoviesDb { get; init; } = DefaultDb;

    public string EinsteinVectorDb { get; init; } = DefaultDb;

    public string UfoDb { get; init; } = DefaultDb;

    public int Timeout { get; init; } = 30; // Default timeout in seconds
}