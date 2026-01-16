using Agentic.GraphRag.Application.Data.Interfaces;

namespace Agentic.GraphRag.Application.EinsteinQuery.Interfaces;

public interface IEinsteinQueryDataAccess : INeo4jDataAccess
{
    Task CreateFullTextIndexIfNotExists();

    Task CreateParentChildVectorIndexIfNotExists();

    Task CreateChunkVectorIndexIfNotExists();

    Task<IList<RankedSearchResult>> QueryParentsAndChildren(ReadOnlyMemory<float> queryEmbedding, int k = 3);

    Task<IList<RankedSearchResult>> QuerySimilarRecords(ReadOnlyMemory<float> queryEmbedding, int k = 3);

    Task SaveTextChunks(IReadOnlyList<string> chunks, IReadOnlyList<ReadOnlyMemory<float>> embeddings, int startIndex = 0);

    Task SaveParentAndChildChunks(string pdfId, int parentChunkId, string parentChunk, IReadOnlyList<string> childChunks, IReadOnlyList<ReadOnlyMemory<float>> embeddings);

    Task RemoveExistingData();
}
