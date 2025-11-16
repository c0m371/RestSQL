using RestSQL.Application.Interfaces;
using RestSQL.Domain;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL;

public static class WebApplicationExtensions
{
    public static void UseRestSQL(this WebApplication webApplication, ConfigurationManager configuration)
    {
        var configReader = webApplication.Services.GetService<IYamlConfigReader>()
            ?? throw new InvalidOperationException("AddRestSQL has not been called on container");

        var configFolder = configuration.GetSection("RestSQL").GetValue<string>("ConfigFolder")
            ?? throw new InvalidOperationException("ConfigFolder not set");

        var config = configReader.Read(configFolder);
        UseRestSQL(webApplication, config);
    }

    public static void UseRestSQL(this WebApplication webApplication, Config config)
    {
        var querydispatcher = webApplication.Services.GetService<IQueryDispatcher>()
            ?? throw new InvalidOperationException("AddRestSQL has not been called on container");

        if (config.Authentication is not null)
        {
            webApplication.UseAuthentication();
            webApplication.UseAuthorization();
        }

        querydispatcher.InitializeExecutors(config.Connections);
        EndpointMapper.MapEndpoints(webApplication, config);
    }
}
