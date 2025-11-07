using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using RestSQL.Application.Interfaces;
using Moq;

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
            ReturnNotFoundOnEmptyResult = true,
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

        var app = CreateTestApplication(endpoint, endpointServiceMock.Object);
        var context = new DefaultHttpContext();

        // Act
        var handler = app.RequestDelegate;
        await handler(context);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
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
            ReturnNotFoundOnEmptyResult = false,
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

        var app = CreateTestApplication(endpoint, endpointServiceMock.Object);
        var context = new DefaultHttpContext();

        // Act
        var handler = app.RequestDelegate;
        await handler(context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
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
            ReturnNotFoundOnEmptyResult = true, // Should be ignored when there is data
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

        var app = CreateTestApplication(endpoint, endpointServiceMock.Object);
        var context = new DefaultHttpContext();

        // Act
        var handler = app.RequestDelegate;
        await handler(context);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
    }

    private static WebApplication CreateTestApplication(Domain.Endpoint endpoint, IEndpointService endpointService)
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        
        var config = new Domain.Config
        {
            Endpoints = new[] { endpoint }
        };

        EndpointMapper.MapEndpoints(app, config);
        
        // Replace the registered service with our mock
        app.Services = new ServiceCollection()
            .AddSingleton(endpointService)
            .BuildServiceProvider();

        return app;
    }
}