using Agentic.GraphRag.Application.Movies;
using Agentic.GraphRag.Application.Movies.Interfaces;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Agentic.GraphRag.Application.UnitTests.Movies;

public class MoviesQueryServiceTests
{
    private readonly Mock<IMoviesDataAccess> _mockDataAccess;
    private readonly Mock<ILogger<MoviesQueryService>> _mockLogger;

    private readonly MoviesQueryService _sut;

    public MoviesQueryServiceTests()
    {
        _mockDataAccess = new Mock<IMoviesDataAccess>();
        _mockLogger = new Mock<ILogger<MoviesQueryService>>();

        _sut = new MoviesQueryService(
            _mockDataAccess.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetMoviesForActor_ReturnsMovieTitles()
    {
        // Arrange
        var actorName = "Tom Hanks";
        var expectedTitles = new List<string> { "Forrest Gump", "Cast Away" };

        _mockDataAccess.Setup(x => x.ExecuteReadListAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>?>()))
            .ReturnsAsync(expectedTitles);        

        // Act
        var result = await _sut.GetMoviesForActor(actorName, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEquivalentTo(expectedTitles);
    }

    [Fact]
    public async Task GetMoviesForActor_ReturnsEmptyMovieTitles_ForUnknownActor()
    {
        // Arrange
        var actorName = "Unkown";

        _mockDataAccess.Setup(x => x.ExecuteReadListAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>?>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetMoviesForActor(actorName, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
