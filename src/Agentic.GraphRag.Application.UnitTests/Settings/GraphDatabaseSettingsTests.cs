using Agentic.GraphRag.Application.Settings;

namespace Agentic.GraphRag.Application.UnitTests.Settings;

public class GraphDatabaseSettingsTests
{
    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var connection = new Uri("bolt://localhost:7687");
        var user = "TEST_USER";
        var password = "PASSWORD";
        var defaultDb = "neo4j";
        var defaultProvider = "neo4j";
        var defaultTimeout = 30;

        // Act
        var options = new GraphDatabaseSettings(connection, user, password);

        // Assert
        options.Connection.ShouldBe(connection);
        options.User.ShouldBe(user);
        options.Password.ShouldBe(password);
        options.Provider.ShouldBe(defaultProvider);
        options.EinsteinVectorDb.ShouldBe(defaultDb);
        options.MoviesDb.ShouldBe(defaultDb);
        options.UfoDb.ShouldBe(defaultDb);
        options.Timeout.ShouldBe(defaultTimeout);
    }

    [Fact]
    public void With_ShouldSetPropertiesCorrectly()
    {
        //Arrange
        var connection = new Uri("bolt://localhost:7687");
        var user = "TEST_USER";
        var password = "PASSWORD";
        var provider = "memgraph";
        var einsteinVectorDb = "einstein";
        var moviesDb = "movies";
        var ufoDb = "ufo";
        var timeout = 100;

        var options = new GraphDatabaseSettings(
            new Uri("https://dummy.endpoint"),
            "DUMMY_USER",
            "DUMMY_PASSWORD")
        {
            Provider = provider,
            Timeout = 10
        };

        // Act
        options = options with
        {
            Connection = connection,
            User = user,
            Password = password,
            EinsteinVectorDb = einsteinVectorDb,
            MoviesDb = moviesDb,
            UfoDb = ufoDb,
            Timeout = timeout
        };

        //Assert
        options.Connection.ShouldBe(connection);
        options.User.ShouldBe(user);
        options.Password.ShouldBe(password);
        options.Provider.ShouldBe(provider);
        options.EinsteinVectorDb.ShouldBe(einsteinVectorDb);
        options.MoviesDb.ShouldBe(moviesDb);
        options.UfoDb.ShouldBe(ufoDb);
        options.Timeout.ShouldBe(timeout);
    }

    [Fact]
    public void NonConstructorProperties_CanBeSetViaInit()
    {
        // Arrange
        var options = new GraphDatabaseSettings(new Uri("http://dummy"), "user", "pwd")
        { 
            Provider = "memgraph",
            EinsteinVectorDb = "einstein",
            MoviesDb = "movies",
            UfoDb = "ufo",
            Timeout = 99 
        };

        // Assert
        options.Provider.ShouldBe("memgraph");
        options.EinsteinVectorDb.ShouldBe("einstein");
        options.MoviesDb.ShouldBe("movies");
        options.UfoDb.ShouldBe("ufo");
        options.Timeout.ShouldBe(99);
    }
}
