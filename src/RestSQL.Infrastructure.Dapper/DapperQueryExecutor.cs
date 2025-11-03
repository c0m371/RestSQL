using System.Data;
using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure.Dapper;

public abstract class DapperQueryExecutor : IQueryExecutor
{
    private IConnectionFactory connectionFactory;
    private IDataAccess dataAccess;

    public DapperQueryExecutor(IConnectionFactory connectionFactory, IDataAccess dataAccess)
    {
        this.connectionFactory = connectionFactory;
        this.dataAccess = dataAccess;
    }

    public abstract DatabaseType Type { get; }

    public async Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(string connectionString, string sql, IDictionary<string, object?> parameters)
    {
        using var connection = connectionFactory.CreateConnection(connectionString);
        var results = await dataAccess.QueryAsync(connection, sql, parameters).ConfigureAwait(false);
        return results.Cast<IDictionary<string, object?>>();
    }

    public ITransaction BeginTransaction(string connectionString)
    {
        var connection = connectionFactory.CreateConnection(connectionString);
        return new DapperTransaction(connection, dataAccess);
    }
}
