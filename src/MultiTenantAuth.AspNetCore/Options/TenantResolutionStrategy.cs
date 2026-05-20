namespace MultiTenantAuth.AspNetCore.Options;

/// <summary>
/// Defines the available strategies for resolving a tenant from an HTTP request.
/// </summary>
public enum TenantResolutionStrategy
{
    /// <summary>No resolution; tenant context will not be populated.</summary>
    None,

    /// <summary>Resolve from the first subdomain label of the request host.</summary>
    Subdomain,

    /// <summary>Resolve from a request header (default: X-Tenant-Id).</summary>
    Header,

    /// <summary>Resolve from a route value (default: tenantId).</summary>
    RouteValue,

    /// <summary>Resolve from a JWT / identity claim (default: tenant_id).</summary>
    Claim,

    /// <summary>
    /// Resolve from a query-string parameter.
    /// <para>
    /// WARNING: Query-string tenant resolution is disabled by default.
    /// Enable <see cref="MultiTenantAuthOptions.EnableQueryStringResolution"/> only when
    /// you fully understand the security implications (e.g. link-sharing, CSRF surface).
    /// </para>
    /// </summary>
    QueryString,

    /// <summary>Resolve using a custom <c>ITenantResolver</c> registered in DI.</summary>
    Custom
}
