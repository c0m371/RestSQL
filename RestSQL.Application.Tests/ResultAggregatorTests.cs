using System.Text.Json.Nodes;
using RestSQL.Application.Interfaces;
using RestSQL.Domain;

namespace RestSQL.Application.Tests;

public class ResultAggregatorTests
{
    private readonly IResultAggregator _aggregator = new ResultAggregator();

    [Fact]
    public void Root_Primitive_ReturnsValue()
    {
        var queryResults = new Dictionary<string, IEnumerable<IDictionary<string, object?>>>
        {
            ["main"] =
            [
                new Dictionary<string, object?> { ["value"] = 42 }
            ]
        };

        var field = new OutputField(
            Type: OutputFieldType.Long,
            IsArray: false,
            Name: null,
            ColumnName: "value",
            QueryName: "main",
            LinkColumn: null,
            Fields: null
        );

        var result = _aggregator.Aggregate(queryResults, field);
        AssertJsonEqual("42", result);
    }

    [Fact]
    public void Object_WithPrimitiveFields_ReturnsJsonObject()
    {
        var queryResults = new Dictionary<string, IEnumerable<IDictionary<string, object?>>>
        {
            ["users"] =
            [
                new Dictionary<string, object?> { ["id"] = 1, ["name"] = "Tom" }
            ]
        };

        var field = new OutputField(
            Type: OutputFieldType.Object,
            IsArray: false,
            Name: null,
            ColumnName: null,
            QueryName: "users",
            LinkColumn: null,
            Fields:
            [
                new(OutputFieldType.Long, false, "id", "id", null, null, null),
                new(OutputFieldType.String, false, "name", "name", null, null, null)
            ]
        );

        var result = _aggregator.Aggregate(queryResults, field);
        AssertJsonEqual(@"{""id"":1,""name"":""Tom""}", result);
    }

    [Fact]
    public void Array_OfObjects_ReturnsJsonArray()
    {
        var queryResults = new Dictionary<string, IEnumerable<IDictionary<string, object?>>>
        {
            ["users"] =
            [
                new Dictionary<string, object?> { ["id"] = 1 },
                new Dictionary<string, object?> { ["id"] = 2 }
            ]
        };

        var field = new OutputField(
            Type: OutputFieldType.Object,
            IsArray: true,
            Name: null,
            ColumnName: null,
            QueryName: "users",
            LinkColumn: null,
            Fields:
            [
                new(OutputFieldType.Long, false, "id", "id", null, null, null)
            ]
        );

        var result = _aggregator.Aggregate(queryResults, field);
        AssertJsonEqual(@"[{""id"":1},{""id"":2}]", result);
    }

    [Fact]
    public void Array_OfPrimitives_ReturnsJsonArray()
    {
        var queryResults = new Dictionary<string, IEnumerable<IDictionary<string, object?>>>
        {
            ["tags"] =
            [
                new Dictionary<string, object?> { ["tag"] = "csharp" },
                new Dictionary<string, object?> { ["tag"] = "json" },
                new Dictionary<string, object?> { ["tag"] = "sql" }
            ]
        };

        var field = new OutputField(
            Type: OutputFieldType.String,
            IsArray: true,
            Name: null,
            ColumnName: "tag",
            QueryName: "tags",
            LinkColumn: null,
            Fields: null
        );

        var result = _aggregator.Aggregate(queryResults, field);
        AssertJsonEqual(@"[""csharp"",""json"",""sql""]", result);
    }

    [Fact]
    public void NullValues_ReturnsJsonNull()
    {
        var queryResults = new Dictionary<string, IEnumerable<IDictionary<string, object?>>>
        {
            ["main"] =
            [
                new Dictionary<string, object?> { ["value"] = null }
            ]
        };

        var field = new OutputField(
            Type: OutputFieldType.String,
            IsArray: false,
            Name: null,
            ColumnName: "value",
            QueryName: "main",
            LinkColumn: null,
            Fields: null
        );

        var result = _aggregator.Aggregate(queryResults, field);
        AssertJsonEqual("null", result);
    }

    [Fact]
    public void MissingColumn_Throws()
    {
        var queryResults = new Dictionary<string, IEnumerable<IDictionary<string, object?>>>
        {
            ["main"] =
            [
                new Dictionary<string, object?> { ["other"] = 123 }
            ]
        };

        var field = new OutputField(
            Type: OutputFieldType.Long,
            IsArray: false,
            Name: null,
            ColumnName: "missing",
            QueryName: "main",
            LinkColumn: null,
            Fields: null
        );

        Assert.Throws<ArgumentException>(() => _aggregator.Aggregate(queryResults, field));
    }

