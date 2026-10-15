using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace TaskApi.Tests;

/// Guardrail test: a new endpoint is protected unless someone deliberately allowlists it here.
public class EndpointSecurityTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly string[] AnonymousAllowlist =
    [
        "GET /health",
        "POST /auth/register",
        "POST /auth/login",
    ];

    [Fact]
    public void Only_allowlisted_endpoints_allow_anonymous_access()
    {
        api.CreateClient(); // boots the host
        var endpoints = api.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();

        var anonymous = endpoints
            .Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(e => $"{e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Single()} /{e.RoutePattern.RawText?.TrimStart('/')}")
            .Order()
            .ToArray();

        Assert.Equal(AnonymousAllowlist.Order().ToArray(), anonymous);
    }
}
