using System.Text.Json.Nodes;

namespace RestSQL.Domain;

public class EndpointResult
{
    public JsonNode? Data { get; }
    public bool HasData { get; }

    private EndpointResult(JsonNode? data, bool hasData)
    {
        Data = data;
        HasData = hasData;
    }

    public static EndpointResult Success(JsonNode? data) => 
        new(data, IsNotEmpty(data));

    public static EndpointResult Empty() => 
        new(null, false);

    private static bool IsNotEmpty(JsonNode? data)
    {
        if (data is null) return false;
        
        // For arrays, check if they're empty
        if (data is JsonArray arr)
            return arr.Count > 0;

        // For objects or other types, consider them non-empty if they exist
        return true;
    }
}