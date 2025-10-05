using System.Text.Json.Nodes;
using RestSQL.Domain;

namespace RestSQL.Application.Interfaces;

public interface IEndpointService
{
    Task<JsonNode?> GetEndpointResultAsync(Endpoint endpoint, IDictionary<string, object?> parameterValues);
}
