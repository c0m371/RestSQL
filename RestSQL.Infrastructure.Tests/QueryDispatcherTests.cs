using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure.Tests;

public class QueryDispatcherTests
{
    // A simple fake implementation of ITransaction for testing purposes
    private record FakeTransaction : ITransaction
    {
        // These methods are not directly tested by the dispatcher, so we throw/ignore
        public Task<IDictionary<string, object?>> ExecuteQueryAsync(string sql, IDictionary<string, object?> parameters) => throw new NotImplementedException();
        public Task<int> ExecuteNonQueryAsync(string sql, IDictionary<string, object?> parameters) => throw new NotImplementedException();
        public Task CommitAsync() => Task.CompletedTask;
        public Task RollbackAsync() => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
    }

    private class FakeQueryExecutor : IQueryExecutor
    {
        public DatabaseType Type { get; set; } = DatabaseType.PostgreSQL;
        public string LastConnectionStringUsed { get; private set; } = string.Empty;

        public Task<ITransaction> BeginTransactionAsync(string connectionString)
        {
            // Capture the connection string used for assertion
            LastConnectionStringUsed = connectionString;
            return Task.FromResult<ITransaction>(new FakeTransaction());
        }

        public Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(string connectionString, string sql, IDictionary<string, object?> parameters)
        {
            // Capture the connection string used for assertion (optional, but good practice)
            LastConnectionStringUsed = connectionString;

            return Task.FromResult<IEnumerable<IDictionary<string, object?>>>(new[]
            {
            new Dictionary<string, object?> { { "result", 42 } }
        });
        }
    }

    [Fact]
    public async Task QueryAsync_Throws_WhenNotInitialized()
    {
        var dispatcher = new QueryDispatcher(new[] { new FakeQueryExecutor() });
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.QueryAsync("conn", "SELECT 1", new Dictionary<string, object?>()));
        Assert.Contains("InitializeExecutors", ex.Message);
    }

    [Fact]
    public async Task QueryAsync_Throws_WhenConnectionNotFound()
    {
        var dispatcher = new QueryDispatcher(new[] { new FakeQueryExecutor() });
        var connections = new Dictionary<string, Connection>
        {
            { "conn1", new Connection { Type = DatabaseType.PostgreSQL, ConnectionString = "CS1" } }
        };
        dispatcher.InitializeExecutors(connections);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            dispatcher.QueryAsync("conn2", "SELECT 1", new Dictionary<string, object?>()));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void InitializeExecutors_Throws_WhenExecutorTypeNotFound()
    {
        var dispatcher = new QueryDispatcher(new[] { new FakeQueryExecutor() });
        var connections = new Dictionary<string, Connection>
        {
            { "conn1", new Connection { Type = DatabaseType.Sqlite, ConnectionString = "CS1" } }
        };
        var ex = Assert.Throws<KeyNotFoundException>(() => dispatcher.InitializeExecutors(connections));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task QueryAsync_ReturnsResult_WhenInitialized()
    {
        var dispatcher = new QueryDispatcher(new[] { new FakeQueryExecutor() });
        var connections = new Dictionary<string, Connection>
        {
            { "conn1", new Connection { Type = DatabaseType.PostgreSQL, ConnectionString = "CS1" } }
        };
        dispatcher.InitializeExecutors(connections);

        var result = await dispatcher.QueryAsync("conn1", "SELECT 1", new Dictionary<string, object?>());
        var dict = Assert.Single(result);
        Assert.Equal(42, dict["result"]);
    }

    [Fact]
    public async Task BeginTransactionAsync_ReturnsTransaction_WhenInitialized()
    {
        // Arrange
        const string expectedConnectionString = "CS_TX";
        const string connectionName = "tx_conn";

        // Use an executor that captures the connection string
        var fakeExecutor = new FakeQueryExecutor { Type = DatabaseType.PostgreSQL };
        var dispatcher = new QueryDispatcher(new[] { fakeExecutor });

        var connections = new Dictionary<string, Connection>
        {
            { connectionName, new Connection { Type = DatabaseType.PostgreSQL, ConnectionString = expectedConnectionString } }
        };
        dispatcher.InitializeExecutors(connections);

        // Act
        var resultTransaction = await dispatcher.BeginTransactionAsync(connectionName);

        // Assert
        // 1. Check the return type
        Assert.NotNull(resultTransaction);
        Assert.IsAssignableFrom<ITransaction>(resultTransaction);

        // 2. Check the correct connection string was passed to the executor
        Assert.Equal(expectedConnectionString, fakeExecutor.LastConnectionStringUsed);
    }

    [Fact]
    public async Task BeginTransactionAsync_Throws_WhenNotInitialized()
    {
        // Arrange
        var dispatcher = new QueryDispatcher(new[] { new FakeQueryExecutor() });

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.BeginTransactionAsync("conn"));

        Assert.Contains("InitializeExecutors", ex.Message);
    }

    [Fact]
    public async Task BeginTransactionAsync_Throws_WhenConnectionNotFound()
    {
        // Arrange
        var dispatcher = new QueryDispatcher(new[] { new FakeQueryExecutor() });
        var connections = new Dictionary<string, Connection>
    {
        { "conn1", new Connection { Type = DatabaseType.PostgreSQL, ConnectionString = "CS1" } }
    };
        dispatcher.InitializeExecutors(connections);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            dispatcher.BeginTransactionAsync("conn2"));

        Assert.Contains("not found", ex.Message);
    }
}