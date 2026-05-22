using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Abstractions;
using MultiTenantAuth.AspNetCore.Models;
using MultiTenantAuth.AspNetCore.Options;

namespace MultiTenantAuth.AspNetCore.Validators;

/// <summary>
/// Default tenant validator.  Checks that:
/// <list type="bullet">
///   <item>When <see cref="MultiTenantAuthOptions.RequireAuthenticatedUser"/> is true,
///         the user must be authenticated (401 otherwise).</item>
///   <item>When <see cref="MultiTenantAuthOptions.RequireTenantClaim"/> is true,
///         the user must have a claim matching the resolved tenant (403 otherwise).
///         Both <c>TenantClaimType</c> and <c>AllowedTenantsClaimType</c> are checked.</item>
/// </list>
/// </summary>
internal sealed class DefaultTenantValidator : ITenantValidator
{
    private readonly MultiTenantAuthOptions _options;

    public DefaultTenantValidator(IOptions<MultiTenantAuthOptions> options)
        => _options = options.Value;

    public ValueTask<TenantValidationResult> ValidateAsync(
        TenantContext tenant,
        ClaimsPrincipal user,
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        if (_options.RequireAuthenticatedUser && user.Identity?.IsAuthenticated != true)
        {
            // RFC 7235 §4.1 — 401 responses MUST include a WWW-Authenticate header.
            context.Response.Headers.WWWAuthenticate = "Bearer";
            return new(TenantValidationResult.Fail(
                StatusCodes.Status401Unauthorized,
                "User is not authenticated."));
        }

        if (_options.RequireTenantClaim && user.Identity?.IsAuthenticated == true)
        {
            if (!UserBelongsToTenant(user, tenant.TenantId))
            {
                return new(TenantValidationResult.Fail(
                    StatusCodes.Status403Forbidden,
                    "User does not have access to this tenant."));
            }
        }

        return new(TenantValidationResult.Success());
    }

    private bool UserBelongsToTenant(ClaimsPrincipal user, string tenantId)
    {
        // Check all values of the primary tenant claim (handles both single and multiple claims).
        foreach (var claim in user.FindAll(_options.TenantClaimType))
        {
            if (string.Equals(claim.Value, tenantId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Check allowed_tenants claim (may be a comma-separated list or multiple claims).
        foreach (var claim in user.FindAll(_options.AllowedTenantsClaimType))
        {
            // Support both single-value and comma-separated multi-value claims.
            if (claim.Value.Contains(',', StringComparison.Ordinal))
            {
                var parts = claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var part in parts)
                {
                    if (string.Equals(part, tenantId, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            else if (string.Equals(claim.Value, tenantId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
