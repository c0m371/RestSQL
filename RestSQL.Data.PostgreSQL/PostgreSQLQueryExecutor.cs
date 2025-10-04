using Dapper;
using Npgsql;
using RestSQL.Config;
using RestSQL.Data.QueryExecution;

namespace RestSQL.Data.PostgreSQL;

public class PostgreSQLQueryExecutor : IQueryExecutor
{
    public DatabaseType Type => DatabaseType.PostgreSQL;

    public async Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(string connectionString, string sql, IDictionary<string, object?> parameters)
    {
        using var connection = new NpgsqlConnection(connectionString);
        var results = await connection.QueryAsync(sql, parameters);
        return results.Cast<IDictionary<string, object?>>();
    }
}
