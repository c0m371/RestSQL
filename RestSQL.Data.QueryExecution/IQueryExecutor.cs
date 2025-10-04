using RestSQL.Config;

namespace RestSQL.Data.QueryExecution;

public interface IQueryExecutor
{
    DatabaseType Type { get; }
    Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(string connectionString, string sql, IDictionary<string, object?> parameters);
}
