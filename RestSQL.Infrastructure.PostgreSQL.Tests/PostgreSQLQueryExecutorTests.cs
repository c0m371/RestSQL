using Xunit;
using Moq;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using RestSQL.Infrastructure.Interfaces;
using RestSQL.Domain;
using System;
using Dapper;
using System.Dynamic;

namespace RestSQL.Infrastructure.PostgreSQL.Tests;

public class PostgreSQLQueryExecutorTests
{
    private readonly Mock<IPostgreSQLConnectionFactory> _mockFactory;
    private readonly Mock<IDbConnection> _mockConnection;
    private readonly Mock<IPostgreSQLDataAccess> _mockDataAccess;
    private readonly PostgreSQLQueryExecutor _executor;
    
    public PostgreSQLQueryExecutorTests()
    {
        // 1. Setup Mocks
        _mockFactory = new Mock<IPostgreSQLConnectionFactory>();
        _mockConnection = new Mock<IDbConnection>();
        _mockDataAccess = new Mock<IPostgreSQLDataAccess>();

        // 2. Configure the Factory to return our Mock Connection
        _mockFactory
            .Setup(f => f.CreatePostgreSQLConnection(It.IsAny<string>()))
            .Returns(_mockConnection.Object);

        // 3. Instantiate the Executor with the Mock Factory
        _executor = new PostgreSQLQueryExecutor(_mockFactory.Object, _mockDataAccess.Object);
        
        // Setup mock connection disposal for QueryAsync test
        _mockConnection.Setup(c => c.Dispose());
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
        _mockFactory.Verify(f => f.CreatePostgreSQLConnection(connString), Times.Once, "Factory must be called to create connection.");
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
        
        // For this test, we must assume that the internal PostgreSQLTransaction 
        // constructor correctly performs the Open() and BeginTransaction() calls on the connection.
        
        // We verify that the connection is created via the factory.
        
        // Act
        // This call executes the real PostgreSQLTransaction constructor logic on our mock connection.
        var transaction = _executor.BeginTransaction(connString);

        // Assert
        Assert.NotNull(transaction);
        Assert.IsAssignableFrom<ITransaction>(transaction);
        
        // Verify dependency calls
        _mockFactory.Verify(f => f.CreatePostgreSQLConnection(connString), Times.Once, "Factory must be called to create connection.");
        
        // Verify the steps that the PostgreSQLTransaction constructor should have executed on the mock connection:
        _mockConnection.VerifyGet(c => c.State, Times.AtLeastOnce, "Connection state must be checked.");
        _mockConnection.Verify(c => c.Open(), Times.Once, "Connection must be opened by PostgreSQLTransaction.");
        _mockConnection.Verify(c => c.BeginTransaction(), Times.Once, "Transaction must be started by PostgreSQLTransaction.");
    }
}