using System.Data;
using MySqlConnector;
using RestSQL.Infrastructure.Dapper;

namespace RestSQL.Infrastructure.MySql;

public class MySqlConnectionFactory : IConnectionFactory
{
    public IDbConnection CreateConnection(string connectionString)
    {
        return new MySqlConnection(connectionString);
    }
}
