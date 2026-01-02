using Microsoft.Extensions.Logging;

namespace Agentic.GraphRag.Application.EinsteinQuery;

// See https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging
internal static class EinsteinLoggingExtensions
{
    public static void ChunksAndEmbeddingsSaved(this ILogger logger, int chunkCount, int mbeddingCount) =>
        _logChunksAndEmbeddingsSaved(logger, chunkCount, mbeddingCount, default!);

    public static void ParentChildChunksAndEmbeddingsSaved(this ILogger logger, string pdfId, int parentChunkId, int chunkCount, int mbeddingCount) =>
        _logParentChildChunksAndEmbeddingsSaved(logger, pdfId, parentChunkId, chunkCount, mbeddingCount, default!);

    public static void FullTextIndexCreated(this ILogger logger, string indexName) =>
        _logFullTextIndexCreated(logger, indexName, default!);

    public static void FullTextIndexCreationFailed(this ILogger logger, Exception ex, string indexName) =>
      _logFullTextIndexCreationException(logger, indexName, ex);

    public static void VectorIndexCreated(this ILogger logger, string indexName) =>
          _logVectorIndexCreated(logger, indexName, default!);

    public static void VectorIndexCreationFailed(this ILogger logger, Exception ex, string indexName) =>
        _logVectorIndexCreationException(logger, indexName, ex);
        
    public static void NoChunksOrEmbeddingsToSave(this ILogger logger) =>
        _logNoChunksOrEmbeddingsToSave(logger, default!);

    public static void NoParentChildChunksOrEmbeddingsToSave(this ILogger logger) =>
        _logNoParentChildChunksOrEmbeddingsToSave(logger, default!);

    public static void QuerySimilarRecordsFailed(this ILogger logger, Exception ex) =>
        _logQuerySimilarRecordsException(logger, ex);

    public static void ExistingDataAndIndexesRemoved(this ILogger logger) =>
        _logExistingDataAndIndexesRemoved(logger, default!);

    public static void RemovingExistingDataAndIndexesFailed(this ILogger logger, Exception ex) =>
        _logRemovingExistingDataAndIndexesException(logger, ex);

    public static void SavingChunksOrEmbeddingsFailed(this ILogger logger, Exception ex) =>
        _logSavingChunksOrEmbeddingsException(logger, ex);

    public static void SavingParentChildChunksOrEmbeddingsFailed(this ILogger logger, Exception ex) =>
        _logSavingParentChildChunksOrEmbeddingsException(logger, ex);

    private static readonly Action<ILogger, Exception> _logExistingDataAndIndexesRemoved =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1, nameof(FullTextIndexCreated)),
            "Removed existing data and indexes from database.");

    private static readonly Action<ILogger, string, Exception> _logFullTextIndexCreated =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, nameof(FullTextIndexCreated)),
            "Created full-text index '{IndexName}'.");

    private static readonly Action<ILogger, string, Exception?> _logFullTextIndexCreationException =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(0, nameof(FullTextIndexCreationFailed)),
            "Failed to create full-text index '{IndexName}'.");

    private static readonly Action<ILogger, Exception> _logNoChunksOrEmbeddingsToSave =
       LoggerMessage.Define(
           LogLevel.Warning,
           new EventId(1, nameof(NoChunksOrEmbeddingsToSave)),
           "No chunks or embeddings to save.");

    private static readonly Action<ILogger, Exception> _logNoParentChildChunksOrEmbeddingsToSave =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1, nameof(NoParentChildChunksOrEmbeddingsToSave)),
            "No parent/child chunks or embeddings to save.");

    private static readonly Action<ILogger, string, Exception> _logVectorIndexCreated =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, nameof(VectorIndexCreated)),
            "Created vector index '{IndexName}'.");

    private static readonly Action<ILogger, string, Exception?> _logVectorIndexCreationException =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(0, nameof(VectorIndexCreated)),
            "Failed to create vector index '{IndexName}'.");

    private static readonly Action<ILogger, Exception?> _logQuerySimilarRecordsException =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(0, nameof(QuerySimilarRecordsFailed)),
            "Failed when querying similar records.");

    private static readonly Action<ILogger, Exception?> _logRemovingExistingDataAndIndexesException =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(0, nameof(RemovingExistingDataAndIndexesFailed)),
            "Error removing existing data and indexes from database.");

    private static readonly Action<ILogger, int, int, Exception> _logChunksAndEmbeddingsSaved =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(1, nameof(ChunksAndEmbeddingsSaved)),
            "Saved {ChunkCount} text chunks and {EmbeddingCount} embeddings.");

    private static readonly Action<ILogger, string, int, int, int, Exception> _logParentChildChunksAndEmbeddingsSaved =
        LoggerMessage.Define<string, int, int, int>(
            LogLevel.Information,
            new EventId(1, nameof(ChunksAndEmbeddingsSaved)),
            "Saved {ChunkCount} text chunks and {EmbeddingCount} embeddings for pdf id '{ParentId}' chunk {ParentChunkId}.");

    private static readonly Action<ILogger, Exception?> _logSavingChunksOrEmbeddingsException =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(0, nameof(RemovingExistingDataAndIndexesFailed)),
            "Error while saving text chunks and embeddings.");

    private static readonly Action<ILogger, Exception?> _logSavingParentChildChunksOrEmbeddingsException =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(0, nameof(RemovingExistingDataAndIndexesFailed)),
            "Error while saving parent/child text chunks and embeddings.");    
}
