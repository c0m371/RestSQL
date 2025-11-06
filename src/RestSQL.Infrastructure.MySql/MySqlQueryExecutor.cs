using RestSQL.Domain;
using RestSQL.Infrastructure.Dapper;

namespace RestSQL.Infrastructure.MySql;

public class MySqlQueryExecutor : DapperQueryExecutor
{
    public MySqlQueryExecutor(MySqlConnectionFactory connectionFactory, IDataAccess dataAccess) : base(connectionFactory, dataAccess)
    {
    }

    public override DatabaseType Type => DatabaseType.MySql;
}
