using System.Diagnostics;

namespace Agentic.GraphRag.Application.Settings;

[DebuggerDisplay($"DownloadDirectory = {{{nameof(DownloadDirectory)}}}, Timeout = {{{nameof(Timeout)},d}}")]
public record DownloadSettings(
    string DownloadDirectory = "downloads")
{
    public const string SectionName = "Download";

    public int Timeout { get; init; } = 300; // Default timeout in seconds
}