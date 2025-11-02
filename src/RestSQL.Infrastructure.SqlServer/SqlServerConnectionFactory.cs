using System.Data;
using Microsoft.Data.SqlClient;
using RestSQL.Infrastructure.Dapper;

namespace RestSQL.Infrastructure.SqlServer;

public class SqlServerConnectionFactory : IConnectionFactory
{
    public IDbConnection CreateConnection(string connectionString)
    {
        return new SqlConnection(connectionString);
    }
}
