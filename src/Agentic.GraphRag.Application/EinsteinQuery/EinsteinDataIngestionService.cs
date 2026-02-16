using Agentic.GraphRag.Application.Chunkers.Interfaces;
using Agentic.GraphRag.Application.EinsteinQuery.Interfaces;
using Agentic.GraphRag.Application.Extensions;
using Agentic.GraphRag.Application.Services.Interfaces;
using Agentic.GraphRag.Application.Settings;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Registry;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Agentic.GraphRag.Application.EinsteinQuery;

public sealed partial class EinsteinDataIngestionService(
    IEinsteinQueryDataAccess dataAccess,
    IDownloadService downloadService,
    IDocumentChunker documentChunker,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    ResiliencePipelineProvider<string> resiliencePipelineProvider,
    IOptions<EinsteinQuerySettings> queryOptions,
    ILogger<EinsteinDataIngestionService> logger) : IEinsteinDataIngestionService
{
    private const int BatchSize = 10;

    private readonly IEinsteinQueryDataAccess _dataAccess = dataAccess;
    private readonly IDownloadService _downloadService = downloadService;
    private readonly IDocumentChunker _documentChunker = documentChunker;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator = embeddingGenerator;
    private readonly EinsteinQuerySettings _querySettings = queryOptions.Value;
    private readonly ILogger<EinsteinDataIngestionService> _logger = logger;
    private readonly ResiliencePipelineProvider<string> _resiliencePipelineProvider = resiliencePipelineProvider;

    [GeneratedRegex(@"(?<=/)([^/]+)(?=\\.pdf(?:\\?|$))")]
    private static partial Regex ExtractPdfIdRegex();

    private static readonly Action<ILogger, string, Exception?> _fileNotFoundLog =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, nameof(LoadData)),
            "The file {File} was not found in the downloads directory");

    private static readonly Action<ILogger, string, Exception?> _logLoadDataCalled =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, nameof(LoadData)),
            "LoadData called: {Message}");

    private static readonly Action<ILogger, string, string, Exception?> _logLoadDataComplete =
    LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(1, nameof(LoadData)),
        "File load complete for uri {Uri} file {FileName}");

    public async Task LoadData(CancellationToken cancellationToken = default)
    {
        //TODO: Move data load to EinsteinDataIngestionService to handle this workflow

        _logLoadDataCalled(_logger, "LoadData called", null);

        await _downloadService.DownloadFileIfNotExists(_querySettings.DocumentUri, _querySettings.DocumentFileName, cancellationToken).ConfigureAwait(false);

        if (!_downloadService.TryGetDownloadedFilePath(_querySettings.DocumentFileName, out var filePath) || filePath is null)
        {
            _fileNotFoundLog(_logger, _querySettings.DocumentFileName, null);
            return;
        }

        await PrepareDatabase().ConfigureAwait(false);

        await ExtractAndSaveTextChunks(filePath, cancellationToken).ConfigureAwait(false);
        await ExtractAndSaveParentAndChildChunks(filePath, cancellationToken).ConfigureAwait(false);

        await _dataAccess.CreateFullTextIndexIfNotExists().ConfigureAwait(false);

        _logLoadDataComplete(_logger, _querySettings.DocumentUri.ToString(), _querySettings.DocumentFileName, null);
    }

    private void LogEmbedding(ReadOnlyMemory<float> embedding)
    {
#pragma warning disable CA1848 // Use the LoggerMessage delegates - can remove this when all logging is moved to delegates
        if (embedding.Length > 0)
        {
            var arr = embedding.ToArray();
            _logger.LogInformation("Embedding array length: {Length}", arr.Length);
            var embeddingString = new StringBuilder("[");
            for (int i = 0; i < Math.Min(arr.Length, 8); i++)
            {
                embeddingString.Append(CultureInfo.InvariantCulture, $"{arr[i]}");
            }
            embeddingString.Append($" ... ]");
            _logger.LogInformation("Embedding {EmbeddingString}", embeddingString);
        }
#pragma warning restore CA1848 // Use the LoggerMessage delegates
    }

    private async Task PrepareDatabase()
    {
        await _dataAccess.RemoveExistingData().ConfigureAwait(false);
        await _dataAccess.CreateChunkVectorIndexIfNotExists().ConfigureAwait(false);
        await _dataAccess.CreateParentChildVectorIndexIfNotExists().ConfigureAwait(false);
    }

    private async Task ExtractAndSaveTextChunks(string filePath, CancellationToken cancellationToken)
    {
        var chunks = new List<string>();
        var embeddings = new List<ReadOnlyMemory<float>>();

        var batchIndex = 0;
        var batch = new List<(string Chunks, ReadOnlyMemory<float> Embeddings)>(BatchSize);

        var resiliencePipeline = _resiliencePipelineProvider.GetPipeline(ResiliencePipelineNames.RateLimitHitRetry);

        await foreach (var chunk in _documentChunker.StreamTextChunks(filePath, cancellationToken).ConfigureAwait(true))
        {
            if (string.IsNullOrWhiteSpace(chunk))
            {
                continue;
            }

#pragma warning disable CA1848 // Use the LoggerMessage delegates - can remove this when all logging is moved to delegates
            _logger.LogInformation("Chunk: {Chunk}", chunk);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            
            var embedding = await resiliencePipeline.GetTextEmbedding(
                chunk, 
                _embeddingGenerator, 
                cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                LogEmbedding(embedding);
            }

            chunks.Add(chunk);
            embeddings.Add(embedding);
            batch.Add((chunk, embedding));
            if (batch.Count >= BatchSize)
            {
                await _dataAccess.SaveTextChunks(
                    [.. batch.Select(x => x.Chunks)],
                    [.. batch.Select(x => x.Embeddings)],
                    batchIndex)
                    .ConfigureAwait(false);

                batchIndex += batch.Count;
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await _dataAccess.SaveTextChunks(
                [.. batch.Select(x => x.Chunks)],
                [.. batch.Select(x => x.Embeddings)],
                batchIndex)
                .ConfigureAwait(false);
        }
    }

    private async Task ExtractAndSaveParentAndChildChunks(string filePath, CancellationToken cancellationToken)
    {
        var pdfIdPattern = ExtractPdfIdRegex();
        var pdfId = pdfIdPattern.Matches(filePath).FirstOrDefault()?.ToString() ?? "unknown";

        var resiliencePipeline = _resiliencePipelineProvider.GetPipeline(ResiliencePipelineNames.RateLimitHitRetry);

        await foreach (var (sectionId, sectionText, childChunks) in _documentChunker.StreamSections(filePath, cancellationToken).ConfigureAwait(true))
        {
            if (string.IsNullOrWhiteSpace(sectionText))
            {
                continue;
            }

            var embeddings = new List<ReadOnlyMemory<float>>();
            foreach (var chunk in childChunks)
            {
                var embedding = await resiliencePipeline.GetTextEmbedding(
                    chunk,
                    _embeddingGenerator,
                    cancellationToken).ConfigureAwait(false);

                embeddings.Add(embedding);
            }

            await _dataAccess.SaveParentAndChildChunks(
                pdfId,
                sectionId,
                sectionText,
                childChunks,
                embeddings)
                .ConfigureAwait(false);
        }
    }
}
