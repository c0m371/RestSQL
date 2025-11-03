using RestSQL.Domain;
using RestSQL.Infrastructure.Dapper;

namespace RestSQL.Infrastructure.SqlServer;

public class SqlServerQueryExecutor : DapperQueryExecutor
{
    public SqlServerQueryExecutor(SqlServerConnectionFactory connectionFactory, IDataAccess dataAccess) : base(connectionFactory, dataAccess)
    {
    }

    public override DatabaseType Type => DatabaseType.SqlServer;
}
