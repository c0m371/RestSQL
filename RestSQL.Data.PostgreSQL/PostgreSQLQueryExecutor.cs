using System;
using Dapper;
using Npgsql;
using RestSQL.Config;
using RestSQL.Data.QueryExecution;

namespace RestSQL.Data.PostgreSQL;

public class PostgreSQLQueryExecutor : IQueryExecutor
{
    public DatabaseType Type => DatabaseType.PostgreSQL;
    
    public Task<IEnumerable<dynamic>> QueryAsync(string connectionString, string sql, object? param)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return connection.QueryAsync(sql, param);
    }
}
