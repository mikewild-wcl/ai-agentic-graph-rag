using Agentic.GraphRag.Application.Chunkers.Interfaces;
using Agentic.GraphRag.Application.EinsteinQuery;
using Agentic.GraphRag.Application.EinsteinQuery.Interfaces;
using Agentic.GraphRag.Application.Services.Interfaces;
using Agentic.GraphRag.Application.Settings;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Registry;

namespace Agentic.GraphRag.Application.UnitTests.EinsteinQuery;

public class EinsteinDataIngestionServiceTests
{
    private const string DOCUMENT_FILE_NAME = "Test.pdf";
    private readonly Uri DOCUMENT_URI = new("http://localhost/test");

    private readonly Mock<IEinsteinQueryDataAccess> _mockDataAccess;
    private readonly Mock<IDownloadService> _mockDownloadService;
    private readonly Mock<IDocumentChunker> _mockDocumentChunker;
    private readonly Mock<IEmbeddingGenerator<string, Embedding<float>>> _mockEmbeddingGenerator;
    private readonly Mock<ResiliencePipelineProvider<string>> _mockResiliencePipelineProvider;

    private readonly Mock<ILogger<EinsteinDataIngestionService>> _mockLogger;

    private readonly EinsteinDataIngestionService _sut;

    public EinsteinDataIngestionServiceTests()
    {
        _mockDataAccess = new Mock<IEinsteinQueryDataAccess>();
        _mockDownloadService = new Mock<IDownloadService>();
        _mockDocumentChunker = new Mock<IDocumentChunker>();
        _mockEmbeddingGenerator = new Mock<IEmbeddingGenerator<string, Embedding<float>>>();
        _mockResiliencePipelineProvider = new Mock<ResiliencePipelineProvider<string>>();

        IOptions<EinsteinQuerySettings> options = new OptionsWrapper<EinsteinQuerySettings>(
            new EinsteinQuerySettings
            {
                DocumentUri = DOCUMENT_URI,
                DocumentFileName = DOCUMENT_FILE_NAME
            });

        _mockLogger = new Mock<ILogger<EinsteinDataIngestionService>>();

        _sut = new EinsteinDataIngestionService(
            _mockDataAccess.Object,
            _mockDownloadService.Object,
            _mockDocumentChunker.Object,
            _mockEmbeddingGenerator.Object,
            _mockResiliencePipelineProvider.Object,
            options,
            _mockLogger.Object);
    }

    [Fact]
    public void Constructor_Succeeds()
    {
        // Assert
        _sut.Should().NotBeNull();
    }
}
