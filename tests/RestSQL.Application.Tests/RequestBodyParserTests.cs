using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Moq;

namespace RestSQL.Application.Tests;

public class RequestBodyParserTests
{
    private readonly RequestBodyParser _parser;
    private readonly Mock<ILogger<RequestBodyParser>> _loggerMock;

    public RequestBodyParserTests()
    {
        _loggerMock = new Mock<ILogger<RequestBodyParser>>();
        _parser = new RequestBodyParser(_loggerMock.Object);
    }

    // Helper to create a MemoryStream from a string
    private static Stream CreateStream(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    [Fact]
    public async Task ReadAndParseJsonStream_ShouldReturnNulls_WhenStreamIsNull()
    {
        // Act
        var result = await _parser.ReadAndParseJsonStreamAsync(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadAndParseJsonStream_ShouldReturnNullNodeAndEmptyString_WhenStreamIsEmpty()
    {
        // Arrange
        using var stream = CreateStream("");

        // Act
        var result = await _parser.ReadAndParseJsonStreamAsync(stream);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadAndParseJsonStream_ShouldParseValidJsonObject()
    {
        // Arrange
        const string jsonContent = """{"id":123, "name":"Test"}""";
        using var stream = CreateStream(jsonContent);

        // Act
        var result = await _parser.ReadAndParseJsonStreamAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<JsonObject>(result);

        // Verify a value
        var jsonObject = result as JsonObject;
        Assert.Equal(123, jsonObject?["id"]?.GetValue<int>());
    }

    [Fact]
    public async Task ReadAndParseJsonStream_ShouldParseValidJsonArray()
    {
        // Arrange
        const string jsonContent = """[1, 2, 3]""";
        using var stream = CreateStream(jsonContent);

        // Act
        var result = await _parser.ReadAndParseJsonStreamAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<JsonArray>(result);

        // Verify array count
        var jsonArray = result as JsonArray;
        Assert.Equal(3, jsonArray?.Count);
    }

    [Fact]
    public async Task ReadAndParseJsonStream_ShouldReturnNullNode_WhenJsonIsInvalid()
    {
        // Arrange
        // Invalid JSON: missing closing brace
        const string invalidJson = """{"id":123, "name":"Test""";
        using var stream = CreateStream(invalidJson);

        // Act
        var result = await _parser.ReadAndParseJsonStreamAsync(stream);

        // Assert
        Assert.Null(result); // JsonNode should be null due to the catch(JsonException)
    }
}