using Agentic.GraphRag.Application.Movies;
using Agentic.GraphRag.Application.Settings;
using Agentic.GraphRag.Application.UnitTests.TestExtensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Agentic.GraphRag.Application.UnitTests.Movies;

public class MoviesDataAccessTests
{
    private const string DATABASE_NAME = "moviesDb";
    private const string DATABASE_NAME_FIELD = "_databaseName";        

    [Fact]
    public async Task Constructor_SetsDatabaseName()
    {
        // Arrange
        IOptions<GraphDatabaseSettings> options = new OptionsWrapper<GraphDatabaseSettings>(
            new GraphDatabaseSettings
            {
                MoviesDb = DATABASE_NAME
            });

        var sut = new MoviesDataAccess(Mock.Of<IDriver>(),
            options,
            NullLogger<MoviesDataAccess>.Instance);

        await using (sut.ConfigureAwait(false))
        {
            // Assert
            var databaseValue = sut.GetPrivateField(DATABASE_NAME_FIELD);
            databaseValue.Should().Be(DATABASE_NAME);
        }
    }
}
