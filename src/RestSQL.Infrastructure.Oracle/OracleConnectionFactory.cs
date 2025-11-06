using System.Data;
using RestSQL.Infrastructure.Dapper;
using Oracle.ManagedDataAccess.Client;

namespace RestSQL.Infrastructure.Oracle;

public class OracleConnectionFactory : IConnectionFactory
{
    public IDbConnection CreateConnection(string connectionString)
    {
        return new OracleConnection(connectionString);
    }
}