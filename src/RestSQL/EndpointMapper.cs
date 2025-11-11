using RestSQL.Application.Interfaces;

namespace RestSQL;

public static class EndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder builder, Domain.Config config)
    {
        foreach (var endpoint in config.Endpoints)
        {
            var endpointBuilder = builder.MapMethods(endpoint.Path, [endpoint.Method], CreateHandlerDelegate(endpoint));
            AddAuthorization(endpoint, endpointBuilder);
        }
    }

    internal static void AddAuthorization(Domain.Endpoint endpoint, RouteHandlerBuilder endpointBuilder)
    {
        if (endpoint.Authorize)
        {
            List<string> scopes = [Constants.RequireAuthenticatedUser];
            if (endpoint.AuthorizationScope is not null)
                scopes.Add(endpoint.AuthorizationScope);

            endpointBuilder.RequireAuthorization(scopes.ToArray());
        }
    }

    internal static Func<HttpRequest, IEndpointService, Task<IResult>> CreateHandlerDelegate(Domain.Endpoint endpoint)
    {
        return async (request, endpointService) =>
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
