using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RestSQL.Application.Tests;


public class RequestBodyParserTests
{
    private readonly RequestBodyParser _parser;

    public RequestBodyParserTests()
    {
        _parser = new RequestBodyParser();
    }

    // Helper to create a MemoryStream from a string
    private static Stream CreateStream(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    [Fact]
    public void ReadAndParseJsonStream_ShouldReturnNulls_WhenStreamIsNull()
    {
        // Act
        var result = _parser.ReadAndParseJsonStream(null);

        // Assert
        Assert.Null(result.jsonValue);
        Assert.Null(result.stringValue);
    }

    [Fact]
    public void ReadAndParseJsonStream_ShouldReturnNullNodeAndEmptyString_WhenStreamIsEmpty()
    {
        // Arrange
        using var stream = CreateStream("");

        // Act
        var result = _parser.ReadAndParseJsonStream(stream);

        // Assert
        Assert.Null(result.jsonValue);
        Assert.Equal("", result.stringValue);
    }

    [Fact]
    public void ReadAndParseJsonStream_ShouldParseValidJsonObjectAndCaptureString()
    {
        // Arrange
        const string jsonContent = """{"id":123, "name":"Test"}""";
        using var stream = CreateStream(jsonContent);

        // Act
        var result = _parser.ReadAndParseJsonStream(stream);

        // Assert
        Assert.NotNull(result.jsonValue);
        Assert.IsType<JsonObject>(result.jsonValue);
        Assert.Equal(jsonContent, result.stringValue);

        // Verify a value
        var jsonObject = result.jsonValue as JsonObject;
        Assert.Equal(123, jsonObject?["id"]?.GetValue<int>());
    }

    [Fact]
    public void ReadAndParseJsonStream_ShouldParseValidJsonArrayAndCaptureString()
    {
        // Arrange
        const string jsonContent = """[1, 2, 3]""";
        using var stream = CreateStream(jsonContent);

        // Act
        var result = _parser.ReadAndParseJsonStream(stream);

        // Assert
        Assert.NotNull(result.jsonValue);
        Assert.IsType<JsonArray>(result.jsonValue);
        Assert.Equal(jsonContent, result.stringValue);

        // Verify array count
        var jsonArray = result.jsonValue as JsonArray;
        Assert.Equal(3, jsonArray?.Count);
    }

    [Fact]
    public void ReadAndParseJsonStream_ShouldReturnNullNode_WhenJsonIsInvalid()
    {
        // Arrange
        // Invalid JSON: missing closing brace
        const string invalidJson = """{"id":123, "name":"Test""";
        using var stream = CreateStream(invalidJson);

        // Act
        var result = _parser.ReadAndParseJsonStream(stream);

        // Assert
        Assert.Null(result.jsonValue); // JsonNode should be null due to the catch(JsonException)
        Assert.Equal(invalidJson, result.stringValue); // Raw string must still be captured
    }
}