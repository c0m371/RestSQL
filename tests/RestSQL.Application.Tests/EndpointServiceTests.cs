using Microsoft.Extensions.Logging;
using Moq;
using RestSQL.Application.Interfaces;
using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RestSQL.Application.Tests;

public class EndpointServiceTests
{
    private readonly Mock<IQueryDispatcher> _queryDispatcherMock;
    private readonly Mock<IResultAggregator> _resultAggregatorMock;
    private readonly Mock<IRequestBodyParser> _requestBodyParserMock;
    private readonly Mock<ILogger<EndpointService>> _loggerMock;
    private readonly EndpointService _service;

    public EndpointServiceTests()
    {
        _queryDispatcherMock = new Mock<IQueryDispatcher>();
        _resultAggregatorMock = new Mock<IResultAggregator>();
        _requestBodyParserMock = new Mock<IRequestBodyParser>();
        _loggerMock = new Mock<ILogger<EndpointService>>();
        _service = new EndpointService(_queryDispatcherMock.Object, _resultAggregatorMock.Object, _requestBodyParserMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetEndpointResult_ShouldCallDispatcherAndAggregator()
    {
        // Arrange
        var endpoint = new Endpoint
        {
            Path = "/test",
            Method = "GET",
            StatusCode = 200,
            SqlQueries = new Dictionary<string, SqlQuery>
            {
                ["query1"] = new SqlQuery { ConnectionName = "conn1", Sql = "SELECT * FROM table1" },
                ["query2"] = new SqlQuery { ConnectionName = "conn2", Sql = "SELECT * FROM table2" }
            },
            OutputStructure = new OutputField
            {
                Type = OutputFieldType.Object,
                IsArray = false,
                QueryName = "query1"
            }
        };

        var parameters = new Dictionary<string, object?> { ["id"] = 123 };

        var query1Result = new List<Dictionary<string, object?>>
        {
            new() { ["name"] = "Alice" }
        };

        var query2Result = new List<Dictionary<string, object?>>
        {
            new() { ["age"] = 30 }
        };

        _queryDispatcherMock
            .Setup(q => q.QueryAsync("conn1", "SELECT * FROM table1", parameters))
            .ReturnsAsync(query1Result);

        _queryDispatcherMock
            .Setup(q => q.QueryAsync("conn2", "SELECT * FROM table2", parameters))
            .ReturnsAsync(query2Result);

        var expectedJson = JsonNode.Parse("""{"test":"ok"}""");

        _resultAggregatorMock
            .Setup(a => a.Aggregate(
                It.Is<IDictionary<string, IEnumerable<IDictionary<string, object?>>>>(
                    d => d.ContainsKey("query1") && d.ContainsKey("query2")
                ),
                endpoint.OutputStructure))
            .Returns(expectedJson);

        // Act
        var result = await _service.GetEndpointResultAsync(endpoint, parameters, null);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasData);
        Assert.Equal(expectedJson, result.Data);
        _queryDispatcherMock.VerifyAll();
        _resultAggregatorMock.VerifyAll();
    }

    [Fact]
    public async Task GetEndpointResult_ShouldHandleEmptySqlQueries()
    {
        // Arrange
        var endpoint = new Endpoint
        {
            Path = "/empty",
            Method = "GET",
            StatusCode = 200,
            SqlQueries = new Dictionary<string, SqlQuery>(),
            OutputStructure = new OutputField
            {
                Type = OutputFieldType.Object,
                IsArray = false
            }
        };

        var parameters = new Dictionary<string, object?>();

        _resultAggregatorMock
            .Setup(a => a.Aggregate(
                It.Is<IDictionary<string, IEnumerable<IDictionary<string, object?>>>>(d => d.Count == 0),
                endpoint.OutputStructure))
            .Returns((JsonNode?)null);

        // Act
        var result = await _service.GetEndpointResultAsync(endpoint, parameters, null);

        // Assert
        Assert.Null(result.Data);
        Assert.False(result.HasData);
        _resultAggregatorMock.VerifyAll();
    }

    [Fact]
    public async Task GetEndpointResult_ShouldSupportSingleQuery()
    {
        // Arrange
        var endpoint = new Endpoint
        {
            Path = "/single",
            Method = "GET",
            StatusCode = 200,
            SqlQueries = new Dictionary<string, SqlQuery>
            {
                ["single"] = new SqlQuery { ConnectionName = "main", Sql = "SELECT 1" }
            },
            OutputStructure = new OutputField
            {
                Type = OutputFieldType.Object,
                IsArray = false,
                QueryName = "single"
            }
        };

        var parameters = new Dictionary<string, object?>();
        var singleResult = new List<Dictionary<string, object?>> { new() { ["value"] = 1 } };

        _queryDispatcherMock
            .Setup(q => q.QueryAsync("main", "SELECT 1", parameters))
            .ReturnsAsync(singleResult);

        var expectedJson = JsonNode.Parse("""{"value":1}""");

        _resultAggregatorMock
            .Setup(a => a.Aggregate(
                It.Is<IDictionary<string, IEnumerable<IDictionary<string, object?>>>>(
                    d => d.ContainsKey("single")
                ),
                endpoint.OutputStructure))
            .Returns(expectedJson);

        // Act
        var result = await _service.GetEndpointResultAsync(endpoint, parameters, null);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasData);
        Assert.Equal(expectedJson, result.Data);
        _queryDispatcherMock.VerifyAll();
        _resultAggregatorMock.VerifyAll();
    }

