using System.Data;
using Npgsql;
using RestSQL.Infrastructure.Dapper;

namespace RestSQL.Infrastructure.PostgreSQL;

public class PostgreSQLConnectionFactory : IConnectionFactory
{
    public IDbConnection CreateConnection(string connectionString)
    {
        return new NpgsqlConnection(connectionString);
    }
}