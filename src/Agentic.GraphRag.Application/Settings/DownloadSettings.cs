using System.Diagnostics;

namespace Agentic.GraphRag.Application.Settings;

[DebuggerDisplay($"DownloadDirectory = {{{nameof(DownloadDirectory)}}}, Timeout = {{{nameof(Timeout)},d}}")]
public record DownloadSettings(
    string DownloadDirectory)
{
    public const string SectionName = "Download";

    //TODO: Remove parameterless constructor when upgrading to .NET 10.0
    //Added as a workaround for failures when binding configuration to record types
    //https://stackoverflow.com/questions/64933022/can-i-use-c-sharp-9-records-as-ioptions
    public DownloadSettings() : this("downloads") { }

    public int Timeout { get; init; } = 300; // Default timeout in seconds
}