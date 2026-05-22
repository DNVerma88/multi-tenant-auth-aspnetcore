using MultiTenantAuth.AspNetCore.Abstractions;
using MultiTenantAuth.AspNetCore.Constants;

namespace MultiTenantAuth.AspNetCore.Options;

/// <summary>
/// Configuration options for the multi-tenant auth middleware.
/// </summary>
public sealed class MultiTenantAuthOptions
{
    // -----------------------------------------------------------------------
    // General
    // -----------------------------------------------------------------------

    /// <summary>
    /// When false the middleware is a no-op and passes every request through.
    /// Useful for local development or feature-flag controlled roll-outs.
    /// Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    // -----------------------------------------------------------------------
    // Resolution order
    // -----------------------------------------------------------------------

    /// <summary>
    /// The ordered list of strategies the middleware tries in sequence.
    /// The first strategy that returns a non-empty tenant id wins.
    /// Default: RouteValue → Header → Subdomain → Claim.
    /// </summary>
    public TenantResolutionStrategy[] ResolutionOrder { get; set; } =
    [
        TenantResolutionStrategy.RouteValue,
        TenantResolutionStrategy.Header,
        TenantResolutionStrategy.Subdomain,
        TenantResolutionStrategy.Claim
    ];

    // -----------------------------------------------------------------------
    // Per-strategy toggles
    // -----------------------------------------------------------------------

    /// <summary>Allow tenant resolution from an HTTP header. Default: true.</summary>
    public bool EnableHeaderResolution { get; set; } = true;

    /// <summary>Allow tenant resolution from a route value. Default: true.</summary>
    public bool EnableRouteResolution { get; set; } = true;

    /// <summary>Allow tenant resolution from the request subdomain. Default: true.</summary>
    public bool EnableSubdomainResolution { get; set; } = true;

    /// <summary>Allow tenant resolution from a JWT/identity claim. Default: true.</summary>
    public bool EnableClaimResolution { get; set; } = true;

    /// <summary>
    /// Allow tenant resolution from a query-string parameter.
    /// <para>
    /// WARNING: Disabled by default. Query-string tenant values are trivially
    /// manipulated and can introduce CSRF/phishing risks. Enable only when you
    /// have additional protections in place and fully understand the trade-offs.
    /// </para>
    /// </summary>
    public bool EnableQueryStringResolution { get; set; } = false;

    // -----------------------------------------------------------------------
    // Strategy parameters
    // -----------------------------------------------------------------------

    /// <summary>HTTP header name carrying the tenant identifier. Default: X-Tenant-Id.</summary>
    public string TenantHeaderName { get; set; } = TenantConstants.DefaultTenantHeaderName;

    /// <summary>Route value key carrying the tenant identifier. Default: tenantId.</summary>
    public string TenantRouteValueName { get; set; } = TenantConstants.DefaultTenantRouteValueName;

    /// <summary>Query-string parameter name. Default: tenantId.</summary>
    public string TenantQueryStringName { get; set; } = TenantConstants.DefaultTenantQueryStringName;

    /// <summary>Claim type that carries the primary tenant identifier. Default: tenant_id.</summary>
    public string TenantClaimType { get; set; } = TenantConstants.DefaultTenantClaimType;

    /// <summary>
    /// Claim type that lists all tenants the user may access (comma-separated or multiple claims).
    /// Default: allowed_tenants.
    /// </summary>
    public string AllowedTenantsClaimType { get; set; } = TenantConstants.DefaultAllowedTenantsClaimType;

    // -----------------------------------------------------------------------
    // Validation / enforcement
    // -----------------------------------------------------------------------

    /// <summary>
    /// When true, requests from unauthenticated users are rejected with 401.
    /// Default: true.
    /// </summary>
    public bool RequireAuthenticatedUser { get; set; } = true;

    /// <summary>
    /// When true, requests where a tenant cannot be resolved are rejected.
    /// Default: true.
    /// </summary>
    public bool RequireResolvedTenant { get; set; } = true;

    /// <summary>
    /// When true, the authenticated user must have a claim matching the resolved tenant.
    /// Default: true.
    /// </summary>
    public bool RequireTenantClaim { get; set; } = true;

    // -----------------------------------------------------------------------
    // Tenant id format validation
    // -----------------------------------------------------------------------

    /// <summary>Minimum length of a tenant identifier. Default: 2.</summary>
    public int MinTenantIdLength { get; set; } = TenantConstants.DefaultMinTenantIdLength;

    /// <summary>Maximum length of a tenant identifier. Default: 64.</summary>
    public int MaxTenantIdLength { get; set; } = TenantConstants.DefaultMaxTenantIdLength;

    /// <summary>Maximum length of a tenant slug. Default: 100.</summary>
    public int MaxTenantSlugLength { get; set; } = TenantConstants.DefaultMaxTenantSlugLength;

    /// <summary>
    /// When true, resolved tenant identifiers are normalised to lowercase before
    /// being stored in <see cref="Models.TenantContext.TenantId"/>. This prevents
    /// case-confusion attacks where "ACME" and "acme" are treated as different
    /// tenants by case-sensitive downstream systems. Default: true.
    /// </summary>
    public bool NormalizeTenantIdToLowercase { get; set; } = true;

    /// <summary>
    /// Regex pattern that a tenant identifier must match.
    /// Default allows letters, digits, hyphens, and underscores.
    /// Set to null to skip pattern validation.
    /// </summary>
    public string? AllowedTenantPattern { get; set; } = TenantConstants.DefaultAllowedTenantPattern;

    // -----------------------------------------------------------------------
    // Failure behaviour
    // -----------------------------------------------------------------------

    /// <summary>
    /// Return HTTP 404 instead of 400 when the resolved tenant is unknown/not found.
    /// Useful to avoid leaking which tenant IDs exist. Default: false.
    /// </summary>
    public bool ReturnNotFoundForUnknownTenant { get; set; } = false;

    // -----------------------------------------------------------------------
    // Extension points
    // -----------------------------------------------------------------------

    /// <summary>
    /// Optional custom tenant resolver factory.
    /// When set and <see cref="TenantResolutionStrategy.Custom"/> is included in
    /// <see cref="ResolutionOrder"/>, this resolver is invoked.
    /// The resolver is resolved from DI; register it with
    /// <c>services.AddSingleton&lt;ITenantResolver, MyResolver&gt;()</c> and set
    /// this property to the same type, or use the extension method overload.
    /// </summary>
    public Type? CustomResolverType { get; set; }

    /// <summary>
    /// Optional custom tenant validator factory type.
    /// When set, replaces the built-in claims-based validator.
    /// Register the type in DI before calling <c>AddMultiTenantAuth</c>.
    /// </summary>
    public Type? CustomValidatorType { get; set; }
}
