using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;

namespace RestSQL.Infrastructure.PostgreSQL;

public class PostgreSQLDataAccess : IPostgreSQLDataAccess
{
    public async Task<int> ExecuteAsync(IDbConnection connection, string sql, object? param, IDbTransaction? transaction)
    {
        var result = await connection.ExecuteAsync(sql, param, transaction).ConfigureAwait(false);
        return result;
    }

    public async Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(IDbConnection connection, string sql, object? parameters)
    {
        var results = await connection.QueryAsync(sql, parameters).ConfigureAwait(false);
        return results.Cast<IDictionary<string, object?>>();
    }

    public async Task<IDictionary<string, object?>> QueryFirstAsync(IDbConnection connection, string sql, object? param, IDbTransaction? transaction)
    {
        var result = await connection.QueryFirstAsync(sql, param, transaction);
        return result;
    }
}
