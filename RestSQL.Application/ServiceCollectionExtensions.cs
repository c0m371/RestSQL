using System;
using Microsoft.Extensions.DependencyInjection;
using RestSQL.Application.Interfaces;

namespace RestSQL.Application;

public static class ServiceCollectionExtensions
{
    public static void AddRestSQLApplication(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IEndpointService, EndpointService>();
        serviceCollection.AddSingleton<IYamlConfigReader, YamlConfigReader>();
        serviceCollection.AddSingleton<IResultAggregator, ResultAggregator>();
    }
}