    [Fact]
    public async Task GetEndpointResult_ShouldPropagateException_WhenQueryDispatcherThrows()
    {
        // Arrange
        var endpoint = new Endpoint
        {
            Path = "/error",
            Method = "GET",
            StatusCode = 200,
            SqlQueries = new Dictionary<string, SqlQuery>
            {
                ["query1"] = new SqlQuery { ConnectionName = "conn", Sql = "SELECT fail" }
            },
            OutputStructure = new OutputField
            {
                Type = OutputFieldType.Object,
                IsArray = false,
                QueryName = "query1"
            }
        };

        var parameters = new Dictionary<string, object?>();
        var exception = new InvalidOperationException("Database failed");

        _queryDispatcherMock
            .Setup(q => q.QueryAsync("conn", "SELECT fail", parameters))
            .ThrowsAsync(exception);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetEndpointResultAsync(endpoint, parameters, null));

        Assert.Equal("Database failed", ex.Message);
        _resultAggregatorMock.Verify(a =>
            a.Aggregate(It.IsAny<IDictionary<string, IEnumerable<IDictionary<string, object?>>>>(), It.IsAny<OutputField>()),
            Times.Never);
    }

    [Fact]
    public async Task GetEndpointResult_ShouldExecuteWriteOperationAndCommitTransaction()
    {
        // Arrange
        var endpoint = new Endpoint
        {
            // Note: SqlQueries is empty so we can focus solely on WriteOperations
            Path = "insert",
            Method = "POST",
            StatusCode = 201,
            SqlQueries = new Dictionary<string, SqlQuery>(),
            WriteOperations = new List<WriteOperation>
            {
                new() { ConnectionName = "conn1", Sql = "INSERT 1", BodyType = WriteOperationBodyType.None }
            },
            OutputStructure = new OutputField { Type = OutputFieldType.Object, IsArray = false }
        };
        var parameters = new Dictionary<string, object?> { ["p1"] = "val1" };

        // Mock the ITransaction and its methods
        var transactionMock = new Mock<ITransaction>();

        // 1. Setup BeginTransactions
        _queryDispatcherMock
            .Setup(q => q.BeginTransaction("conn1"))
            .Returns(transactionMock.Object);

        // 2. Setup ExecuteNonQueryAsync (the write operation itself)
        transactionMock
            .Setup(t => t.ExecuteNonQueryAsync("INSERT 1", It.Is<IDictionary<string, object?>>(d => d["p1"]!.Equals("val1"))))
            .Returns(Task.FromResult(1))
            .Verifiable();

        // Act
        var result = await _service.GetEndpointResultAsync(endpoint, parameters, null);

        // Assert

        // Verify transaction flow: COMMIT and DISPOSE must be called
        transactionMock.Verify(t => t.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object?>>()), Times.Once);
        transactionMock.Verify(t => t.Commit(), Times.Once, "Commit must be called on success.");
        transactionMock.Verify(t => t.Dispose(), Times.Once, "Dispose must be called in finally.");
        transactionMock.Verify(t => t.Rollback(), Times.Never, "Rollback must NOT be called on success.");

        _queryDispatcherMock.Verify(q => q.BeginTransaction("conn1"), Times.Once);
    }

    [Fact]
    public async Task GetEndpointResult_ShouldRollbackAndDispose_WhenWriteOperationFails()
    {
        // Arrange
        var endpoint = new Endpoint
        {
            SqlQueries = new Dictionary<string, SqlQuery>(),
            Path = "insert",
            Method = "POST",
            StatusCode = 201,
            WriteOperations = new List<WriteOperation>
            {
                new() { ConnectionName = "conn1", Sql = "INSERT fail", BodyType = WriteOperationBodyType.None }
            },
            OutputStructure = new OutputField { Type = OutputFieldType.Object, IsArray = false }
        };
        var parameters = new Dictionary<string, object?> { ["p1"] = "val1" };
        var expectedException = new InvalidOperationException("Write operation failed.");

        var transactionMock = new Mock<ITransaction>();
        _queryDispatcherMock.Setup(q => q.BeginTransaction("conn1")).Returns(transactionMock.Object);

        // 1. Setup ExecuteNonQueryAsync to throw an exception
        transactionMock
            .Setup(t => t.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object?>>()))
            .ThrowsAsync(expectedException);

        // 2. Setup Rollback and Dispose to complete normally
        transactionMock.Setup(t => t.Rollback());
        transactionMock.Setup(t => t.Dispose());

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetEndpointResultAsync(endpoint, parameters, null));

        Assert.Equal(expectedException.Message, ex.Message);

        // Verify transaction flow: ROLLBACK and DISPOSE must be called
        transactionMock.Verify(t => t.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object?>>()), Times.Once);
        transactionMock.Verify(t => t.Commit(), Times.Never, "Commit must NOT be called on failure.");
        transactionMock.Verify(t => t.Rollback(), Times.Once, "Rollback must be called on failure.");
        transactionMock.Verify(t => t.Dispose(), Times.Once, "Dispose must be called in finally.");

        // Verify no queries were run if write fails
        _queryDispatcherMock.Verify(q => q.QueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object?>>()), Times.Never);
    }

    [Fact]
    public async Task GetEndpointResult_ShouldParseBodyAndAddValueParameter()
    {
        // Arrange
        var rawParamName = "body";
        var endpoint = new Endpoint
        {
            Path = "insert",
            Method = "POST",
            StatusCode = 201,
            SqlQueries = new Dictionary<string, SqlQuery>(),
            WriteOperations = new List<WriteOperation>
            {
                new() { ConnectionName = "conn1", Sql = "INSERT @body", BodyType = WriteOperationBodyType.Value, ValueParameterName = rawParamName }
            },
            OutputStructure = new OutputField { Type = OutputFieldType.Object, IsArray = false }
        };
        var parameters = new Dictionary<string, object?>();
        var body = "\"value\"";
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

        // Mock parser to return a JsonObject (the Raw type handles any JsonNode)
        JsonNode? parsedBody = null;
        try
        {
            parsedBody = JsonNode.Parse(bodyStream);
        }
        catch (JsonException)
        { }

        _requestBodyParserMock
            .Setup(p => p.ReadAndParseJsonStreamAsync(bodyStream))
            .ReturnsAsync(parsedBody)
            .Verifiable();

        var transactionMock = new Mock<ITransaction>();
        _queryDispatcherMock.Setup(q => q.BeginTransaction(It.IsAny<string>())).Returns(transactionMock.Object);

        // Setup ExecuteNonQueryAsync to capture the final parameters
        IDictionary<string, object?> finalParameters = null!;
        transactionMock
            .Setup(t => t.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object?>>()))
            .Callback<string, IDictionary<string, object?>>((s, p) => finalParameters = p) // Capture the dictionary
            .Returns(Task.FromResult(1));

        // Act
        await _service.GetEndpointResultAsync(endpoint, parameters, bodyStream);

        // Assert
        _requestBodyParserMock.VerifyAll();

        // Verify the parameter dictionary contains the raw body under the specified name
        Assert.NotNull(finalParameters);
        Assert.True(finalParameters.ContainsKey(rawParamName));
        // Check that the value is the correct JsonNode (GetValue<object?> wraps the JsonNode value)
        var bodyValue = finalParameters[rawParamName];
        Assert.NotNull(bodyValue);
        Assert.IsType<string>(bodyValue);
    }

    [Fact]
    public async Task GetEndpointResult_ShouldParseBodyAndMergeObjectParameters()
    {
        // Arrange
        var endpoint = new Endpoint
        {
            Path = "insert",
            Method = "POST",
            StatusCode = 201,
            SqlQueries = new Dictionary<string, SqlQuery>(),
            WriteOperations = new List<WriteOperation>
            {
                new() { ConnectionName = "conn1", Sql = "INSERT @val", BodyType = WriteOperationBodyType.Object }
            },
            OutputStructure = new OutputField { Type = OutputFieldType.Object, IsArray = false }
        };
        // Initial parameters will be merged with body parameters
        var initialParameters = new Dictionary<string, object?> { ["existing"] = 100L };
        var body = """{"bodyKey1":"bodyValue1", "bodyKey2":200}""";
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

        // Mock parser to return a JsonObject
        var parsedBody = JsonNode.Parse(bodyStream) as JsonObject;
        _requestBodyParserMock
            .Setup(p => p.ReadAndParseJsonStreamAsync(It.IsAny<Stream>()))
            .ReturnsAsync(parsedBody)
            .Verifiable();

        var transactionMock = new Mock<ITransaction>();
        _queryDispatcherMock.Setup(q => q.BeginTransaction(It.IsAny<string>())).Returns(transactionMock.Object);

        // Setup ExecuteNonQueryAsync to capture the final parameters
        IDictionary<string, object?> finalParameters = null!;
        transactionMock
            .Setup(t => t.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object?>>()))
            .Callback<string, IDictionary<string, object?>>((s, p) => finalParameters = p)
            .Returns(Task.FromResult(1));

        // Act
        await _service.GetEndpointResultAsync(endpoint, initialParameters, bodyStream);

        // Assert
        Assert.NotNull(finalParameters);

        // Verify all parameters are merged (3: existing + bodyKey1 + bodyKey2)
        Assert.Equal(3, finalParameters.Count);
        Assert.True(finalParameters.ContainsKey("existing"));
        Assert.True(finalParameters.ContainsKey("bodyKey1"));
        Assert.True(finalParameters.ContainsKey("bodyKey2"));

        // Check merged values (values are JsonValue, checking their underlying CLR types)
        Assert.Equal(100L, finalParameters["existing"]);
        Assert.Equal("bodyValue1", finalParameters["bodyKey1"]);
        Assert.Equal(200L, finalParameters["bodyKey2"]);
    }

    [Fact]
    public async Task GetEndpointResult_ShouldCaptureAndPassOutput()
    {
        // Arrange
        var endpoint = new Endpoint
        {
            // Note: SqlQueries is empty so we can focus solely on WriteOperations
            Path = "insert",
            Method = "POST",
            StatusCode = 201,
            SqlQueries = new Dictionary<string, SqlQuery>(),
            WriteOperations = new List<WriteOperation>
            {
                new() {
                    ConnectionName = "conn1",
                    Sql = "INSERT 1",
                    BodyType = WriteOperationBodyType.None,
                    OutputCaptures =
                    [
                        new OutputCapture() { ColumnName = "Column1", ParameterName = "Param1" },
                        new OutputCapture() { ColumnName = "Column2", ParameterName = "Param2" }
                    ]
                },
                new() { ConnectionName = "conn1", Sql = "INSERT 2", BodyType = WriteOperationBodyType.None }
            },
            OutputStructure = new OutputField { Type = OutputFieldType.Object, IsArray = false }
        };
        var parameters = new Dictionary<string, object?> { ["existingParameter"] = "existingValue" };

        var transactionMock = new Mock<ITransaction>();

        _queryDispatcherMock
            .Setup(q => q.BeginTransaction("conn1"))
            .Returns(transactionMock.Object);

        transactionMock
            .Setup(t => t.ExecuteQueryAsync("INSERT 1", It.Is<IDictionary<string, object?>>(d => d["existingParameter"]!.Equals("existingValue"))))
            .Returns(Task.FromResult((IDictionary<string, object?>)new Dictionary<string, object?>()
            {
                ["Column1"] = "Value1",
                ["Column2"] = "Value2",
            }))
            .Verifiable();

        IDictionary<string, object?> inputParametersForOperation2 = null!;
        transactionMock
            .Setup(t => t.ExecuteNonQueryAsync("INSERT 2", It.IsAny<IDictionary<string, object?>>()))
            .Callback<string, IDictionary<string, object?>>((s, p) => inputParametersForOperation2 = p)
            .Returns(Task.FromResult(1))
            .Verifiable();

        // Act
        var result = await _service.GetEndpointResultAsync(endpoint, parameters, null);

        // Assert

        // Verify output of operation 1 was merged (3: existing + Param1 + Param2)
        Assert.Equal(3, inputParametersForOperation2.Count);
        Assert.True(inputParametersForOperation2.ContainsKey("existingParameter"));
        Assert.True(inputParametersForOperation2.ContainsKey("Param1"));
        Assert.True(inputParametersForOperation2.ContainsKey("Param2"));

        // Check merged values
        Assert.Equal("existingValue", inputParametersForOperation2["existingParameter"]);
        Assert.Equal("Value1", inputParametersForOperation2["Param1"]);
        Assert.Equal("Value2", inputParametersForOperation2["Param2"]);
    }

    [Fact]
    public async Task GetEndpointResult_ShouldReturnEmptyResultWhenOutputStructureNull()
    {
        // Arrange
        var endpoint = new Endpoint
        {
            // Note: SqlQueries is empty so we can focus solely on WriteOperations
            Path = "insert",
            Method = "POST",
            StatusCode = 201,
            SqlQueries = new Dictionary<string, SqlQuery>(),
            WriteOperations = new List<WriteOperation>
            {
                new() {
                    ConnectionName = "conn1",
                    Sql = "INSERT 1",
                    BodyType = WriteOperationBodyType.None
                },
                new() { ConnectionName = "conn1", Sql = "INSERT 2", BodyType = WriteOperationBodyType.None }
            },
            OutputStructure = null
        };

        var transactionMock = new Mock<ITransaction>();

        _queryDispatcherMock
            .Setup(q => q.BeginTransaction("conn1"))
            .Returns(transactionMock.Object);

        transactionMock
            .Setup(t => t.ExecuteNonQueryAsync("INSERT 1", It.IsAny<IDictionary<string, object?>>()))
            .Returns(Task.FromResult(1))
            .Verifiable();

        transactionMock
            .Setup(t => t.ExecuteNonQueryAsync("INSERT 2", It.IsAny<IDictionary<string, object?>>()))
            .Returns(Task.FromResult(1))
            .Verifiable();

        // Act
        var result = await _service.GetEndpointResultAsync(endpoint, new Dictionary<string, object?>(), null);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Data);
        Assert.False(result.HasData);
        
        // Aggregator should not be called
        _resultAggregatorMock.Verify(a =>
            a.Aggregate(It.IsAny<IDictionary<string, IEnumerable<IDictionary<string, object?>>>>(), It.IsAny<OutputField>()),
            Times.Never);
    }

    [Fact]
    public async Task GetEndpointResult_ShouldReturnEmptyResult_WhenQueryReturnsEmptyArray()
    {
        // Arrange
        var endpoint = new Endpoint
        {
            Path = "/empty",
            Method = "GET",
            StatusCode = 200,
            SqlQueries = new Dictionary<string, SqlQuery>
            {
                ["query1"] = new SqlQuery { ConnectionName = "conn1", Sql = "SELECT * FROM empty" }
            },
            OutputStructure = new OutputField
            {
                Type = OutputFieldType.Object,
                IsArray = true,
                QueryName = "query1"
            }
        };

        var parameters = new Dictionary<string, object?>();
        var emptyResult = new List<Dictionary<string, object?>>();

        _queryDispatcherMock
            .Setup(q => q.QueryAsync("conn1", "SELECT * FROM empty", parameters))
            .ReturnsAsync(emptyResult);

        _resultAggregatorMock
            .Setup(a => a.Aggregate(
                It.Is<IDictionary<string, IEnumerable<IDictionary<string, object?>>>>(
                    d => d.ContainsKey("query1")
                ),
                endpoint.OutputStructure))
            .Returns(new JsonArray());

        // Act
        var result = await _service.GetEndpointResultAsync(endpoint, parameters, null);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.False(result.HasData);
        _queryDispatcherMock.VerifyAll();
        _resultAggregatorMock.VerifyAll();
    }

    [Fact]
    public async Task GetEndpointResult_ShouldReturnDataResult_WhenQueryReturnsData()
    {
        // Arrange
        var endpoint = new Endpoint
        {
            Path = "/data",
            Method = "GET",
            StatusCode = 200,
            SqlQueries = new Dictionary<string, SqlQuery>
            {
                ["query1"] = new SqlQuery { ConnectionName = "conn1", Sql = "SELECT * FROM data" }
            },
            OutputStructure = new OutputField
            {
                Type = OutputFieldType.Object,
                IsArray = true,
                QueryName = "query1"
            }
        };

        var parameters = new Dictionary<string, object?>();
        var queryResult = new List<Dictionary<string, object?>>
        {
            new() { ["id"] = 1, ["name"] = "Test" }
        };

        _queryDispatcherMock
            .Setup(q => q.QueryAsync("conn1", "SELECT * FROM data", parameters))
            .ReturnsAsync(queryResult);

        var expectedJson = new JsonArray();
        expectedJson.Add(JsonNode.Parse("""{"id":1,"name":"Test"}"""));

        _resultAggregatorMock
            .Setup(a => a.Aggregate(
                It.Is<IDictionary<string, IEnumerable<IDictionary<string, object?>>>>(
                    d => d.ContainsKey("query1")
                ),
                endpoint.OutputStructure))
            .Returns(expectedJson);

        // Act
        var result = await _service.GetEndpointResultAsync(endpoint, parameters, null);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.True(result.HasData);
        Assert.Equal(expectedJson, result.Data);
        _queryDispatcherMock.VerifyAll();
        _resultAggregatorMock.VerifyAll();
    }
}
