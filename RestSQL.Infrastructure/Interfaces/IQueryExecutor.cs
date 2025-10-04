using RestSQL.Domain;

namespace RestSQL.Infrastructure.Interfaces;

public interface IQueryExecutor
{
    DatabaseType Type { get; }
    Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(string connectionString, string sql, IDictionary<string, object?> parameters);
}
