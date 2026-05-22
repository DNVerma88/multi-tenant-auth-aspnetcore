using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Abstractions;
using MultiTenantAuth.AspNetCore.Constants;
using MultiTenantAuth.AspNetCore.Models;
using MultiTenantAuth.AspNetCore.Options;

namespace MultiTenantAuth.AspNetCore.Resolvers;

/// <summary>
/// Resolves the tenant from a query-string parameter.
/// <para>
/// WARNING: Disabled by default via <see cref="MultiTenantAuthOptions.EnableQueryStringResolution"/>.
/// Query-string values are easily manipulated by attackers via link-sharing and browser history.
/// Only enable this in scenarios where you have additional protections (e.g. short-lived signed URLs).
/// </para>
/// </summary>
internal sealed class QueryStringTenantResolver : ITenantResolver
{
    private readonly MultiTenantAuthOptions _options;

    public QueryStringTenantResolver(IOptions<MultiTenantAuthOptions> options)
        => _options = options.Value;

    public ValueTask<TenantResolutionResult> ResolveAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableQueryStringResolution)
            return new(TenantResolutionResult.Fail("Query-string resolution is disabled."));

        var key = _options.TenantQueryStringName;
        if (!context.Request.Query.TryGetValue(key, out var values)
            || values.Count == 0)
        {
            return new(TenantResolutionResult.Fail("Tenant query-string parameter not present."));
        }

        var tenantId = values[0]?.Trim();
        if (string.IsNullOrWhiteSpace(tenantId))
            return new(TenantResolutionResult.Fail("Tenant query-string parameter is empty."));

        var tenant = new TenantContext
        {
            TenantId = tenantId,
            Source = TenantConstants.ResolutionSources.QueryString,
            IsResolved = true
        };

        return new(TenantResolutionResult.Success(tenant));
    }
}
