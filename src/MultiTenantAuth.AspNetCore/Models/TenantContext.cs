namespace MultiTenantAuth.AspNetCore.Models;

/// <summary>
/// Represents the resolved tenant context for the current HTTP request.
/// </summary>
public sealed class TenantContext
{
    /// <summary>The unique identifier of the resolved tenant.</summary>
    public required string TenantId { get; init; }

    /// <summary>An optional human-readable slug for the tenant (e.g. from subdomain).</summary>
    public string? TenantSlug { get; init; }

    /// <summary>
    /// Describes which resolution strategy produced this context
    /// (e.g. "header", "route", "subdomain", "claim", "querystring", "custom").
    /// </summary>
    public string? Source { get; init; }

    /// <summary>Indicates that a tenant was successfully resolved.</summary>
    public bool IsResolved { get; init; }
}
