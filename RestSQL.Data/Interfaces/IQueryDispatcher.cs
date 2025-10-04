namespace RestSQL.Data.Interfaces;

public interface IQueryDispatcher
{
    Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(string connectionName, string sql, IDictionary<string, object?> parameters);
}
