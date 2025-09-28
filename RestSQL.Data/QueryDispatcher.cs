using System;
using RestSQL.Config;
using RestSQL.Data.Interfaces;

namespace RestSQL.Data;

public class QueryDispatcher
{
    private readonly Dictionary<string, (string connectionstring, IQueryExecutor queryExecutor)> queryExecutors = [];

    public QueryDispatcher(IList<Connection> connections, IList<IQueryExecutor> queryExecutors)
    {
        InitializeExecutors(connections, queryExecutors);
    }



    public Task<IEnumerable<dynamic>> QueryAsync(string connectionName, string sql, object? parameters)
    {
        bool found = queryExecutors.TryGetValue(connectionName, out var queryExecutor);

        if (!found)
            throw new KeyNotFoundException($"Query executor for connection {connectionName} not found");

        return queryExecutor.queryExecutor.QueryAsync(queryExecutor.connectionstring, sql, parameters);
    }

    private void InitializeExecutors(IList<Connection> connections, IList<IQueryExecutor> supportedQueryExecutors)
    {
        foreach (var connection in connections)
        {
            var queryExecutor =
                supportedQueryExecutors.SingleOrDefault(e => e.Type == connection.Type)
                ?? throw new KeyNotFoundException($"Query executor for database type {connection.Type} not found");

            queryExecutors.Add(connection.Name, (connection.ConnectionString, queryExecutor));
        }
    }
}
