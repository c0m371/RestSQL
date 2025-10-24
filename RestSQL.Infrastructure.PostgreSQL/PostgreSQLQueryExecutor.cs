using System.Data;
using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure.PostgreSQL;

public class PostgreSQLQueryExecutor : IQueryExecutor
{
    private IPostgreSQLConnectionFactory connectionFactory;
    private IPostgreSQLDataAccess dataAccess;

    public PostgreSQLQueryExecutor(IPostgreSQLConnectionFactory connectionFactory, IPostgreSQLDataAccess dataAccess)
    {
        this.connectionFactory = connectionFactory;
        this.dataAccess = dataAccess;
    }

    public DatabaseType Type => DatabaseType.PostgreSQL;

    public async Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(string connectionString, string sql, IDictionary<string, object?> parameters)
    {
        using var connection = connectionFactory.CreatePostgreSQLConnection(connectionString);
        var results = await dataAccess.QueryAsync(connection, sql, parameters).ConfigureAwait(false);
        return results.Cast<IDictionary<string, object?>>();
    }

    public ITransaction BeginTransaction(string connectionString)
    {
        var connection = connectionFactory.CreatePostgreSQLConnection(connectionString);
        return new PostgreSQLTransaction(connection, dataAccess);
    }
}
