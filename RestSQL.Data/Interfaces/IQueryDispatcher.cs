namespace RestSQL.Data.Interfaces;

public interface IQueryDispatcher
{
    Task<IEnumerable<dynamic>> QueryAsync(string connectionName, string sql, object? parameters);
}
