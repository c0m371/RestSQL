using System;
using System.Threading.Tasks;
using RestSQL.Application.Interfaces;
using RestSQL.Config;
using RestSQL.Data.Interfaces;

namespace RestSQL.Application;

public class EndpointService(IQueryDispatcher queryDispatcher, IResultAggregator resultAggregator) : IEndpointService
{
    public async Task<object?> GetEndpointResult(Endpoint endpoint)
    {
        throw new NotImplementedException();
        // var flatResult = await queryDispatcher.QueryAsync(endpoint.ConnectionName, endpoint.Sql, endpoint.Parameters).ConfigureAwait(false);
        // var result = resultAggregator.Aggregate(flatResult, endpoint.OutputStructure);
        // return result;
    }
}
