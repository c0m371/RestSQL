using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RestSQL.Application.Interfaces;
using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Application;

public class EndpointService(IQueryDispatcher queryDispatcher, IResultAggregator resultAggregator) : IEndpointService
{
    public async Task<JsonNode?> GetEndpointResultAsync(Endpoint endpoint, IDictionary<string, object?> parameterValues, Stream body)
    {
        await ExecuteWriteOperations(endpoint, parameterValues, body).ConfigureAwait(false);
        return await ExecuteQueries(endpoint, parameterValues).ConfigureAwait(false);
    }

    private async Task ExecuteWriteOperations(Endpoint endpoint, IDictionary<string, object?> parameterValues, Stream body)
    {
        if (!endpoint.WriteOperations.Any())
            return;

        var parsedBody = await ReadAndParseJsonStreamAsync(body).ConfigureAwait(false);
        
    }

    private async Task<JsonNode?> ExecuteQueries(Endpoint endpoint, IDictionary<string, object?> parameterValues)
    {
        var queryTasks = endpoint.SqlQueries
                    .Select(async q => new { Name = q.Key, Result = await queryDispatcher.QueryAsync(q.Value.ConnectionName, q.Value.Sql, parameterValues).ConfigureAwait(false) });
        var taskResults = await Task.WhenAll(queryTasks).ConfigureAwait(false);
        var queryResults = taskResults.ToDictionary(r => r.Name, r => r.Result);

        return resultAggregator.Aggregate(queryResults, endpoint.OutputStructure);
    }

    private async Task<object?> ReadAndParseJsonStreamAsync(Stream stream)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        var jsonNode = await JsonSerializer.DeserializeAsync<JsonNode>(stream, options);

        if (jsonNode == null)
            return null;

        if (jsonNode is JsonObject jsonObject)
            return jsonObject.AsObject().ToDictionary(p => p.Key, p => (object?)p.Value);

        else if (jsonNode is JsonArray jsonArray)
        {
            return jsonArray.Select(item => (object?)item switch
            {
                JsonObject obj => obj.ToDictionary(p => p.Key, p => (object?)p.Value),
                JsonValue value => value.GetValue<object>(),
                _ => null
            }).ToList();
        }

        throw new JsonException("Request body must be a single JSON object or an array.");
    }
}
