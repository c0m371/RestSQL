using System;
using RestSQL.Config;

namespace RestSQL.Data.Interfaces;

public interface IQueryExecutor
{
    DatabaseType Type { get; }
    Task<IEnumerable<dynamic>> QueryAsync(string connectionString, string sql, object? param);
}
