using Moq;
using System.Data;
using RestSQL.Domain;

namespace RestSQL.Infrastructure.Dapper.Tests;

public class DapperQueryExecutorTests
{
    private readonly Mock<IConnectionFactory> _mockFactory;
    private readonly Mock<IDbConnection> _mockConnection;
    private readonly Mock<IDataAccess> _mockDataAccess;
    private readonly DapperQueryExecutor _executor;

    public DapperQueryExecutorTests()
    {
        // 1. Setup Mocks
        _mockFactory = new Mock<IConnectionFactory>();
        _mockConnection = new Mock<IDbConnection>();
        _mockDataAccess = new Mock<IDataAccess>();

        // 2. Configure the Factory to return our Mock Connection
        _mockFactory
            .Setup(f => f.CreateConnection(It.IsAny<string>()))
            .Returns(_mockConnection.Object);

        // 3. Instantiate a small concrete executor (DapperQueryExecutor is abstract)
        _executor = new TestExecutor(_mockFactory.Object, _mockDataAccess.Object);

        // Setup mock connection disposal for QueryAsync test
        _mockConnection.Setup(c => c.Dispose());
    }

    private class TestExecutor : DapperQueryExecutor
    {
        public TestExecutor(IConnectionFactory factory, IDataAccess dataAccess) : base(factory, dataAccess) { }
        public override DatabaseType Type => DatabaseType.PostgreSQL;
    }

    // --------------------------------------------------------------------------------
    // QUERY ASYNC TESTS
    // --------------------------------------------------------------------------------

    [Fact]
    public void Type_ShouldReturnPostgreSQL()
    {
        Assert.Equal(DatabaseType.PostgreSQL, _executor.Type);
    }

    [Fact]
    public async Task QueryAsync_ShouldCreateConnectionExecuteQueryAndDispose()
    {
        // Arrange
        const string connString = "Host=test;DB=test";
        const string sql = "SELECT * FROM test";
        var parameters = new Dictionary<string, object?> { { "id", 1 } };
        var expectedResults = new List<IDictionary<string, object?>>
            {
                new Dictionary<string, object?> { { "col1", "data1" } }
            };

        _mockDataAccess.Setup(d => d.QueryAsync(
            _mockConnection.Object, // Verify correct connection is passed
            sql,
            parameters))
            .ReturnsAsync(expectedResults);

        // Act
        var result = await _executor.QueryAsync(connString, sql, parameters);

        // Assert
        var dict = Assert.Single(result);
        Assert.Equal("data1", dict["col1"]);

        // Verify dependency calls
        _mockFactory.Verify(f => f.CreateConnection(connString), Times.Once, "Factory must be called to create connection.");
        _mockDataAccess.Verify(c => c.QueryAsync(_mockConnection.Object, sql, parameters), Times.Once, "Dapper Query must be executed.");
        _mockConnection.Verify(c => c.Dispose(), Times.Once, "Connection must be disposed by the 'using' block.");
    }

    // --------------------------------------------------------------------------------
    // BEGIN TRANSACTION TESTS
    // --------------------------------------------------------------------------------

    [Fact]
    public void BeginTransaction_ShouldCreateConnectionAndReturnTransaction()
    {
        // Arrange
        const string connString = "Host=test;DB=test";

        // Prepare mock connection to behave like a real connection
        var mockTransaction = new Mock<IDbTransaction>();
        _mockConnection.Setup(c => c.BeginTransaction()).Returns(mockTransaction.Object);
        _mockConnection.SetupGet(c => c.State).Returns(ConnectionState.Closed);
        _mockConnection.Setup(c => c.Open()).Verifiable();

        // Act
        var transaction = _executor.BeginTransaction(connString);

        // Assert
        Assert.NotNull(transaction);
        Assert.IsAssignableFrom<RestSQL.Infrastructure.Interfaces.ITransaction>(transaction);

        // Verify dependency calls
        _mockFactory.Verify(f => f.CreateConnection(connString), Times.Once, "Factory must be called to create connection.");

        // Verify the steps that the transaction constructor should have executed on the mock connection:
        _mockConnection.VerifyGet(c => c.State, Times.AtLeastOnce, "Connection state must be checked.");
        _mockConnection.Verify(c => c.Open(), Times.Once, "Connection must be opened by transaction implementation.");
        _mockConnection.Verify(c => c.BeginTransaction(), Times.Once, "Transaction must be started by transaction implementation.");
    }
}