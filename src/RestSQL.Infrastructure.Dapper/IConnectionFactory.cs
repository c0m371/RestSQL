using System.Data;

namespace RestSQL.Infrastructure.Dapper;

public interface IConnectionFactory
{
    IDbConnection CreateConnection(string connectionString);
}
