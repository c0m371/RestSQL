using System;
using RestSQL.Config;
using RestSQL.Data.Interfaces;

namespace RestSQL.Data;

public class QueryDispatcher
{
    private readonly Dictionary<string, ConnectionWithExecutor> connectionExecutors = [];

    public QueryDispatcher(IList<Connection> connections, IList<IQueryExecutor> queryExecutors)
    {
        InitializeExecutors(connections, queryExecutors);
    }



    public Task<IEnumerable<dynamic>> QueryAsync(string connectionName, string sql, object? parameters)
    {
        connectionExecutors.TryGetValue(connectionName, out var connectionWithExecutor);

        if (connectionWithExecutor == null)
            throw new KeyNotFoundException($"Query executor for connection {connectionName} not found");

        return connectionWithExecutor.QueryExecutor.QueryAsync(connectionWithExecutor.Connection.ConnectionString, sql, parameters);
    }

    private void InitializeExecutors(IList<Connection> connections, IList<IQueryExecutor> queryExecutors)
    {
        foreach (var connection in connections)
        {
            var queryExecutor =
                queryExecutors.SingleOrDefault(e => e.Type == connection.Type)
                ?? throw new KeyNotFoundException($"Query executor for database type {connection.Type} not found");

            connectionExecutors.Add(connection.Name, new ConnectionWithExecutor(connection, queryExecutor));
        }
    }

    private record ConnectionWithExecutor(Connection Connection, IQueryExecutor QueryExecutor);
}
