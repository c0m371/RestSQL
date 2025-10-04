using System.Text.Json.Nodes;
using RestSQL.Application.Interfaces;
using RestSQL.Config;
using RestSQL.Data.Interfaces;

namespace RestSQL.Application;

public class EndpointService(IQueryDispatcher queryDispatcher, IResultAggregator resultAggregator) : IEndpointService
{
    public async Task<JsonNode?> GetEndpointResult(Endpoint endpoint, IDictionary<string, object?> parameterValues)
    {
        var queryTasks = endpoint.SqlQueries
            .Select(async q => new { Name = q.Key, Result = await queryDispatcher.QueryAsync(q.Value.ConnectionName, q.Value.Sql, parameterValues).ConfigureAwait(false) });
        var taskResults = await Task.WhenAll(queryTasks).ConfigureAwait(false);
        var queryResults = taskResults.ToDictionary(r => r.Name, r => r.Result);

        return resultAggregator.Aggregate(queryResults, endpoint.OutputStructure);
    }
}
