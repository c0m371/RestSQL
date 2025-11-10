using RestSQL.Application.Interfaces;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;

namespace RestSQL;

public static class EndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder builder, Domain.Config config)
    {
        foreach (var endpoint in config.Endpoints)
        {
            builder.MapMethods(endpoint.Path, new[] { endpoint.Method }, CreateHandlerDelegate(endpoint));
        }
    }

    internal static Func<HttpRequest, IEndpointService, Task<IResult>> CreateHandlerDelegate(Domain.Endpoint endpoint)
    {
        return async (HttpRequest request, IEndpointService endpointService) =>
        {
            var parameters = new Dictionary<string, object?>();
            request.RouteValues.ToList().ForEach(kvp => parameters.Add(kvp.Key, kvp.Value));
            request.Query.ToList().ForEach(kvp => parameters.Add(kvp.Key, kvp.Value));

            var result = await endpointService.GetEndpointResultAsync(endpoint, parameters, request.Body).ConfigureAwait(false);

            return Results.Json(
                result.Data,
                statusCode: endpoint.StatusCodeOnEmptyResult is not null && !result.HasData
                    ? endpoint.StatusCodeOnEmptyResult
                    : endpoint.StatusCode
            );
        };
    }
}
