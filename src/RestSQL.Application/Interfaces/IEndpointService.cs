using RestSQL.Domain;

namespace RestSQL.Application.Interfaces;

public interface IEndpointService
{
    Task<EndpointResult> GetEndpointResultAsync(Endpoint endpoint, IDictionary<string, object?> parameterValues, Stream? body);
}
