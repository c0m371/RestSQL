using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure;

public class QueryDispatcher(IEnumerable<IQueryExecutor> queryExecutors, ILogger<QueryDispatcher>? logger = null) : IQueryDispatcher
{
    private readonly Dictionary<string, ConnectionWithExecutor> connectionsWithExecutors = [];
    private readonly ILogger<QueryDispatcher> _logger = logger ?? NullLogger<QueryDispatcher>.Instance;

    public async Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(string connectionName, string sql, IDictionary<string, object?> parameters)
    {
        _logger.LogDebug("QueryAsync called for connection={connection} sql={sql}", connectionName, sql);
        var connectionWithExecutor = GetConnectionWithExecutor(connectionName);
        var res = await connectionWithExecutor.QueryExecutor.QueryAsync(connectionWithExecutor.Connection.ConnectionString, sql, parameters).ConfigureAwait(false);
        _logger.LogDebug("QueryAsync finished for connection={connection} rows={count}", connectionName, res?.Count() ?? 0);
        return res;
    }

    public ITransaction BeginTransaction(string connectionName)
    {
        _logger.LogDebug("BeginTransaction requested for connection={connection}", connectionName);
        var connectionWithExecutor = GetConnectionWithExecutor(connectionName);
        var tx = connectionWithExecutor.QueryExecutor.BeginTransaction(connectionWithExecutor.Connection.ConnectionString);
        _logger.LogInformation("Began transaction for connection={connection}", connectionName);
        return tx;
    }

    public void InitializeExecutors(IDictionary<string, Connection> connections)
    {
        _logger.LogInformation("Initializing executors for {count} connections", connections.Count);
        foreach (var kvp in connections)
        {
            var queryExecutor =
                queryExecutors.SingleOrDefault(e => e.Type == kvp.Value.Type)
                ?? throw new KeyNotFoundException($"Query executor for database type {kvp.Value.Type} not found");

            _logger.LogDebug("Assigning executor {type} to connection {name}", kvp.Value.Type, kvp.Key);
            connectionsWithExecutors.Add(kvp.Key, new ConnectionWithExecutor(kvp.Value, queryExecutor));
        }
    }

    private ConnectionWithExecutor GetConnectionWithExecutor(string connectionName)
    {
        if (!connectionsWithExecutors.TryGetValue(connectionName, out var connectionWithExecutor))
        {
            if (connectionsWithExecutors.Count == 0)
            {
                _logger.LogError("Query attempted before InitializeExecutors were called");
                throw new InvalidOperationException("Cannot query before InitializeExecutors were called");
            }

            _logger.LogError("Query executor for connection '{conn}' not found", connectionName);
            throw new KeyNotFoundException($"Query executor for connection '{connectionName}' not found.");
        }

        return connectionWithExecutor;
    }

    private record ConnectionWithExecutor(Connection Connection, IQueryExecutor QueryExecutor);
}
