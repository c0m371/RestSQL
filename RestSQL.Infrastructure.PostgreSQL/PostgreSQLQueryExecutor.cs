using Dapper;
using Npgsql;
using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure.PostgreSQL;

public class PostgreSQLQueryExecutor : IQueryExecutor
{
    public DatabaseType Type => DatabaseType.PostgreSQL;

    public async Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(string connectionString, string sql, IDictionary<string, object?> parameters)
    {
        using var connection = new NpgsqlConnection(connectionString);
        var results = await connection.QueryAsync(sql, parameters);
        return results.Cast<IDictionary<string, object?>>();
    }

    public async Task<ITransaction> BeginTransactionAsync(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString);
        return await PostgreSQLTransaction.CreateAsync(connection).ConfigureAwait(false);
    }

    private class PostgreSQLTransaction : ITransaction
    {
        private NpgsqlConnection connection;
        private NpgsqlTransaction transaction;
        private bool disposed;

        private PostgreSQLTransaction(NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            this.connection = connection;
            this.transaction = transaction;
        }

        public static async Task<ITransaction> CreateAsync(NpgsqlConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync().ConfigureAwait(false);

            var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);
            return new PostgreSQLTransaction(connection, transaction);
        }

        public async Task<IDictionary<string, object?>> ExecuteQueryAsync(string sql, IDictionary<string, object?> parameters)
        {
            if (sql is null) throw new ArgumentNullException(nameof(sql));
            return await connection.QueryFirstAsync(sql, parameters, transaction).ConfigureAwait(false);
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, IDictionary<string, object?> parameters)
        {
            if (sql is null) throw new ArgumentNullException(nameof(sql));
            var affected = await connection.ExecuteAsync(sql, parameters, transaction).ConfigureAwait(false);
            return affected;
        }

        public async Task CommitAsync()
        {
            if (disposed) throw new ObjectDisposedException(nameof(PostgreSQLTransaction));
            await transaction.CommitAsync().ConfigureAwait(false);
        }

        public async Task RollbackAsync()
        {
            if (disposed) throw new ObjectDisposedException(nameof(PostgreSQLTransaction));
            await transaction.RollbackAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try
            {
                transaction?.Dispose();
            }
            catch
            {
                //TODO log
            }
            finally
            {
                try { connection?.Dispose(); } catch { }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (disposed) return;
            disposed = true;
            try
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                //TODO log
            }
            finally
            {
                try
                {
                    if (connection is not null)
                    {
                        await connection.DisposeAsync().ConfigureAwait(false);
                    }
                }
                catch
                {
                    //TODO log
                }
            }
        }
    }
}

