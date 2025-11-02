using System;
using RestSQL.Domain;
using RestSQL.Infrastructure.Dapper;

namespace RestSQL.Infrastructure.PostgreSQL;

public class PostgreSQLQueryExecutor : DapperQueryExecutor
{
    public PostgreSQLQueryExecutor(PostgreSQLConnectionFactory connectionFactory, IDataAccess dataAccess) : base(connectionFactory, dataAccess)
    {
    }

    public override DatabaseType Type => DatabaseType.PostgreSQL;
}
