using Microsoft.AspNetCore.Http;
using RestSQL.Application.Interfaces;
using Moq;
using Microsoft.AspNetCore.Builder;
using RestSQL.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Routing;

namespace RestSQL.Tests;

public class EndpointMapperTests
{
    [Fact]
    public async Task MapEndpoints_ReturnsNotFound_WhenNoDataAndFlagIsTrue()
    {
        // Arrange
        var endpoint = new Domain.Endpoint
        {
            Path = "/test",
            Method = "GET",
            StatusCode = 200,
            StatusCodeOnEmptyResult = 404,
            OutputStructure = new Domain.OutputField
            {
                Type = Domain.OutputFieldType.Object,
                IsArray = true
            }
        };

        var endpointServiceMock = new Mock<IEndpointService>();
        endpointServiceMock
            .Setup(s => s.GetEndpointResultAsync(
                endpoint,
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<Stream>()))
            .ReturnsAsync(Domain.EndpointResult.Empty());

        using var server = CreateTestServer(endpoint, endpointServiceMock.Object);
        using var client = server.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task MapEndpoints_ReturnsConfiguredStatusCode_WhenNoDataAndFlagIsFalse()
    {
        // Arrange
        var endpoint = new Domain.Endpoint
        {
            Path = "/test",
            Method = "GET",
            StatusCode = 200,
            OutputStructure = new Domain.OutputField
            {
                Type = Domain.OutputFieldType.Object,
                IsArray = true
            }
        };

        var endpointServiceMock = new Mock<IEndpointService>();
        endpointServiceMock
            .Setup(s => s.GetEndpointResultAsync(
                endpoint,
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<Stream>()))
            .ReturnsAsync(Domain.EndpointResult.Empty());

        using var server = CreateTestServer(endpoint, endpointServiceMock.Object);
        using var client = server.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task MapEndpoints_ReturnsConfiguredStatusCode_WhenHasData()
    {
        // Arrange
        var endpoint = new Domain.Endpoint
        {
            Path = "/test",
            Method = "GET",
            StatusCode = 201,
            StatusCodeOnEmptyResult = 404,
            OutputStructure = new Domain.OutputField
            {
                Type = Domain.OutputFieldType.Object,
                IsArray = false
            }
        };

        var data = System.Text.Json.Nodes.JsonNode.Parse("""{"test":"value"}""");
        var endpointServiceMock = new Mock<IEndpointService>();
        endpointServiceMock
            .Setup(s => s.GetEndpointResultAsync(
                endpoint,
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<Stream>()))
            .ReturnsAsync(Domain.EndpointResult.Success(data));

        using var server = CreateTestServer(endpoint, endpointServiceMock.Object);
        using var client = server.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        Assert.Equal(StatusCodes.Status201Created, (int)response.StatusCode);
    }

    private static TestServer CreateTestServer(Domain.Endpoint endpoint, IEndpointService endpointService)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddSingleton(endpointService);
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    var config = new Domain.Config
                    {
                        Endpoints = new[] { endpoint },
                        Connections = new Dictionary<string, Connection>()
                    };

                    EndpointMapper.MapEndpoints(endpoints, config);
                });
            });

        return new TestServer(builder);
    }
}