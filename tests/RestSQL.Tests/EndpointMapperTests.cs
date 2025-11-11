using Microsoft.AspNetCore.Http;
using RestSQL.Application.Interfaces;
using Moq;
using Microsoft.AspNetCore.Builder;
using RestSQL.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace RestSQL.Tests;

public class EndpointMapperTests
{
    [Fact]
    public async Task MapEndpoints_ReturnsNotFound_WhenNoDataAndFlagIsTrue()
    {
        // Arrange
        var endpoint = new Domain.Endpoint
        {
            Path = "/test",
            Method = "GET",
            StatusCode = 200,
            StatusCodeOnEmptyResult = 404,
            OutputStructure = new Domain.OutputField
            {
                Type = Domain.OutputFieldType.Object,
                IsArray = true
            }
        };

        var endpointServiceMock = new Mock<IEndpointService>();
        endpointServiceMock
            .Setup(s => s.GetEndpointResultAsync(
                endpoint,
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<Stream>()))
            .ReturnsAsync(Domain.EndpointResult.Empty());

        using var server = CreateTestServer(endpoint, endpointServiceMock.Object);
        using var client = server.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task MapEndpoints_ReturnsConfiguredStatusCode_WhenNoDataAndFlagIsFalse()
    {
        // Arrange
        var endpoint = new Domain.Endpoint
        {
            Path = "/test",
            Method = "GET",
            StatusCode = 200,
            OutputStructure = new Domain.OutputField
            {
                Type = Domain.OutputFieldType.Object,
                IsArray = true
            }
        };

        var endpointServiceMock = new Mock<IEndpointService>();
        endpointServiceMock
            .Setup(s => s.GetEndpointResultAsync(
                endpoint,
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<Stream>()))
            .ReturnsAsync(Domain.EndpointResult.Empty());

        using var server = CreateTestServer(endpoint, endpointServiceMock.Object);
        using var client = server.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task MapEndpoints_ReturnsConfiguredStatusCode_WhenHasData()
    {
        // Arrange
        var endpoint = new Domain.Endpoint
        {
            Path = "/test",
            Method = "GET",
            StatusCode = 201,
            StatusCodeOnEmptyResult = 404,
            OutputStructure = new Domain.OutputField
            {
                Type = Domain.OutputFieldType.Object,
                IsArray = false
            }
        };

        var data = System.Text.Json.Nodes.JsonNode.Parse("""{"test":"value"}""");
        var endpointServiceMock = new Mock<IEndpointService>();
        endpointServiceMock
            .Setup(s => s.GetEndpointResultAsync(
                endpoint,
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<Stream>()))
            .ReturnsAsync(Domain.EndpointResult.Success(data));

        using var server = CreateTestServer(endpoint, endpointServiceMock.Object);
        using var client = server.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        Assert.Equal(StatusCodes.Status201Created, (int)response.StatusCode);
    }

    [Fact]
    public async Task MapEndpoints_Returns401_WhenAuthorizationRequiredButNoUser()
    {
        // Arrange
        var endpoint = new Domain.Endpoint
        {
            Path = "/test/auth",
            Method = "GET",
            StatusCode = 200,
            Authorize = true, // Authorization required
                              // AuthorizationScope not set (tests basic authentication requirement)
            OutputStructure = null
        };

        var endpointServiceMock = new Mock<IEndpointService>();
        // Use the default CreateTestServer which simulates an unauthenticated request
        using var server = CreateTestServer(endpoint, endpointServiceMock.Object);
        using var client = server.CreateClient();

        // Act
        // Request made without any authentication header
        var response = await client.GetAsync("/test/auth");

        // Assert
        // Authorization middleware should return 401 Unauthorized because Authorize=true
        Assert.Equal(StatusCodes.Status401Unauthorized, (int)response.StatusCode);
    }

    [Fact]
    public async Task MapEndpoints_Returns403_WhenAuthorizationRequiredButScopeIsMissing()
    {
        // Arrange
        var requiredScope = "data:write";
        var endpoint = new Domain.Endpoint
        {
            Path = "/test/scope",
            Method = "POST",
            StatusCode = 200,
            Authorize = true,
            AuthorizationScope = requiredScope, // Requires the "data:write" scope
            OutputStructure = null
        };

        var endpointServiceMock = new Mock<IEndpointService>();

        // Setup a user who is authenticated, but ONLY has the "data:read" scope
        var claims = new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim("scope", "data:read") // Missing the required 'data:write' scope
        };

        // Use the specialized CreateTestServer to set the authenticated user
        using var server = CreateTestServer(endpoint, endpointServiceMock.Object, claims);
        using var client = server.CreateClient();

        // Act
        var response = await client.PostAsync("/test/scope", new StringContent(""));

        // Assert
        // Authorization middleware should return 403 Forbidden because the required scope is missing
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);

        // Also verify the endpoint service was NOT called
        endpointServiceMock.Verify(
            s => s.GetEndpointResultAsync(It.IsAny<Domain.Endpoint>(), It.IsAny<IDictionary<string, object?>>(), It.IsAny<Stream>()),
            Times.Never);
    }


    private static TestServer CreateTestServer(Domain.Endpoint endpoint, IEndpointService endpointService, Claim[]? userClaims = null)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddSingleton(endpointService);

                // 1. Configure Authentication Services (Using a Test Scheme)
                services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.AuthenticationScheme, options => { });

                // 2. Configure Authorization Services (Important for the .RequireAuthorization() call)
                services.AddAuthorization(options =>
                {
                    // Add a default policy that requires authentication, necessary for Authorize=true without a scope
                    options.AddPolicy("RequireAuthenticatedUser", policy => policy.RequireAuthenticatedUser());

                    // Add a policy for the AuthorizationScope check (assuming you map scope claims to policies)
                    if (endpoint.AuthorizationScope is not null)
                    {
                        options.AddPolicy(endpoint.AuthorizationScope, policy =>
                            policy.RequireClaim("scope", endpoint.AuthorizationScope));
                    }
                });
            })
            .Configure(app =>
            {
                // Set up the Authentication Handler logic
                app.Use(async (context, next) =>
                {
                    // Manually set the authenticated user based on the claims passed in
                    if (userClaims is not null)
                    {
                        var identity = new ClaimsIdentity(userClaims, TestAuthHandler.AuthenticationScheme);
                        context.User = new ClaimsPrincipal(identity);
                    }
                    await next();
                });

                app.UseRouting();

                // 3. Add Authentication and Authorization Middleware
                app.UseAuthentication();
                app.UseAuthorization();

                app.UseEndpoints(endpoints =>
                {
                    var config = new Domain.Config
                    {
                        Endpoints = new[] { endpoint },
                        Connections = new Dictionary<string, Connection>()
                    };

                    EndpointMapper.MapEndpoints(endpoints, config);
                });
            });

        return new TestServer(builder);
    }

    // --- AUTHENTICATION HELPER CLASS ---

    // Simple test handler to bypass real token validation and simulate success/failure
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string AuthenticationScheme = "TestScheme";

        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        // This handler only executes if the User is explicitly set in the HttpContext (which we do in CreateTestServer)
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.User.Identity?.IsAuthenticated == true)
            {
                var ticket = new AuthenticationTicket(Context.User, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return Task.FromResult(AuthenticateResult.Fail("No user principal set for TestScheme."));
        }
    }
}