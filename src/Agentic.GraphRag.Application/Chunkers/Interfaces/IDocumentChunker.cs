namespace Agentic.GraphRag.Application.Chunkers.Interfaces;

public interface IDocumentChunker
{
    IAsyncEnumerable<string> StreamTextChunks(
        string filePath,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<(int SectionId, string SectionText, IReadOnlyList<string> ChildChunks)> StreamSections(
        string filePath,
        CancellationToken cancellationToken = default);
}
