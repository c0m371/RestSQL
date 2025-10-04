using Dapper;
using Npgsql;
using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure.PostgreSQL;

public class PostgreSQLQueryExecutor : IQueryDispatcher
{
    public DatabaseType Type => DatabaseType.PostgreSQL;

    public async Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(string connectionString, string sql, IDictionary<string, object?> parameters)
    {
        using var connection = new NpgsqlConnection(connectionString);
        var results = await connection.QueryAsync(sql, parameters);
        return results.Cast<IDictionary<string, object?>>();
    }
}
