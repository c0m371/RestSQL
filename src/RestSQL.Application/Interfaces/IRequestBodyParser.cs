using System.Text.Json.Nodes;

namespace RestSQL.Application.Interfaces;

public interface IRequestBodyParser
{
    Task<JsonNode?> ReadAndParseJsonStreamAsync(Stream? stream);
}
