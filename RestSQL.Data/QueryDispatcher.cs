using RestSQL.Config;
using RestSQL.Data.Interfaces;

namespace RestSQL.Data;

public class QueryDispatcher : IQueryDispatcher
{
    private readonly Dictionary<string, ConnectionWithExecutor> connectionsWithExecutors = [];

    public QueryDispatcher(IList<Connection> connections, IList<IQueryExecutor> queryExecutors)
    {
        InitializeExecutors(connections, queryExecutors);
    }

    public async Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(string connectionName, string sql, IDictionary<string, object?> parameters)
    {
        if (!connectionsWithExecutors.TryGetValue(connectionName, out var connectionWithExecutor))
            throw new KeyNotFoundException($"Query executor for connection '{connectionName}' not found.");

        return await connectionWithExecutor.QueryExecutor.QueryAsync(connectionWithExecutor.Connection.ConnectionString, sql, parameters).ConfigureAwait(false);
    }

    private void InitializeExecutors(IList<Connection> connections, IList<IQueryExecutor> queryExecutors)
    {
        foreach (var connection in connections)
        {
            var queryExecutor =
                queryExecutors.SingleOrDefault(e => e.Type == connection.Type)
                ?? throw new KeyNotFoundException($"Query executor for database type {connection.Type} not found");

            connectionsWithExecutors.Add(connection.Name, new ConnectionWithExecutor(connection, queryExecutor));
        }
    }

    private record ConnectionWithExecutor(Connection Connection, IQueryExecutor QueryExecutor);
}
