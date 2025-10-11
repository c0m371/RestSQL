using System;

namespace RestSQL.Infrastructure.Interfaces;

public interface ITransaction : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Execute a SQL query that returns a result row.
    /// </summary>
    Task<IDictionary<string, object?>> ExecuteQueryAsync(string sql, IDictionary<string, object?> parameters);

    /// <summary>
    /// Execute a SQL non-query command that returns the number of affected rows.
    /// </summary>
    Task<int> ExecuteNonQueryAsync(string sql, IDictionary<string, object?> parameters);

    /// <summary>
    /// Commit the transaction.
    /// </summary>
    Task CommitAsync();

    /// <summary>
    /// Rollback the transaction.
    /// </summary>
    Task RollbackAsync();
}
