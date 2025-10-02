using System;
using RestSQL.Config;

namespace RestSQL.Data.QueryExecution;

public interface IQueryExecutor
{
    DatabaseType Type { get; }
    Task<IEnumerable<dynamic>> QueryAsync(string connectionString, string sql, object? param);
}
