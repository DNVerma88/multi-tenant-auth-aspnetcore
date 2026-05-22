using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Abstractions;
using MultiTenantAuth.AspNetCore.Constants;
using MultiTenantAuth.AspNetCore.Models;
using MultiTenantAuth.AspNetCore.Options;

namespace MultiTenantAuth.AspNetCore.Resolvers;

/// <summary>
/// Resolves the tenant from the first label of the request host (subdomain).
/// For example, <c>acme.example.com</c> → tenant slug <c>acme</c>.
/// Single-label hosts (e.g. <c>localhost</c>) are skipped.
/// </summary>
internal sealed class SubdomainTenantResolver : ITenantResolver
{
    private readonly MultiTenantAuthOptions _options;

    public SubdomainTenantResolver(IOptions<MultiTenantAuthOptions> options)
        => _options = options.Value;

    public ValueTask<TenantResolutionResult> ResolveAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableSubdomainResolution)
            return new(TenantResolutionResult.Fail("Subdomain resolution is disabled."));

        var host = context.Request.Host.Host;

        // Reject empty hosts.
        if (string.IsNullOrEmpty(host))
            return new(TenantResolutionResult.Fail("Empty host."));

        // Reject IP addresses (IPv4 and IPv6) — they have no meaningful subdomain.
        if (IPAddress.TryParse(host, out _))
            return new(TenantResolutionResult.Fail("Host is an IP address; no subdomain to extract."));

        // Reject single-label hostnames (e.g. localhost).
        var dotIndex = host.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex <= 0)
            return new(TenantResolutionResult.Fail("Host has no subdomain."));

        var slug = host[..dotIndex];

        if (string.IsNullOrWhiteSpace(slug))
            return new(TenantResolutionResult.Fail("Subdomain label is empty."));

        var tenant = new TenantContext
        {
            TenantId = slug,
            TenantSlug = slug,
            Source = TenantConstants.ResolutionSources.Subdomain,
            IsResolved = true
        };

        return new(TenantResolutionResult.Success(tenant));
    }
}
