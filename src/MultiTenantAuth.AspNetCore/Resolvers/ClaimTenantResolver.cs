using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Abstractions;
using MultiTenantAuth.AspNetCore.Constants;
using MultiTenantAuth.AspNetCore.Models;
using MultiTenantAuth.AspNetCore.Options;

namespace MultiTenantAuth.AspNetCore.Resolvers;

/// <summary>
/// Resolves the tenant from the authenticated user's JWT/identity claim
/// (default claim type: tenant_id).
/// </summary>
internal sealed class ClaimTenantResolver : ITenantResolver
{
    private readonly MultiTenantAuthOptions _options;

    public ClaimTenantResolver(IOptions<MultiTenantAuthOptions> options)
        => _options = options.Value;

    public ValueTask<TenantResolutionResult> ResolveAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableClaimResolution)
            return new(TenantResolutionResult.Fail("Claim resolution is disabled."));

        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
            return new(TenantResolutionResult.Fail("User not authenticated; cannot resolve from claim."));

        var tenantId = user.FindFirstValue(_options.TenantClaimType);
        if (string.IsNullOrWhiteSpace(tenantId))
            return new(TenantResolutionResult.Fail("Tenant claim not present."));

        var tenant = new TenantContext
        {
            TenantId = tenantId,
            Source = TenantConstants.ResolutionSources.Claim,
            IsResolved = true
        };

        return new(TenantResolutionResult.Success(tenant));
    }
}
