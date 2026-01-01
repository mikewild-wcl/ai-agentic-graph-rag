using Agentic.GraphRag.Application.Data;
using Agentic.GraphRag.Application.EinsteinQuery.Interfaces;
using Agentic.GraphRag.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using System.Globalization;

namespace Agentic.GraphRag.Application.EinsteinQuery;

public class EinsteinDataAccess : Neo4jDataAccess, IEinsteinQueryDataAccess
{
    private readonly ILogger<EinsteinDataAccess> _logger;

#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable IDE0290 // Use primary constructor - disabled because of code passing options base class constructor call
    public EinsteinDataAccess(
        IDriver driver,
        IOptions<GraphDatabaseSettings> options,
        ILogger<EinsteinDataAccess> logger)
        : base(
            driver,
            options?.Value?.EinsteinVectorDb ?? GraphDatabaseSettings.DefaultDb,
            logger)
    {
        _logger = logger;
    }
#pragma warning restore IDE0290 // Use primary constructor

    public async Task CreateFullTextIndexIfNotExists()
    {
        const string indexName = "index_ftPdfChunk";

        try
        {
            await ExecuteWriteTransactionAsync(
                $"""
                CREATE FULLTEXT INDEX {indexName} IF NOT EXISTS
                FOR (c:Chunk) 
                ON EACH [c.text]
                """).ConfigureAwait(false);

            _logger.FullTextIndexCreated(indexName);
        }
        catch (Exception ex)
        {
            _logger.FullTextIndexCreationFailed(ex, indexName);
        }
    }

    public async Task CreateChunkVectorIndexIfNotExists()
    {
        const string indexName = "index_pdfChunk";

        try
        {
            await ExecuteWriteTransactionAsync(
                $"""
                CREATE VECTOR INDEX {indexName} IF NOT EXISTS 
                FOR (c:Chunk)
                ON c.embedding
                """).ConfigureAwait(false);

            _logger.VectorIndexCreated(indexName);
        }
        catch (Exception ex)
        {
            _logger.VectorIndexCreationFailed(ex, indexName);
        }
    }

    public async Task CreateChildVectorIndexIfNotExists()
    {
        const string indexName = "index_parent";

        try
        {
            await ExecuteWriteTransactionAsync(
                $"""
                CREATE VECTOR INDEX {indexName} IF NOT EXISTS
                FOR (c:Child)
                ON c.embedding
                """).ConfigureAwait(false);

            _logger.VectorIndexCreated(indexName);
        }
        catch (Exception ex)
        {
            _logger.VectorIndexCreationFailed(ex, indexName);
        }
    }

    public async Task<IList<RankedSearchResult>> QuerySimilarRecords(ReadOnlyMemory<float> queryEmbedding, int k = 3)
    {
        List<RankedSearchResult> rankedResults = [];

        try
        {
            var results = await ExecuteReadDictionaryAsync(
                """
                CALL db.index.vector.queryNodes('index_pdfChunk', $k, $question_embedding) 
                YIELD node AS hits, score
                RETURN hits{ text: hits.text, score, index: hits.index } AS rankedResult
                ORDER BY score DESC
                """,
                "rankedResult",
                new Dictionary<string, object>
                {
                    { "k", 2 }, // k as in in KNN - number of nearest neighbors
                    { "question_embedding", queryEmbedding.ToArray() }
                })
                .ConfigureAwait(false);

            rankedResults.AddRange(results.Select(x =>
                new RankedSearchResult(
                    x["text"] as string ?? string.Empty,
                    Convert.ToDouble(x["score"], CultureInfo.InvariantCulture),
                    Convert.ToInt32(x["index"], CultureInfo.InvariantCulture)
                )));
        }
        catch (Exception ex)
        {
            _logger.QuerySimilarRecordsFailed(ex);
        }

        return rankedResults;
    }

    public async Task RemoveExistingData()
    {
        try
        {
            await ExecuteWriteTransactionAsync("DROP INDEX index_ftPdfChunk IF EXISTS;").ConfigureAwait(false);
            await ExecuteWriteTransactionAsync("DROP INDEX index_pdfChunk IF EXISTS;").ConfigureAwait(false);
            await ExecuteWriteTransactionAsync("DROP INDEX index_parent IF EXISTS; ").ConfigureAwait(false);

            await ExecuteWriteTransactionAsync("MATCH (c:Chunk) DETACH DELETE c;").ConfigureAwait(false);
            await ExecuteWriteTransactionAsync("MATCH (c:Child) DETACH DELETE c;").ConfigureAwait(false);
            await ExecuteWriteTransactionAsync("MATCH (p:Parent) DETACH DELETE p;").ConfigureAwait(false);

            _logger.ExistingDataAndIndexesRemoved();
        }
        catch (Exception ex)
        {
            _logger.RemovingExistingDataAndIndexesFailed(ex);
        }
    }

    public async Task SaveTextChunks(IReadOnlyList<string> chunks, IReadOnlyList<ReadOnlyMemory<float>> embeddings, int startIndex = 0)
    {
        if (chunks is null || embeddings is null || chunks.Count != embeddings.Count)
        {
            throw new ArgumentException("Chunks and embeddings must be non-null and have the same count.");
        }

        if (chunks.Count == 0 || embeddings.Count == 0)
        {
            _logger.NoChunksOrEmbeddingsToSave();
            return;
        }

        try
        {
            await ExecuteWriteTransactionAsync(
                """
                WITH $chunks as chunks, range(0, size($chunks) - 1) AS index
                UNWIND index AS i
                WITH i, chunks[i] AS chunk, $embeddings[i] AS embedding
                MERGE (c:Chunk {index: i + $startIndex})
                SET c.text = chunk, c.embedding = embedding
                """,
                new Dictionary<string, object>
                {
                    { "chunks", chunks },
                    { "embeddings", embeddings.Select(e => e.ToArray()).ToList() },
                    { "startIndex", startIndex }
                })
                .ConfigureAwait(false);

            _logger.ChunksAndEmbeddingsSaved(chunks.Count, embeddings.Count);
        }
        catch (Exception ex)
        {
            _logger.SavingChunksOrEmbeddingsFailed(ex);
        }
    }

    public async Task SaveParentAndChildChunks(IReadOnlyList<string> chunks, IReadOnlyList<ReadOnlyMemory<float>> embeddings, int startIndex = 0)
    {
        if (chunks is null || embeddings is null || chunks.Count != embeddings.Count * 2)
        {
            throw new ArgumentException("Chunks and embeddings must be non-null and have the correct counts.");
        }
        if (chunks.Count == 0 || embeddings.Count == 0)
        {
            _logger.NoParentChildChunksOrEmbeddingsToSave();
        }

        try
        {
            /*
            MERGE (pdf:PDF {id:$pdf_id})
            MERGE (p:Parent {id:$pdf_id + '-' + $id})
            SET p.text = $parent
            MERGE (pdf)-[:HAS_PARENT]->(p)
            WITH p, $children AS children, $embeddings as embeddings
            UNWIND range(0, size(children) - 1) AS child_index
            MERGE (c:Child {id: $pdf_id + '-' + $id + '-' + toString(child_index)})
            SET c.text = children[child_index], c.embedding = embeddings[child_index]
            MERGE (p)-[:HAS_CHILD]->(c);     */

            //TODO: Add a logger method that includes the correct counts
            _logger.ChunksAndEmbeddingsSaved(chunks.Count, embeddings.Count);
        }
        catch (Exception ex)
        {
            _logger.SavingChunksOrEmbeddingsFailed(ex);
        }
    }
#pragma warning restore CA1031 // Do not catch general exception types
}
