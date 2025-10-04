using Microsoft.Extensions.DependencyInjection;
using RestSQL.Data.Interfaces;

namespace RestSQL.Data;

public static class ServiceCollectionExtensions
{
    public static void AddRestSQLData(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IQueryDispatcher, QueryDispatcher>();
    }
}
