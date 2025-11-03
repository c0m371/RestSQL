using Moq;
using System.Data;
using RestSQL.Domain;

namespace RestSQL.Infrastructure.Dapper.Tests;

public class DapperTransactionTests
{
    private readonly Mock<IDbConnection> _mockConnection;
    private readonly Mock<IDbTransaction> _mockTransaction;
    private readonly Mock<IDataAccess> _mockDataAccess;
    private readonly Mock<IConnectionFactory> _mockFactory;

    public DapperTransactionTests()
    {
        // 1. Setup Mocks (MockBehavior.Strict ensures that any un-setup call throws)
        _mockConnection = new Mock<IDbConnection>();
        _mockTransaction = new Mock<IDbTransaction>();
        _mockDataAccess = new Mock<IDataAccess>();
        _mockFactory = new Mock<IConnectionFactory>();

        // 2. Configure mock connection to return the mock transaction
        _mockConnection.Setup(c => c.BeginTransaction()).Returns(_mockTransaction.Object);
        // We initially set the connection state for the constructor test
        _mockConnection.SetupGet(c => c.State).Returns(ConnectionState.Closed);

        // 3. Setup disposal on the mocks (must be done to prevent verification failure in Dispose tests)
        _mockConnection.Setup(c => c.Dispose());
        _mockTransaction.Setup(t => t.Dispose());

        // Factory returns our mock connection
        _mockFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(_mockConnection.Object);
    }

    // --- CONSTRUCTOR / CREATE VIA EXECUTOR TESTS ---

    [Fact]
    public void Constructor_ShouldOpenConnectionAndBeginTransaction()
    {
        // Arrange
        _mockConnection.Setup(c => c.Open()).Verifiable();

        // Use the executor to create transaction (DapperTransaction is internal)
        var executor = new TestExecutor(_mockFactory.Object, _mockDataAccess.Object);

        // Act
        using var transaction = executor.BeginTransaction("cs");

        // Assert
        _mockConnection.Verify(c => c.Open(), Times.Once, "Connection must be opened if closed.");
        _mockConnection.Verify(c => c.BeginTransaction(), Times.Once, "Transaction must be started.");
    }

    [Fact]
    public void Constructor_ShouldNotOpenConnection_IfAlreadyOpen()
    {
        // Arrange
        _mockConnection.SetupGet(c => c.State).Returns(ConnectionState.Open);

        var executor = new TestExecutor(_mockFactory.Object, _mockDataAccess.Object);

        // Act
        using var transaction = executor.BeginTransaction("cs");

        // Assert
        _mockConnection.Verify(c => c.Open(), Times.Never, "Connection should not be opened if already open.");
        _mockConnection.Verify(c => c.BeginTransaction(), Times.Once, "Transaction must still be started.");
    }

    // --- EXECUTION TESTS ---

    [Fact]
    public async Task ExecuteQueryAsync_ShouldCallDataAccessQueryFirstWithTransaction()
    {
        // Arrange
        const string sql = "SELECT 1";
        var parameters = new Dictionary<string, object?> { { "id", 1 } };
        var expectedResult = new Dictionary<string, object?> { { "result", 42 } };

        var executor = new TestExecutor(_mockFactory.Object, _mockDataAccess.Object);
        using var transaction = executor.BeginTransaction("cs");

        _mockDataAccess
            .Setup(d => d.QueryFirstAsync(_mockConnection.Object, sql, parameters, _mockTransaction.Object))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await transaction.ExecuteQueryAsync(sql, parameters);

        // Assert
        Assert.Equal(42, result["result"]);
        _mockDataAccess.Verify(c => c.QueryFirstAsync(_mockConnection.Object, sql, parameters, _mockTransaction.Object), Times.Once);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_ShouldCallDataAccessExecuteWithTransaction()
    {
        // Arrange
        const string sql = "UPDATE 1";
        var parameters = new Dictionary<string, object?> { { "name", "test" } };

        var executor = new TestExecutor(_mockFactory.Object, _mockDataAccess.Object);
        using var transaction = executor.BeginTransaction("cs");

        _mockDataAccess
            .Setup(d => d.ExecuteAsync(_mockConnection.Object, sql, parameters, _mockTransaction.Object))
            .ReturnsAsync(1);

        // Act
        var affectedRows = await transaction.ExecuteNonQueryAsync(sql, parameters);

        // Assert
        Assert.Equal(1, affectedRows);
        _mockDataAccess.Verify(c => c.ExecuteAsync(_mockConnection.Object, sql, parameters, _mockTransaction.Object), Times.Once);
    }

    // --- COMMIT / ROLLBACK TESTS ---

    [Fact]
    public void Commit_ShouldCallSynchronousCommit()
    {
        // Arrange
        var executor = new TestExecutor(_mockFactory.Object, _mockDataAccess.Object);
        using var transaction = executor.BeginTransaction("cs");

        _mockTransaction.Setup(t => t.Commit()).Verifiable();

        // Act
        transaction.Commit();

        // Assert
        _mockTransaction.Verify(t => t.Commit(), Times.Once);
    }

    [Fact]
    public void Rollback_ShouldCallSynchronousRollback()
    {
        // Arrange
        var executor = new TestExecutor(_mockFactory.Object, _mockDataAccess.Object);
        using var transaction = executor.BeginTransaction("cs");

        _mockTransaction.Setup(t => t.Rollback()).Verifiable();

        // Act
        transaction.Rollback();

        // Assert
        _mockTransaction.Verify(t => t.Rollback(), Times.Once);
    }

    [Fact]
    public void Commit_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        // Arrange
        var executor = new TestExecutor(_mockFactory.Object, _mockDataAccess.Object);
        var transaction = executor.BeginTransaction("cs");
        transaction.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => transaction.Commit());
    }

    // --- DISPOSE TESTS ---

    [Fact]
    public void Dispose_ShouldCallDisposeOnTransactionAndConnection()
    {
        // Arrange
        var executor = new TestExecutor(_mockFactory.Object, _mockDataAccess.Object);
        var transaction = executor.BeginTransaction("cs");

        // Setup Dispose to be verifiable
        _mockTransaction.Setup(t => t.Dispose()).Verifiable();
        _mockConnection.Setup(c => c.Dispose()).Verifiable();

        // Act
        transaction.Dispose();

        // Assert
        _mockTransaction.Verify(t => t.Dispose(), Times.Once, "IDbTransaction must be disposed.");
        _mockConnection.Verify(c => c.Dispose(), Times.Once, "IDbConnection must be disposed.");
    }

    [Fact]
    public void Dispose_ShouldNotThrow_IfTransactionDisposeFails()
    {
        // Arrange
        var executor = new TestExecutor(_mockFactory.Object, _mockDataAccess.Object);
        var transaction = executor.BeginTransaction("cs");

        // Setup transaction dispose to throw, but connection dispose to succeed
        _mockTransaction.Setup(t => t.Dispose()).Throws(new Exception("Transaction cleanup failed!"));
        _mockConnection.Setup(c => c.Dispose()).Verifiable();

        // Act & Assert
        var ex = Record.Exception(() => transaction.Dispose());
        Assert.Null(ex);

        // Assert that connection disposal was still attempted
        _mockConnection.Verify(c => c.Dispose(), Times.Once);
    }

    private class TestExecutor : DapperQueryExecutor
    {
        public TestExecutor(IConnectionFactory factory, IDataAccess dataAccess) : base(factory, dataAccess) { }
        public override DatabaseType Type => DatabaseType.PostgreSQL;
    }
}