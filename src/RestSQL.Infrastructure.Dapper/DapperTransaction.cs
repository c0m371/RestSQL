using System.Data;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure.Dapper;

public class DapperTransaction : ITransaction
{
    private IDbConnection connection;
    private IDbTransaction transaction;
    private IDataAccess dataAccess;
    private bool disposed;

    internal DapperTransaction(IDbConnection connection, IDataAccess dataAccess)
    {
        this.connection = connection;
        this.dataAccess = dataAccess;

        if (connection.State != ConnectionState.Open)
            connection.Open();

        transaction = connection.BeginTransaction();
    }

    public async Task<IDictionary<string, object?>> ExecuteQueryAsync(string sql, IDictionary<string, object?> parameters)
    {
        if (sql is null) throw new ArgumentNullException(nameof(sql));
        return await dataAccess.QueryFirstAsync(connection, sql, parameters, transaction).ConfigureAwait(false);
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, IDictionary<string, object?> parameters)
    {
        if (sql is null) throw new ArgumentNullException(nameof(sql));
        var affected = await dataAccess.ExecuteAsync(connection, sql, parameters, transaction).ConfigureAwait(false);
        return affected;
    }

    public void Commit()
    {
        if (disposed) throw new ObjectDisposedException(nameof(DapperTransaction));
        transaction.Commit();
    }

    public void Rollback()
    {
        if (disposed) throw new ObjectDisposedException(nameof(DapperTransaction));
        transaction.Rollback();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
        {
            try
            {
                transaction?.Dispose();
            }
            catch
            {
                //TODO log
            }

            try
            {
                connection?.Dispose();
            }
            catch
            {
                //TODO log
            }
        }

        disposed = true;
    }
}

