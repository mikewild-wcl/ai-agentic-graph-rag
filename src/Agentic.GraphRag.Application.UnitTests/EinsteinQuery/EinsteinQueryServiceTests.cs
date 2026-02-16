using Agentic.GraphRag.Application.EinsteinQuery;
using Agentic.GraphRag.Application.EinsteinQuery.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Polly.Registry;

namespace Agentic.GraphRag.Application.UnitTests.EinsteinQuery;

public class EinsteinQueryServiceTests
{
    private readonly Mock<IChatClient> _mockChatClient;
    private readonly Mock<IEinsteinQueryDataAccess> _mockDataAccess;
    private readonly Mock<IEmbeddingGenerator<string, Embedding<float>>> _mockEmbeddingGenerator;
    private readonly Mock<ResiliencePipelineProvider<string>> _mockResiliencePipelineProvider;
    private readonly Shared.Configuration.AISettings _aISettings;
    private readonly Mock<ILogger<EinsteinQueryService>> _mockLogger;

    private readonly EinsteinQueryService _sut;

    public EinsteinQueryServiceTests()
    {
        _mockChatClient = new Mock<IChatClient>();
        _mockDataAccess = new Mock<IEinsteinQueryDataAccess>();
        _mockEmbeddingGenerator = new Mock<IEmbeddingGenerator<string, Embedding<float>>>();
        _mockResiliencePipelineProvider = new Mock<ResiliencePipelineProvider<string>>();
        _mockLogger = new Mock<ILogger<EinsteinQueryService>>();

        _aISettings = new Shared.Configuration.AISettings(
                Shared.Configuration.AIProvider.AzureOpenAI,
                "deploymentName",
                "modelName",
                Timeout: 60);

        _sut = new EinsteinQueryService(
            _mockChatClient.Object,
            _mockDataAccess.Object,
            _mockEmbeddingGenerator.Object,
            _mockResiliencePipelineProvider.Object,
            _aISettings,
            _mockLogger.Object);
    }

    [Fact]
    public void Constructor_Succeeds()
    {
        // Assert
        _sut.ShouldNotBeNull();
    }
}
