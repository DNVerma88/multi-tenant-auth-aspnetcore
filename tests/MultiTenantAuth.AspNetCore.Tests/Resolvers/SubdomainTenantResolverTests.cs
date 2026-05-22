using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Options;
using MultiTenantAuth.AspNetCore.Resolvers;

namespace MultiTenantAuth.AspNetCore.Tests.Resolvers;

public class SubdomainTenantResolverTests
{
    private static IOptions<MultiTenantAuthOptions> Opts(Action<MultiTenantAuthOptions>? configure = null)
    {
        var o = new MultiTenantAuthOptions();
        configure?.Invoke(o);
        return Microsoft.Extensions.Options.Options.Create(o);
    }

    [Fact]
    public async Task ResolveAsync_WithSubdomain_ReturnsSuccess()
    {
        var resolver = new SubdomainTenantResolver(Opts());
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("acme.example.com");

        var result = await resolver.ResolveAsync(ctx);

        Assert.True(result.Succeeded);
        Assert.Equal("acme", result.Tenant!.TenantId);
        Assert.Equal("acme", result.Tenant.TenantSlug);
        Assert.Equal("subdomain", result.Tenant.Source);
    }

    [Fact]
    public async Task ResolveAsync_LocalhostHost_ReturnsFail()
    {
        var resolver = new SubdomainTenantResolver(Opts());
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("localhost");

        var result = await resolver.ResolveAsync(ctx);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ResolveAsync_WhenDisabled_ReturnsFail()
    {
        var resolver = new SubdomainTenantResolver(Opts(o => o.EnableSubdomainResolution = false));
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("acme.example.com");

        var result = await resolver.ResolveAsync(ctx);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ResolveAsync_IpAddress_FailsResolution()
    {
        // Security fix (VULN-03): IP addresses must be rejected — they have no
        // meaningful subdomain and "192" extracted as a label would bypass tenant guards.
        var resolver = new SubdomainTenantResolver(Opts());
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("192.168.1.1");

        var result = await resolver.ResolveAsync(ctx);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ResolveAsync_IPv6Address_FailsResolution()
    {
        // HostString.Host for "[::1]" returns "::1" which IPAddress.TryParse handles.
        var resolver = new SubdomainTenantResolver(Opts());
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("[::1]");

        var result = await resolver.ResolveAsync(ctx);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ResolveAsync_EmptyHost_ReturnsFail()
    {
        var resolver = new SubdomainTenantResolver(Opts());
        var ctx = new DefaultHttpContext();
        // DefaultHttpContext.Request.Host is empty by default.

        var result = await resolver.ResolveAsync(ctx);

        Assert.False(result.Succeeded);
    }
}
