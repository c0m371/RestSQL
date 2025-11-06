using RestSQL.Domain;
using RestSQL.Infrastructure.Dapper;

namespace RestSQL.Infrastructure.Sqlite;

public class SqliteQueryExecutor : DapperQueryExecutor
{
    public SqliteQueryExecutor(SqliteConnectionFactory connectionFactory, IDataAccess dataAccess) : base(connectionFactory, dataAccess)
    {
    }

    public override DatabaseType Type => DatabaseType.Sqlite;
}