using RestSQL.Domain;
using RestSQL.Infrastructure.Dapper;

namespace RestSQL.Infrastructure.Oracle;

public class OracleQueryExecutor : DapperQueryExecutor
{
    public OracleQueryExecutor(OracleConnectionFactory connectionFactory, IDataAccess dataAccess) : base(connectionFactory, dataAccess)
    {
    }

    public override DatabaseType Type => DatabaseType.Oracle;
}
