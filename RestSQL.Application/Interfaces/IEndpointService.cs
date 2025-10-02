using RestSQL.Config;

namespace RestSQL.Application.Interfaces;

public interface IEndpointService
{
    Task<object?> GetEndpointResult(Endpoint endpoint);
}
