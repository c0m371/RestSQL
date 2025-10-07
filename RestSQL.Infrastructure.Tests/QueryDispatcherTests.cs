using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure.Tests;

public class QueryDispatcherTests
{
    private class FakeQueryExecutor : IQueryExecutor
    {
        public DatabaseType Type { get; set; } = DatabaseType.PostgreSQL;
        public Task<IEnumerable<IDictionary<string, object?>>> QueryAsync(string connectionString, string sql, IDictionary<string, object?> parameters)
        {
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
}