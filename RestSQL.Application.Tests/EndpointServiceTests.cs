using System.Text.Json.Nodes;
using Moq;
using RestSQL.Application.Interfaces;
using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Application.Tests;

public class EndpointServiceTests
{
    private readonly Mock<IQueryDispatcher> _queryDispatcherMock;
    private readonly Mock<IResultAggregator> _resultAggregatorMock;
    private readonly EndpointService _service;

    public EndpointServiceTests()
    {
        _queryDispatcherMock = new Mock<IQueryDispatcher>();
        _resultAggregatorMock = new Mock<IResultAggregator>();
        _service = new EndpointService(_queryDispatcherMock.Object, _resultAggregatorMock.Object);
    }

    [Fact]
    public async Task GetEndpointResult_ShouldCallDispatcherAndAggregator()
    {
        // Arrange
        var endpoint = new Endpoint(
            Path: "/test",
            UrlParameters: new(),
            SqlQueries: new Dictionary<string, SqlQuery>
            {
                ["query1"] = new("conn1", "SELECT * FROM table1"),
                ["query2"] = new("conn2", "SELECT * FROM table2")
            },
            OutputStructure: new(
                Type: OutputFieldType.Object,
                IsArray: false,
                Name: null,
                ColumnName: null,
                QueryName: "query1",
                LinkColumn: null,
                Fields: null
            )
        );

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
        var result = await _service.GetEndpointResultAsync(endpoint, parameters);

        // Assert
        Assert.Equal(expectedJson, result);
        _queryDispatcherMock.VerifyAll();
        _resultAggregatorMock.VerifyAll();
    }

    [Fact]
    public async Task GetEndpointResult_ShouldHandleEmptySqlQueries()
    {
        // Arrange
        var endpoint = new Endpoint(
            Path: "/empty",
            UrlParameters: new(),
            SqlQueries: new(),
            OutputStructure: new(
                Type: OutputFieldType.Object,
                IsArray: false,
                Name: null,
                ColumnName: null,
                QueryName: null,
                LinkColumn: null,
                Fields: null
            )
        );

        var parameters = new Dictionary<string, object?>();

        _resultAggregatorMock
            .Setup(a => a.Aggregate(
                It.Is<IDictionary<string, IEnumerable<IDictionary<string, object?>>>>(d => d.Count == 0),
                endpoint.OutputStructure))
            .Returns((JsonNode?)null);

        // Act
        var result = await _service.GetEndpointResultAsync(endpoint, parameters);

        // Assert
        Assert.Null(result);
        _resultAggregatorMock.VerifyAll();
    }

    [Fact]
    public async Task GetEndpointResult_ShouldSupportSingleQuery()
    {
        // Arrange
        var endpoint = new Endpoint(
            Path: "/single",
            UrlParameters: new(),
            SqlQueries: new Dictionary<string, SqlQuery>
            {
                ["single"] = new("main", "SELECT 1")
            },
            OutputStructure: new(
                Type: OutputFieldType.Object,
                IsArray: false,
                Name: null,
                ColumnName: null,
                QueryName: "single",
                LinkColumn: null,
                Fields: null
            )
        );

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
        var result = await _service.GetEndpointResultAsync(endpoint, parameters);

        // Assert
        Assert.Equal(expectedJson, result);
        _queryDispatcherMock.VerifyAll();
        _resultAggregatorMock.VerifyAll();
    }

    [Fact]
    public async Task GetEndpointResult_ShouldPropagateException_WhenQueryDispatcherThrows()
    {
        // Arrange
        var endpoint = new Endpoint(
            Path: "/error",
            UrlParameters: new(),
            SqlQueries: new Dictionary<string, SqlQuery>
            {
                ["query1"] = new("conn", "SELECT fail")
            },
            OutputStructure: new(
                Type: OutputFieldType.Object,
                IsArray: false,
                Name: null,
                ColumnName: null,
                QueryName: "query1",
                LinkColumn: null,
                Fields: null
            )
        );

        var parameters = new Dictionary<string, object?>();
        var exception = new InvalidOperationException("Database failed");

        _queryDispatcherMock
            .Setup(q => q.QueryAsync("conn", "SELECT fail", parameters))
            .ThrowsAsync(exception);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetEndpointResultAsync(endpoint, parameters));

        Assert.Equal("Database failed", ex.Message);
        _resultAggregatorMock.Verify(a => a.Aggregate(It.IsAny<IDictionary<string, IEnumerable<IDictionary<string, object?>>>>(), It.IsAny<OutputField>()), Times.Never);
    }
}