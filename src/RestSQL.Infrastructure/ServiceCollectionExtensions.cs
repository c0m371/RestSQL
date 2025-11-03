using Microsoft.Extensions.DependencyInjection;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static void AddRestSQLInfrastructure(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IQueryDispatcher, QueryDispatcher>();
    }
}
