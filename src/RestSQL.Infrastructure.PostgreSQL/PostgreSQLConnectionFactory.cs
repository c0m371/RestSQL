using System.Data;
using Npgsql;

namespace RestSQL.Infrastructure.PostgreSQL;

public class PostgreSQLConnectionFactory : IPostgreSQLConnectionFactory
{
    public IDbConnection CreatePostgreSQLConnection(string connectionString)
    {
        return new NpgsqlConnection(connectionString);
    }
}