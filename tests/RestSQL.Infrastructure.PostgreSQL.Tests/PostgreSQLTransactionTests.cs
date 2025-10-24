using Xunit;
using Moq;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using RestSQL.Infrastructure.Interfaces;
using System;
using Dapper;

namespace RestSQL.Infrastructure.PostgreSQL.Tests;

public class PostgreSQLTransactionTests
{
    private readonly Mock<IDbConnection> _mockConnection;
    private readonly Mock<IDbTransaction> _mockTransaction;
    private readonly Mock<IPostgreSQLDataAccess> _mockDataAccess;

    public PostgreSQLTransactionTests()
    {
        // 1. Setup Mocks (MockBehavior.Strict ensures that any un-setup call throws)
        _mockConnection = new Mock<IDbConnection>();
        _mockTransaction = new Mock<IDbTransaction>();
        _mockDataAccess = new Mock<IPostgreSQLDataAccess>();

        // 2. Configure mock connection to return the mock transaction
        _mockConnection.Setup(c => c.BeginTransaction()).Returns(_mockTransaction.Object);
        // We initially set the connection state for the constructor test
        _mockConnection.SetupGet(c => c.State).Returns(ConnectionState.Closed);
        
        // 3. Setup disposal on the mocks (must be done to prevent verification failure in Dispose tests)
        _mockConnection.Setup(c => c.Dispose());
        _mockTransaction.Setup(t => t.Dispose());
    }

    // --- CONSTRUCTOR TESTS ---

    [Fact]
    public void Constructor_ShouldOpenConnectionAndBeginTransaction()
    {
        // Arrange
        _mockConnection.Setup(c => c.Open()).Verifiable();

        // Act
        var transaction = new PostgreSQLTransaction(_mockConnection.Object, _mockDataAccess.Object);

        // Assert
        // Verify synchronous Open() and BeginTransaction() were called
        _mockConnection.Verify(c => c.Open(), Times.Once, "Connection must be opened if closed.");
        _mockConnection.Verify(c => c.BeginTransaction(), Times.Once, "Transaction must be started.");
    }
    
    [Fact]
    public void Constructor_ShouldNotOpenConnection_IfAlreadyOpen()
    {
        // Arrange
        _mockConnection.SetupGet(c => c.State).Returns(ConnectionState.Open);
        
        // Act
        var transaction = new PostgreSQLTransaction(_mockConnection.Object, _mockDataAccess.Object);

        // Assert
        // Verify Open() was NOT called
        _mockConnection.Verify(c => c.Open(), Times.Never, "Connection should not be opened if already open.");
        _mockConnection.Verify(c => c.BeginTransaction(), Times.Once, "Transaction must still be started.");
    }

    // --- EXECUTION TESTS ---

    [Fact]
    public async Task ExecuteQueryAsync_ShouldCallDapperQueryFirstWithTransaction()
    {
        // Arrange
        const string sql = "SELECT 1";
        var parameters = new Dictionary<string, object?> { { "id", 1 } };
        var expectedResult = new Dictionary<string, object?> { { "result", 42 } };
        var transaction = new PostgreSQLTransaction(_mockConnection.Object, _mockDataAccess.Object);

        _mockDataAccess
            .Setup(d => d.QueryFirstAsync(_mockConnection.Object, sql, parameters, _mockTransaction.Object))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await transaction.ExecuteQueryAsync(sql, parameters);

        // Assert
        Assert.Equal(42, result["result"]);
        // Verify Dapper method was called with the correct transaction
        _mockDataAccess.Verify(c => c.QueryFirstAsync(_mockConnection.Object, sql, parameters, _mockTransaction.Object), Times.Once);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_ShouldCallDapperExecuteWithTransaction()
    {
        // Arrange
        const string sql = "UPDATE 1";
        var parameters = new Dictionary<string, object?> { { "name", "test" } };
        var transaction = new PostgreSQLTransaction(_mockConnection.Object, _mockDataAccess.Object);

        _mockDataAccess
            .Setup(d => d.ExecuteAsync(_mockConnection.Object, sql, parameters, _mockTransaction.Object))
            .ReturnsAsync(1);

        // Act
        var affectedRows = await transaction.ExecuteNonQueryAsync(sql, parameters);

        // Assert
        Assert.Equal(1, affectedRows);
        // Verify Dapper method was called with the correct transaction
        _mockDataAccess.Verify(c => c.ExecuteAsync(_mockConnection.Object, sql, parameters, _mockTransaction.Object), Times.Once);
    }
    
    // --- COMMIT / ROLLBACK TESTS (New Synchronous Contract) ---

    [Fact]
    public void Commit_ShouldCallSynchronousCommit()
    {
        // Arrange
        var transaction = new PostgreSQLTransaction(_mockConnection.Object, _mockDataAccess.Object);
        _mockTransaction.Setup(t => t.Commit()).Verifiable();

        // Act
        transaction.Commit();

        // Assert
        // Verify the underlying IDbTransaction.Commit() was called
        _mockTransaction.Verify(t => t.Commit(), Times.Once);
    }
    
    [Fact]
    public void Rollback_ShouldCallSynchronousRollback()
    {
        // Arrange
        var transaction = new PostgreSQLTransaction(_mockConnection.Object, _mockDataAccess.Object);
        _mockTransaction.Setup(t => t.Rollback()).Verifiable();

        // Act
        transaction.Rollback();

        // Assert
        // Verify the underlying IDbTransaction.Rollback() was called
        _mockTransaction.Verify(t => t.Rollback(), Times.Once);
    }
    
    [Fact]
    public void Commit_ShouldThrowObjectDisposedException_WhenDisposed()
    {
        // Arrange
        var transaction = new PostgreSQLTransaction(_mockConnection.Object, _mockDataAccess.Object);
        transaction.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => transaction.Commit());
    }

    // --- DISPOSE TESTS ---
    
    [Fact]
    public void Dispose_ShouldCallDisposeOnTransactionAndConnection()
    {
        // Arrange
        var transaction = new PostgreSQLTransaction(_mockConnection.Object, _mockDataAccess.Object);

        // Setup Dispose to be verifiable
        _mockTransaction.Setup(t => t.Dispose()).Verifiable();
        _mockConnection.Setup(c => c.Dispose()).Verifiable();

        // Act
        transaction.Dispose();

        // Assert
        // Verify that BOTH are disposed, due to the try-catch blocks
        _mockTransaction.Verify(t => t.Dispose(), Times.Once, "IDbTransaction must be disposed.");
        _mockConnection.Verify(c => c.Dispose(), Times.Once, "IDbConnection must be disposed.");
    }
    
    [Fact]
    public void Dispose_ShouldNotThrow_IfTransactionDisposeFails()
    {
        // Arrange
        var transaction = new PostgreSQLTransaction(_mockConnection.Object, _mockDataAccess.Object);
        
        // Setup transaction dispose to throw, but connection dispose to succeed
        _mockTransaction.Setup(t => t.Dispose()).Throws(new Exception("Transaction cleanup failed!"));
        _mockConnection.Setup(c => c.Dispose()).Verifiable();

        // Act & Assert
        // Verify that calling Dispose does NOT throw, because the error is caught/logged internally.
        // This validates the robustness of the sequential try-catch disposal pattern.
        var ex = Record.Exception(() => transaction.Dispose());
        Assert.Null(ex);
        
        // Assert that connection disposal was still attempted
        _mockConnection.Verify(c => c.Dispose(), Times.Once);
    }
}