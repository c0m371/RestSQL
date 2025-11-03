using System.Data;

namespace RestSQL.Infrastructure.Dapper;

public interface IDataAccess
{
    Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(
        IDbConnection connection,
        string sql,
        object? param
    );

    Task<IDictionary<string, object?>> QueryFirstAsync(
        IDbConnection connection,
        string sql,
        object? param,
        IDbTransaction? transaction
    );

    Task<int> ExecuteAsync(
        IDbConnection connection,
        string sql,
        object? param,
        IDbTransaction? transaction
    );
}
