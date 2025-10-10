using System.Data;
using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure;

public class QueryDispatcher(IEnumerable<IQueryExecutor> queryExecutors) : IQueryDispatcher
{
    private readonly Dictionary<string, ConnectionWithExecutor> connectionsWithExecutors = [];

    public async Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(string connectionName, string sql, IDictionary<string, object?> parameters)
    {
        var connectionWithExecutor = GetConnectionWithExecutor(connectionName);
        return await connectionWithExecutor.QueryExecutor.QueryAsync(connectionWithExecutor.Connection.ConnectionString, sql, parameters).ConfigureAwait(false);
    }

    public void InitializeExecutors(IDictionary<string, Connection> connections)
    {
        foreach (var kvp in connections)
        {
            var queryExecutor =
                queryExecutors.SingleOrDefault(e => e.Type == kvp.Value.Type)
                ?? throw new KeyNotFoundException($"Query executor for database type {kvp.Value.Type} not found");

            connectionsWithExecutors.Add(kvp.Key, new ConnectionWithExecutor(kvp.Value, queryExecutor));
        }
    }

    private ConnectionWithExecutor GetConnectionWithExecutor(string connectionName)
    {
        if (!connectionsWithExecutors.TryGetValue(connectionName, out var connectionWithExecutor))
        {
            if (connectionsWithExecutors.Count == 0)
                throw new InvalidOperationException("Cannot query before InitializeExecutors were called");

            throw new KeyNotFoundException($"Query executor for connection '{connectionName}' not found.");
        }

        return connectionWithExecutor;
    }

    private record ConnectionWithExecutor(Connection Connection, IQueryExecutor QueryExecutor);
}
