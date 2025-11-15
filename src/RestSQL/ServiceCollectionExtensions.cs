using RestSQL.Application;
using RestSQL.Infrastructure;
using RestSQL.Infrastructure.Dapper;
using RestSQL.Infrastructure.MySql;
using RestSQL.Infrastructure.PostgreSQL;
using RestSQL.Infrastructure.SqlServer;
using RestSQL.Infrastructure.Oracle;
using RestSQL.Infrastructure.Sqlite;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using RestSQL.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace RestSQL;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRestSQL(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddRestSQLApplication();
        serviceCollection.AddRestSQLInfrastructure();
        serviceCollection.AddRestSQLInfrastructureDapper();
        serviceCollection.AddPostgreSQL();
        serviceCollection.AddSqlServer();
        serviceCollection.AddMySql();
        serviceCollection.AddOracle();
        serviceCollection.AddSqlite();

        return serviceCollection;
    }

    public static void AddRestSQLAuthentication(this IServiceCollection serviceCollection, ConfigurationManager configuration)
    {
        // Always add authentication
        serviceCollection.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer();

        serviceCollection.AddAuthorization();

        serviceCollection.Configure<RestSQLConfig>(configuration.GetSection("RestSQL"));

        // Use AddOptions with Configure to inject the service
        serviceCollection.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IYamlConfigReader, IOptions<RestSQLConfig>>((options, configService, restSQLConfig) =>
            {
                var config = configService.Read(restSQLConfig.Value.ConfigFolder);

                if (config.Authentication is not null)
                {
                    options.Authority = config.Authentication.Authority;
                    options.Audience = config.Authentication.Audience;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true
                    };
                }
                else
                {
                    // Disable validation if not required
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        SignatureValidator = (token, parameters) => new JwtSecurityToken(token)
                    };
                }
            });

        serviceCollection.AddOptions<AuthorizationOptions>()
        .Configure<IYamlConfigReader, IOptions<RestSQLConfig>>((options, configService, restSQLConfig) =>
        {
            var config = configService.Read(restSQLConfig.Value.ConfigFolder);

            if (config.Authentication is not null)
            {
                foreach (var scope in config.Authentication.Scopes)
                    options.AddPolicy(
                        scope,
                        policy => policy.RequireClaim("scope", scope)
                    );

                options.AddPolicy(Constants.RequireAuthenticatedUser, policy => policy.RequireAuthenticatedUser());
            }
        });
    }
}
