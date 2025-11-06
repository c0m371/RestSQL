using System.Data;
using Microsoft.Data.Sqlite;
using RestSQL.Infrastructure.Dapper;

namespace RestSQL.Infrastructure.Sqlite;

public class SqliteConnectionFactory: IConnectionFactory
{
    public IDbConnection CreateConnection(string connectionString)
    {
        return new SqliteConnection(connectionString);
    }
}
