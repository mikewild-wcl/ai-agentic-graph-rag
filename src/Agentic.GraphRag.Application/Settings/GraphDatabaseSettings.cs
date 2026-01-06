using System.Diagnostics;

namespace Agentic.GraphRag.Application.Settings;

[DebuggerDisplay($"Connection = {{{nameof(Connection)}}}, User = {{{nameof(User)},nq}}")]
public record GraphDatabaseSettings(
    Uri Connection,
    string User,
    string Password)
{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    //Added as a workaround for failures when binding configuration to record types
    //https://stackoverflow.com/questions/64933022/can-i-use-c-sharp-9-records-as-ioptions
    public GraphDatabaseSettings() : this(default, default, default) { }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

    public const string SectionName = "GraphDatabase";
    
    public const string DefaultDb = "neo4j";

    public string Provider { get; init; } = "neo4j";

    public string MoviesDb { get; init; } = DefaultDb;

    public string EinsteinVectorDb { get; init; } = DefaultDb;

    public string UfoDb { get; init; } = DefaultDb;

    public int Timeout { get; init; } = 30; // Default timeout in seconds
}