    [Fact]
    public void Nested_ObjectWithArrayOfObjects_LinkedByForeignKey()
    {
        var queryResults = new Dictionary<string, IEnumerable<IDictionary<string, object?>>>
        {
            ["users"] =
            [
                new Dictionary<string, object?> { ["user_id"] = 1, ["name"] = "Tom" },
                new Dictionary<string, object?> { ["user_id"] = 2, ["name"] = "Alice" }
            ],
            ["posts"] =
            [
                new Dictionary<string, object?> { ["id"] = 100, ["user_id"] = 1, ["title"] = "Hello" },
                new Dictionary<string, object?> { ["id"] = 101, ["user_id"] = 1, ["title"] = "World" },
                new Dictionary<string, object?> { ["id"] = 200, ["user_id"] = 2, ["title"] = "Nested" }
            ]
        };

        var postsField = new OutputField(
            Type: OutputFieldType.Object,
            IsArray: true,
            Name: "posts",
            ColumnName: null,
            QueryName: "posts",
            LinkColumn: "user_id",
            Fields:
            [
                new(OutputFieldType.Long, false, "id", "id", null, null, null),
                new(OutputFieldType.String, false, "title", "title", null, null, null)
            ]
        );

        var userField = new OutputField(
            Type: OutputFieldType.Object,
            IsArray: true,
            Name: null,
            ColumnName: null,
            QueryName: "users",
            LinkColumn: null,
            Fields:
            [
                new(OutputFieldType.Long, false, "id", "user_id", null, null, null),
                new(OutputFieldType.String, false, "name", "name", null, null, null),
                postsField
            ]
        );

        var result = _aggregator.Aggregate(queryResults, userField);

        var expected = @"[{""id"":1,""name"":""Tom"",""posts"":[{""id"":100,""title"":""Hello""},{""id"":101,""title"":""World""}]},{""id"":2,""name"":""Alice"",""posts"":[{""id"":200,""title"":""Nested""}]}]";
        AssertJsonEqual(expected, result);
    }

    [Fact]
    public void Nested_ObjectWithArrayOfPrimitives_LinkedByForeignKey()
    {
        var queryResults = new Dictionary<string, IEnumerable<IDictionary<string, object?>>>
        {
            ["users"] =
            [
                new Dictionary<string, object?> { ["user_id"] = 1, ["name"] = "Tom" }
            ],
            ["tags"] =
            [
                new Dictionary<string, object?> { ["user_id"] = 1, ["tag"] = "csharp" },
                new Dictionary<string, object?> { ["user_id"] = 1, ["tag"] = "sql" }
            ]
        };

        var tagsField = new OutputField(
            Type: OutputFieldType.String,
            IsArray: true,
            Name: "tags",
            ColumnName: "tag",
            QueryName: "tags",
            LinkColumn: "user_id",
            Fields: null
        );

        var userField = new OutputField(
            Type: OutputFieldType.Object,
            IsArray: true,
            Name: null,
            ColumnName: null,
            QueryName: "users",
            LinkColumn: null,
            Fields:
            [
                new(OutputFieldType.Long, false, "id", "user_id", null, null, null),
                new(OutputFieldType.String, false, "name", "name", null, null, null),
                tagsField
            ]
        );

        var result = _aggregator.Aggregate(queryResults, userField);

        var expected = @"[{""id"":1,""name"":""Tom"",""tags"":[""csharp"",""sql""]}]";

        AssertJsonEqual(expected, result);
    }

    [Fact]
    public void RootArrayOfPrimitives_ReturnsJsonArray()
    {
        var queryResults = new Dictionary<string, IEnumerable<IDictionary<string, object?>>>
        {
            ["tags"] =
            [
                    new Dictionary<string, object?> { ["tag"] = "x" },
                    new Dictionary<string, object?> { ["tag"] = "y" }
                ]
        };

        var field = new OutputField(
            Type: OutputFieldType.String,
            IsArray: true,
            Name: null,
            ColumnName: "tag",
            QueryName: "tags",
            LinkColumn: null,
            Fields: null
        );

        var result = _aggregator.Aggregate(queryResults, field);

        AssertJsonEqual(@"[""x"",""y""]", result);
    }

    private static void AssertJsonEqual(string expected, JsonNode? actual)
    {
        if (expected != (actual?.ToJsonString() ?? "null"))
        {
            var actualJson = actual?.ToJsonString(new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            }) ?? "null";

            throw new Xunit.Sdk.XunitException(
                $"JSON mismatch:\nExpected:\n{expected}\n\nActual:\n{actualJson}"
            );
        }
    }
}