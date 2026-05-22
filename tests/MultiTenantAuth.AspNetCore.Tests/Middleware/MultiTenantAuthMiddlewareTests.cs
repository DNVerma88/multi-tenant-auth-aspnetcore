using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Abstractions;
using MultiTenantAuth.AspNetCore.Extensions;
using MultiTenantAuth.AspNetCore.Models;
using MultiTenantAuth.AspNetCore.Options;

namespace MultiTenantAuth.AspNetCore.Tests.Middleware;

/// <summary>
/// Integration tests that spin up a real ASP.NET Core test host and verify
/// the end-to-end middleware behaviour.
/// </summary>
public class MultiTenantAuthMiddlewareTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static TestServer BuildServer(
        Action<MultiTenantAuthOptions>? configure = null,
        bool authenticated = false,
        string? tenantClaim = null,
        string[]? allowedTenants = null,
        ITenantResolver? customResolver = null,
        ITenantValidator? customValidator = null)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAuthorization();

                // Fake authentication scheme so tests can opt into auth.
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>("Test", _ => { });

                services.AddSingleton<FakeAuthHandlerOptions>(new FakeAuthHandlerOptions(
                    authenticated, tenantClaim, allowedTenants));

                if (customResolver is not null)
                    services.AddSingleton<ITenantResolver>(customResolver);

                if (customValidator is not null)
                    services.AddSingleton<ITenantValidator>(customValidator);

                services.AddMultiTenantAuth(opt =>
                {
                    configure?.Invoke(opt);
                    if (customResolver is not null)
                    {
                        opt.CustomResolverType = customResolver.GetType();
                        opt.ResolutionOrder = [TenantResolutionStrategy.Custom];
                    }
                    if (customValidator is not null)
                        opt.CustomValidatorType = customValidator.GetType();
                });
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseMultiTenantAuth();
                app.UseAuthorization();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/api/{tenantId}/data", async ctx =>
                    {
                        var accessor = ctx.RequestServices.GetRequiredService<ITenantContextAccessor>();
                        var tenantId = accessor.Current?.TenantId ?? "none";
                        await ctx.Response.WriteAsync(tenantId);
                    });

                    endpoints.MapGet("/api/no-route", async ctx =>
                    {
                        var accessor = ctx.RequestServices.GetRequiredService<ITenantContextAccessor>();
                        await ctx.Response.WriteAsync(accessor.Current?.TenantId ?? "none");
                    });
                });
            });

        return new TestServer(builder);
    }

    private static HttpClient Client(
        Action<MultiTenantAuthOptions>? configure = null,
        bool authenticated = false,
        string? tenantClaim = null,
        string[]? allowedTenants = null,
        ITenantResolver? customResolver = null,
        ITenantValidator? customValidator = null)
        => BuildServer(configure, authenticated, tenantClaim, allowedTenants, customResolver, customValidator)
            .CreateClient();

    // -----------------------------------------------------------------------
    // Route resolution
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Request_RouteValue_ResolvesTenant()
    {
        var client = Client(
            configure: o => o.ResolutionOrder = [TenantResolutionStrategy.RouteValue],
            authenticated: true,
            tenantClaim: "acme");

        var response = await client.GetAsync("/api/acme/data");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("acme", await response.Content.ReadAsStringAsync());
    }

    // -----------------------------------------------------------------------
    // Header resolution
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Request_Header_ResolvesTenant()
    {
        var client = Client(
            configure: o => o.ResolutionOrder = [TenantResolutionStrategy.Header],
            authenticated: true,
            tenantClaim: "beta");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/no-route");
        request.Headers.Add("X-Tenant-Id", "beta");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("beta", await response.Content.ReadAsStringAsync());
    }

    // -----------------------------------------------------------------------
    // Format validation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Request_InvalidTenantIdFormat_Returns400()
    {
        var client = Client(
            configure: o => o.ResolutionOrder = [TenantResolutionStrategy.Header],
            authenticated: true,
            tenantClaim: "tenant with spaces");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/no-route");
        request.Headers.Add("X-Tenant-Id", "tenant with spaces");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Auth enforcement
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Request_UnresolvedTenant_Returns400()
    {
        var client = Client(
            configure: o =>
            {
                o.ResolutionOrder = [TenantResolutionStrategy.Header];
                o.RequireResolvedTenant = true;
            },
            authenticated: true,
            tenantClaim: "acme");

        // No header sent → no tenant resolved.
        var response = await client.GetAsync("/api/no-route");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Request_Unauthenticated_Returns401()
    {
        var client = Client(
            configure: o =>
            {
                o.ResolutionOrder = [TenantResolutionStrategy.Header];
                o.RequireAuthenticatedUser = true;
            },
            authenticated: false);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/no-route");
        request.Headers.Add("X-Tenant-Id", "acme");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_AuthenticatedWrongTenant_Returns403()
    {
        var client = Client(
            configure: o => o.ResolutionOrder = [TenantResolutionStrategy.Header],
            authenticated: true,
            tenantClaim: "other-tenant");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/no-route");
        request.Headers.Add("X-Tenant-Id", "acme");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Request_AuthenticatedMatchingTenant_Returns200()
    {
        var client = Client(
            configure: o => o.ResolutionOrder = [TenantResolutionStrategy.Header],
            authenticated: true,
            tenantClaim: "acme");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/no-route");
        request.Headers.Add("X-Tenant-Id", "acme");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Request_AllowedTenantsClaim_Returns200()
    {
        var client = Client(
            configure: o => o.ResolutionOrder = [TenantResolutionStrategy.Header],
            authenticated: true,
            allowedTenants: ["acme", "beta", "gamma"]);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/no-route");
        request.Headers.Add("X-Tenant-Id", "beta");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Query string disabled by default
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Request_QueryStringDisabledByDefault_UnresolvedReturns400()
    {
        var client = Client(
            configure: o =>
            {
                // Only query string in the order; should fail because it is disabled.
                o.ResolutionOrder = [TenantResolutionStrategy.QueryString];
                o.RequireResolvedTenant = true;
            },
            authenticated: true,
            tenantClaim: "acme");

        var response = await client.GetAsync("/api/no-route?tenantId=acme");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Middleware disabled
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Request_MiddlewareDisabled_PassThrough()
    {
        var client = Client(
            configure: o => o.Enabled = false,
            authenticated: false);

        var response = await client.GetAsync("/api/no-route");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // 404 for unknown tenant option
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Request_ReturnNotFound_WhenConfigured()
    {
        var client = Client(
            configure: o =>
            {
                o.ResolutionOrder = [TenantResolutionStrategy.Header];
                o.RequireResolvedTenant = true;
                o.ReturnNotFoundForUnknownTenant = true;
            },
            authenticated: true,
            tenantClaim: "acme");

        // No header → tenant not resolved → should return 404.
        var response = await client.GetAsync("/api/no-route");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Custom resolver
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Request_CustomResolver_IsInvoked()
    {
        var customResolver = new FixedTenantResolver("custom-tenant");
        var client = Client(
            configure: o => o.ResolutionOrder = [TenantResolutionStrategy.Custom],
            authenticated: true,
            tenantClaim: "custom-tenant",
            customResolver: customResolver);

        var response = await client.GetAsync("/api/no-route");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("custom-tenant", await response.Content.ReadAsStringAsync());
    }

    // -----------------------------------------------------------------------
    // Custom validator
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Request_CustomValidator_IsInvoked()
    {
        var customValidator = new AlwaysAllowValidator();
        var client = Client(
            configure: o => o.ResolutionOrder = [TenantResolutionStrategy.Header],
            authenticated: false, // would normally 401
            customValidator: customValidator);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/no-route");
        request.Headers.Add("X-Tenant-Id", "acme");
        var response = await client.SendAsync(request);

        // Custom validator overrides default 401.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Context cleared after request
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Context_ClearedAfterRequest_NoCrossContamination()
    {
        var client = Client(
            configure: o => o.ResolutionOrder = [TenantResolutionStrategy.Header],
            authenticated: true,
            tenantClaim: "acme");

        // First request with tenant.
        var r1 = new HttpRequestMessage(HttpMethod.Get, "/api/no-route");
        r1.Headers.Add("X-Tenant-Id", "acme");
        var resp1 = await client.SendAsync(r1);
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);

        // Second request without tenant header should fail (no cross-request contamination).
        var r2 = new HttpRequestMessage(HttpMethod.Get, "/api/no-route");
        var resp2 = await client.SendAsync(r2);
        Assert.Equal(HttpStatusCode.BadRequest, resp2.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Whitespace trimming in resolvers
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Request_HeaderWithWhitespacePadding_TrimmedAndResolved()
    {
        var client = Client(
            configure: o => o.ResolutionOrder = [TenantResolutionStrategy.Header],
            authenticated: true,
            tenantClaim: "acme");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/no-route");
        request.Headers.TryAddWithoutValidation("X-Tenant-Id", "  acme  ");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("acme", await response.Content.ReadAsStringAsync());
    }

    // -----------------------------------------------------------------------
    // ResolutionOrder null / empty guard
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Request_ResolutionOrderNull_Returns400()
    {
        var client = Client(
            configure: o =>
            {
                o.ResolutionOrder = null!;
                o.RequireResolvedTenant = true;
            },
            authenticated: true,
            tenantClaim: "acme");

        var response = await client.GetAsync("/api/no-route");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Request_ResolutionOrderEmpty_Returns400()
    {
        var client = Client(
            configure: o =>
            {
                o.ResolutionOrder = [];
                o.RequireResolvedTenant = true;
            },
            authenticated: true,
            tenantClaim: "acme");

        var response = await client.GetAsync("/api/no-route");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Tenant ID normalisation (VULN-08)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Request_UppercaseTenantId_NormalizedToLowercase()
    {
        // NormalizeTenantIdToLowercase defaults to true.
        // Header sends "ACME"; claim is "acme" — case-insensitive match allows auth.
        // The stored TenantId in context must be "acme" (lowercased).
        var client = Client(
            configure: o => o.ResolutionOrder = [TenantResolutionStrategy.Header],
            authenticated: true,
            tenantClaim: "acme");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/no-route");
        request.Headers.Add("X-Tenant-Id", "ACME");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("acme", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Request_NormalizationDisabled_PreservesOriginalCase()
    {
        var client = Client(
            configure: o =>
            {
                o.ResolutionOrder = [TenantResolutionStrategy.Header];
                o.NormalizeTenantIdToLowercase = false;
            },
            authenticated: true,
            tenantClaim: "ACME");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/no-route");
        request.Headers.Add("X-Tenant-Id", "ACME");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ACME", await response.Content.ReadAsStringAsync());
    }

    // -----------------------------------------------------------------------
    // WWW-Authenticate header on 401 (VULN-04)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Request_Unauthenticated_Returns401_WithWwwAuthenticateHeader()
    {
        var client = Client(
            configure: o =>
            {
                o.ResolutionOrder = [TenantResolutionStrategy.Header];
                o.RequireAuthenticatedUser = true;
            },
            authenticated: false);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/no-route");
        request.Headers.Add("X-Tenant-Id", "acme");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(
            response.Headers.WwwAuthenticate.Any(),
            "401 response must include a WWW-Authenticate header (RFC 7235 §4.1).");
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }
}

// ---------------------------------------------------------------------------
// Test doubles
// ---------------------------------------------------------------------------

file sealed class FakeAuthHandlerOptions(bool authenticated, string? tenantClaim, string[]? allowedTenants)
{
    public bool Authenticated { get; } = authenticated;
    public string? TenantClaim { get; } = tenantClaim;
    public string[]? AllowedTenants { get; } = allowedTenants;
}

file sealed class FakeAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder,
    FakeAuthHandlerOptions fakeOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!fakeOptions.Authenticated)
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new(ClaimTypes.Name, "testuser") };

        if (fakeOptions.TenantClaim is not null)
            claims.Add(new Claim("tenant_id", fakeOptions.TenantClaim));

        if (fakeOptions.AllowedTenants is not null)
            claims.Add(new Claim("allowed_tenants",
                string.Join(",", fakeOptions.AllowedTenants)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}

file sealed class FixedTenantResolver(string tenantId) : ITenantResolver
{
    public ValueTask<TenantResolutionResult> ResolveAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        var tenant = new TenantContext
        {
            TenantId = tenantId,
            Source = "custom",
            IsResolved = true
        };
        return new(TenantResolutionResult.Success(tenant));
    }
}

file sealed class AlwaysAllowValidator : ITenantValidator
{
    public ValueTask<TenantValidationResult> ValidateAsync(
        TenantContext tenant,
        ClaimsPrincipal user,
        HttpContext context,
        CancellationToken cancellationToken = default)
        => new(TenantValidationResult.Success());
}
