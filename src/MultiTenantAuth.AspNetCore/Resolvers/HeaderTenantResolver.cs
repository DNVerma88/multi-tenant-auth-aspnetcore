using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Abstractions;
using MultiTenantAuth.AspNetCore.Constants;
using MultiTenantAuth.AspNetCore.Models;
using MultiTenantAuth.AspNetCore.Options;

namespace MultiTenantAuth.AspNetCore.Resolvers;

/// <summary>Resolves the tenant from a request HTTP header (default: X-Tenant-Id).</summary>
internal sealed class HeaderTenantResolver : ITenantResolver
{
    private readonly MultiTenantAuthOptions _options;

    public HeaderTenantResolver(IOptions<MultiTenantAuthOptions> options)
        => _options = options.Value;

    public ValueTask<TenantResolutionResult> ResolveAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableHeaderResolution)
            return new(TenantResolutionResult.Fail("Header resolution is disabled."));

        var headerName = _options.TenantHeaderName;
        if (!context.Request.Headers.TryGetValue(headerName, out var values)
            || values.Count == 0)
        {
            return new(TenantResolutionResult.Fail("Tenant header not present."));
        }

        var tenantId = values[0]?.Trim();
        if (string.IsNullOrWhiteSpace(tenantId))
            return new(TenantResolutionResult.Fail("Tenant header is empty."));

        var tenant = new TenantContext
        {
            TenantId = tenantId,
            Source = TenantConstants.ResolutionSources.Header,
            IsResolved = true
        };

        return new(TenantResolutionResult.Success(tenant));
    }
}
