using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Abstractions;
using MultiTenantAuth.AspNetCore.Constants;
using MultiTenantAuth.AspNetCore.Models;
using MultiTenantAuth.AspNetCore.Options;

namespace MultiTenantAuth.AspNetCore.Resolvers;

/// <summary>Resolves the tenant from a route value (default key: tenantId).</summary>
internal sealed class RouteValueTenantResolver : ITenantResolver
{
    private readonly MultiTenantAuthOptions _options;

    public RouteValueTenantResolver(IOptions<MultiTenantAuthOptions> options)
        => _options = options.Value;

    public ValueTask<TenantResolutionResult> ResolveAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableRouteResolution)
            return new(TenantResolutionResult.Fail("Route resolution is disabled."));

        var routeKey = _options.TenantRouteValueName;
        if (!context.Request.RouteValues.TryGetValue(routeKey, out var raw)
            || raw is not string rawTenantId
            || string.IsNullOrWhiteSpace(rawTenantId))
        {
            return new(TenantResolutionResult.Fail("Tenant route value not present."));
        }

        var tenantId = rawTenantId.Trim();

        var tenant = new TenantContext
        {
            TenantId = tenantId,
            Source = TenantConstants.ResolutionSources.RouteValue,
            IsResolved = true
        };

        return new(TenantResolutionResult.Success(tenant));
    }
}
