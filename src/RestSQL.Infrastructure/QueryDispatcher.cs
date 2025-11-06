using Microsoft.Extensions.Logging;
using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure;

public class QueryDispatcher(IEnumerable<IQueryExecutor> queryExecutors, ILogger<QueryDispatcher> logger) : IQueryDispatcher
{
    private readonly Dictionary<string, ConnectionWithExecutor> connectionsWithExecutors = [];

    public async Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(string connectionName, string sql, IDictionary<string, object?> parameters)
    {
        logger.LogDebug("QueryAsync called for connection={connection} sql={sql}", connectionName, sql);
        var connectionWithExecutor = GetConnectionWithExecutor(connectionName);
        var res = await connectionWithExecutor.QueryExecutor.QueryAsync(connectionWithExecutor.Connection.ConnectionString, sql, parameters).ConfigureAwait(false);
        logger.LogDebug("QueryAsync finished for connection={connection} rows={count}", connectionName, res.Count());
        return res;
    }

    public ITransaction BeginTransaction(string connectionName)
    {
        logger.LogDebug("BeginTransaction requested for connection={connection}", connectionName);
        var connectionWithExecutor = GetConnectionWithExecutor(connectionName);
        var tx = connectionWithExecutor.QueryExecutor.BeginTransaction(connectionWithExecutor.Connection.ConnectionString);
        logger.LogDebug("Began transaction for connection={connection}", connectionName);
        return tx;
    }

    public void InitializeExecutors(IDictionary<string, Connection> connections)
    {
        logger.LogInformation("Initializing executors for {count} connections", connections.Count);
        foreach (var kvp in connections)
        {
            var queryExecutor =
                queryExecutors.SingleOrDefault(e => e.Type == kvp.Value.Type)
                ?? throw new KeyNotFoundException($"Query executor for database type {kvp.Value.Type} not found");

            logger.LogInformation("Assigning executor {type} to connection {name}", kvp.Value.Type, kvp.Key);
            connectionsWithExecutors.Add(kvp.Key, new ConnectionWithExecutor(kvp.Value, queryExecutor));
        }
    }

    private ConnectionWithExecutor GetConnectionWithExecutor(string connectionName)
    {
        if (!connectionsWithExecutors.TryGetValue(connectionName, out var connectionWithExecutor))
        {
            if (connectionsWithExecutors.Count == 0)
            {
                logger.LogError("Query attempted before InitializeExecutors were called");
                throw new InvalidOperationException("Cannot query before InitializeExecutors were called");
            }

            logger.LogError("Query executor for connection '{conn}' not found", connectionName);
            throw new KeyNotFoundException($"Query executor for connection '{connectionName}' not found.");
        }

        return connectionWithExecutor;
    }

    private record ConnectionWithExecutor(Connection Connection, IQueryExecutor QueryExecutor);
}
