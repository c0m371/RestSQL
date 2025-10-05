using System;
using RestSQL.Application.Interfaces;

namespace RestSQL;

public static class EndpointMapper
{
    public static void MapEndpoints(WebApplication webApplication, Domain.Config config)
    {
        foreach (var endpoint in config.Endpoints)
        {
            if (endpoint.Verb != "GET")
                throw new NotSupportedException("Only HTTPGET supported for now");

            webApplication.MapGet(endpoint.Path, async (HttpRequest request, IEndpointService endpointService) =>
            {
                var parameters = new Dictionary<string, object?>();
                request.RouteValues.ToList().ForEach(kvp => parameters.Add(kvp.Key, kvp.Value));
                request.Query.ToList().ForEach(kvp => parameters.Add(kvp.Key, kvp.Value));

                var result = await endpointService.GetEndpointResultAsync(endpoint, parameters).ConfigureAwait(false);

                return Results.Json(result, statusCode: endpoint.StatusCode);
            });
        }
    }
}
