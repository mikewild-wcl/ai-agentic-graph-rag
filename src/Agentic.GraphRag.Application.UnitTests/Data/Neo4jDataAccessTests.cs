using Agentic.GraphRag.Application.Data;
using Agentic.GraphRag.Application.UnitTests.TestExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neo4j.Driver;

namespace Agentic.GraphRag.Application.UnitTests.Data;

public class Neo4jDataAccessTests
{
    private const string DATABASE_NAME_FIELD = "_databaseName";
    private const string TEST_DATABASE_NAME = "testdb";

    /* Test class required because Neo4jDataAccess is abstract */
    private sealed class TestNeo4jDataAccess(
        IDriver driver,
        string databaseName,
        ILogger<Neo4jDataAccess> logger)
        : Neo4jDataAccess(
            driver,
            databaseName,
            logger)
    {
        // Expose public wrappers already available on base class; nothing else required.
    }

    [Fact]
    public async Task Constructor_SetsDatabaseName()
    {
        // Arrange
        var sut = new TestNeo4jDataAccess(Mock.Of<IDriver>(),
            TEST_DATABASE_NAME,
            NullLogger<Neo4jDataAccess>.Instance);

        await using (sut.ConfigureAwait(false))
        {
            // Assert
            var databaseValue = sut.GetPrivateField(DATABASE_NAME_FIELD);
            databaseValue.ShouldBe(TEST_DATABASE_NAME);
        }
    }

    [Fact]
    public void Constructor_WithEmptyDatabaseName_ThrowsException()
    {
        // Act and Assert
        Should.Throw<ArgumentException>(() => new TestNeo4jDataAccess(Mock.Of<IDriver>(),
            string.Empty,
            NullLogger<Neo4jDataAccess>.Instance));
    }

    [Fact]
    public void Constructor_WithNullDatabaseName_ThrowsException()
    {
        // Act and Assert
        Should.Throw<ArgumentNullException>(() => new TestNeo4jDataAccess(Mock.Of<IDriver>(),
            null!,
            NullLogger<Neo4jDataAccess>.Instance));
    }

    [Fact]
    public async Task ExecuteReadListAsync_Invokes_RunAsync_AndReturnsMappedValues()
    {
        // Arrange
        var query = "MATCH (n) RETURN n.name AS name";
        var returnKey = "name";
        var expectedValue = "Alice";

        var recordMock = new Mock<IRecord>();
        recordMock.Setup(r => r.Values).Returns(new Dictionary<string, object> { { returnKey, expectedValue } });

        var mockAsyncEnumerableRecords = TestMocks.MockAsyncEnumerable([recordMock.Object]);

        var cursorMock = new Mock<IResultCursor>();
        cursorMock
            .Setup(cursor => cursor.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => mockAsyncEnumerableRecords.GetAsyncEnumerator());

        var txMock = new Mock<IAsyncTransaction>();
        txMock
            .Setup(t => t.RunAsync(It.Is<string>(q => q == query), It.IsAny<IDictionary<string, object>?>()))
            .ReturnsAsync(() => cursorMock.Object);

        var sessionMock = new Mock<IAsyncSession>();
        sessionMock
            .Setup(s => s.ExecuteReadAsync(It.IsAny<Func<IAsyncQueryRunner, Task<List<string>>>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncTransaction, Task<List<string>>>, Action<TransactionConfigBuilder>>((callback, _) => callback(txMock.Object));

        var driverMock = new Mock<IDriver>();
        driverMock
            .Setup(d => d.AsyncSession(It.IsAny<Action<SessionConfigBuilder>>()))
            .Returns(sessionMock.Object);

        var loggerMock = new Mock<ILogger<Neo4jDataAccess>>();

        var sut = new TestNeo4jDataAccess(driverMock.Object, TEST_DATABASE_NAME, loggerMock.Object);

        // Act
        await using (sut.ConfigureAwait(false))
        {
            var result = await sut.ExecuteReadListAsync(query, returnKey);

            // Assert
            result.ShouldHaveSingleItem(expectedValue);
            txMock.Verify(t => t.RunAsync(query, It.IsAny<IDictionary<string, object>?>()), Times.Once);
            txMock.Verify(t => t.RunAsync(query, It.Is<IDictionary<string, object>?>(dic => dic != null && dic.Count == 0)), Times.Once);
        }
    }

    [Fact]
    public async Task ExecuteReadListAsync_WithParameters_Invokes_RunAsync_AndReturnsMappedValues()
    {
        // Arrange
        var query = "MATCH (p:Person {alias: $name}) RETURN p.name AS name";
        var parameter = "name";
        var parameterValue = "Bob";
        var returnKey = "name";
        var expectedValue = "Alice";

        var parameters = new Dictionary<string, object> { { parameter, parameterValue } };

        var recordMock = new Mock<IRecord>();
        recordMock.Setup(r => r.Values).Returns(new Dictionary<string, object> { { returnKey, expectedValue } });

        var mockAsyncEnumerableRecords = TestMocks.MockAsyncEnumerable([recordMock.Object]);

        var cursorMock = new Mock<IResultCursor>();
        cursorMock
            .Setup(cursor => cursor.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => mockAsyncEnumerableRecords.GetAsyncEnumerator());

        var txMock = new Mock<IAsyncTransaction>();
        txMock
            .Setup(t => t.RunAsync(It.Is<string>(q => q == query), It.IsAny<IDictionary<string, object>?>()))
            .ReturnsAsync(() => cursorMock.Object);

        var sessionMock = new Mock<IAsyncSession>();
        sessionMock
            .Setup(s => s.ExecuteReadAsync(It.IsAny<Func<IAsyncQueryRunner, Task<List<string>>>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncTransaction, Task<List<string>>>, Action<TransactionConfigBuilder>>((callback, _) => callback(txMock.Object));

        var driverMock = new Mock<IDriver>();
        driverMock
            .Setup(d => d.AsyncSession(It.IsAny<Action<SessionConfigBuilder>>()))
            .Returns(sessionMock.Object);

        var loggerMock = new Mock<ILogger<Neo4jDataAccess>>();

        var sut = new TestNeo4jDataAccess(driverMock.Object, TEST_DATABASE_NAME, loggerMock.Object);

        // Act
        await using (sut.ConfigureAwait(false))
        {
            var result = await sut.ExecuteReadListAsync(query, returnKey, parameters);

            // Assert
            result.ShouldHaveSingleItem(expectedValue);
            txMock.Verify(t => t.RunAsync(query, It.IsAny<IDictionary<string, object>?>()), Times.Once);
            txMock.Verify(t => t.RunAsync(query, It.Is<IDictionary<string, object>?>(dic => dic != null && dic.Count == 1)), Times.Once);
            txMock.Verify(t => t.RunAsync(query, It.Is<IDictionary<string, object>?>(dic => dic != null && dic.ContainsKey(parameter) && dic[parameter] as string == parameterValue)), Times.Once);
        }
    }
}