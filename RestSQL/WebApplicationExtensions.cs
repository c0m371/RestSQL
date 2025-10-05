using System;
using RestSQL.Application.Interfaces;

namespace RestSQL;

public static class WebApplicationExtensions
{
    public static void UseRestSQL(this WebApplication webApplication, string configFolder)
    {
        var configReader = webApplication.Services.GetService<IYamlConfigReader>()
            ?? throw new InvalidOperationException("AddRestSQL has not been called on container");

        var config = configReader.Read(configFolder);
        EndpointMapper.MapEndpoints(webApplication, config);
    }
}
