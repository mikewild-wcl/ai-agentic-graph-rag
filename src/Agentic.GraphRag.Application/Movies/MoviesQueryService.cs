using Agentic.GraphRag.Application.Movies.Interfaces;
using Microsoft.Extensions.Logging;

namespace Agentic.GraphRag.Application.Movies;

public sealed class MoviesQueryService(
    IMoviesDataAccess dataAccess,
    ILogger<MoviesQueryService> logger) : IMoviesQueryService
{
    private readonly IMoviesDataAccess _dataAccess = dataAccess;
    private readonly ILogger<MoviesQueryService> _logger = logger;

    private static readonly Action<Microsoft.Extensions.Logging.ILogger, string, Exception?> _logQueryError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, nameof(MoviesQueryService)),
            "Error querying movies for actor {ActorName}");

    public async Task<List<string>> GetMoviesForActor(string actorName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dataAccess.ExecuteReadListAsync(
                @"MATCH (a:Person {name: $name})-[:ACTED_IN]->(m:Movie) RETURN m.title AS movieTitle",
                "movieTitle",
                new Dictionary<string, object> {
                    { "name", actorName }
                })
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logQueryError(_logger, actorName, ex);
            throw;
        }
    }
}
