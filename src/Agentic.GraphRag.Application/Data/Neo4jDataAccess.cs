using Agentic.GraphRag.Application.Data.Interfaces;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Agentic.GraphRag.Application.Data;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
public abstract class Neo4jDataAccess : INeo4jDataAccess
{
    private readonly string _databaseName;

    private readonly IAsyncSession _session;

    private readonly ILogger<Neo4jDataAccess> _logger;

    private static readonly Action<Microsoft.Extensions.Logging.ILogger, string, Exception?> _logQueryError =
    LoggerMessage.Define<string>(LogLevel.Error,
        new EventId(1, nameof(Neo4jDataAccess)),
        "There was a problem while executing database {AccessType} query");

    protected Neo4jDataAccess(
        IDriver driver,
        string databaseName,
        ILogger<Neo4jDataAccess> logger)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        _databaseName = databaseName;

        _session = driver.AsyncSession(o => o.WithDatabase(_databaseName));
        _logger = logger;
    }

    public async Task<List<Dictionary<string, object>>> ExecuteReadDictionaryAsync(string query, string returnObjectKey, IDictionary<string, object>? parameters = null)
    {
        return await ExecuteReadTransactionAsync<Dictionary<string, object>>(query, returnObjectKey, parameters);
    }

    public async Task<List<string>> ExecuteReadListAsync(string query, string returnObjectKey, IDictionary<string, object>? parameters = null)
    {
        return await ExecuteReadTransactionAsync<string>(query, returnObjectKey, parameters);
    }

    public async Task<T> ExecuteReadScalarAsync<T>(string query, IDictionary<string, object>? parameters = null)
    {
        try
        {
            parameters ??= new Dictionary<string, object>();

            var result = await _session.ExecuteReadAsync(async tx =>
            {
                var res = await tx.RunAsync(query, parameters);
                var scalar = (await res.SingleAsync())[0].As<T>();
                return scalar;
            });

            return result;
        }
        catch (Exception ex)
        {
            _logQueryError(_logger, "Read Scalar", ex);
            throw;
        }
    }

    public async Task ExecuteWriteTransactionAsync(string query, IDictionary<string, object>? parameters = null)
    {
        try
        {
            parameters ??= new Dictionary<string, object>();

            var result = await _session.ExecuteWriteAsync(async tx =>
            {
                var res = await tx.RunAsync(query, parameters);
                return res;
            });

            //return result;
        }
        catch (Exception ex)
        {
            _logQueryError(_logger, "Write", ex);
            throw;
        }
    }

    public async Task<T> ExecuteWriteTransactionAsync<T>(string query, IDictionary<string, object>? parameters = null)
    {
        try
        {
            parameters ??= new Dictionary<string, object>();

            var result = await _session.ExecuteWriteAsync(async tx =>
            {
                var res = await tx.RunAsync(query, parameters);
                var scalar = (await res.SingleAsync())[0].As<T>();
                return scalar;
            });

            return result;
        }
        catch (Exception ex)
        {
            _logQueryError(_logger, "Write", ex);
            throw;
        }
    }

    private async Task<List<T>> ExecuteReadTransactionAsync<T>(string query, string returnObjectKey, IDictionary<string, object>? parameters)
    {
        try
        {
            parameters ??= new Dictionary<string, object>();

            var result = await _session.ExecuteReadAsync(async tx =>
            {
                var res = await tx.RunAsync(query, parameters);

                var records = await res.ToListAsync();

                var data = records.Select(x => (T)x.Values[returnObjectKey])?.ToList();

                return data;
            });

            return result ?? Enumerable.Empty<T>().ToList();
        }
        catch (Exception ex)
        {
            _logQueryError(_logger, "Read", ex);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_session is not null)
        {
            await _session.CloseAsync();
            await _session.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
