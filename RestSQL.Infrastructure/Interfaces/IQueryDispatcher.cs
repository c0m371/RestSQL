using RestSQL.Domain;

namespace RestSQL.Infrastructure.Interfaces;

public interface IQueryDispatcher
{
    void InitializeExecutors(IDictionary<string, Connection> connections);
    Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(string connectionName, string sql, IDictionary<string, object?> parameters);
}
