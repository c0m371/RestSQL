using RestSQL.Application.Interfaces;

namespace RestSQL;

public static class EndpointMapper
{
    public static void MapEndpoints(WebApplication webApplication, Domain.Config config)
    {
        foreach (var endpoint in config.Endpoints)
        {
            webApplication.MapMethods(endpoint.Path, [endpoint.Method], async (HttpRequest request, IEndpointService endpointService) =>
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
            });
        }
    }
}
