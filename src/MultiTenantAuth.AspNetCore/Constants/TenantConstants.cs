namespace MultiTenantAuth.AspNetCore.Constants;

/// <summary>
/// Well-known constants used throughout the multi-tenant auth middleware.
/// </summary>
public static class TenantConstants
{
    /// <summary>Default HTTP header name used to carry the tenant identifier.</summary>
    public const string DefaultTenantHeaderName = "X-Tenant-Id";

    /// <summary>Default route value key used to carry the tenant identifier.</summary>
    public const string DefaultTenantRouteValueName = "tenantId";

    /// <summary>Default query string key used to carry the tenant identifier (disabled by default).</summary>
    public const string DefaultTenantQueryStringName = "tenantId";

    /// <summary>Default JWT / claim type used to identify the tenant.</summary>
    public const string DefaultTenantClaimType = "tenant_id";

    /// <summary>Default claim type that lists all tenants a user is allowed to access.</summary>
    public const string DefaultAllowedTenantsClaimType = "allowed_tenants";

    /// <summary>Minimum allowed length for a tenant identifier.</summary>
    public const int DefaultMinTenantIdLength = 2;

    /// <summary>Maximum allowed length for a tenant identifier.</summary>
    public const int DefaultMaxTenantIdLength = 64;

    /// <summary>Maximum allowed length for a tenant slug.</summary>
    public const int DefaultMaxTenantSlugLength = 100;

    /// <summary>
    /// The regex-compatible allowed character pattern for tenant identifiers.
    /// Allows letters, digits, hyphens, and underscores only.
    /// </summary>
    public const string DefaultAllowedTenantPattern = @"^[a-zA-Z0-9\-_]+$";

    internal static class ResolutionSources
    {
        public const string Header = "header";
        public const string RouteValue = "route";
        public const string Subdomain = "subdomain";
        public const string Claim = "claim";
        public const string QueryString = "querystring";
        public const string Custom = "custom";
    }
}
