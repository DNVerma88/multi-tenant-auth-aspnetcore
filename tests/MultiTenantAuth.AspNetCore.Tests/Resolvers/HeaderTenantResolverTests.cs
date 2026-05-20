using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Options;
using MultiTenantAuth.AspNetCore.Resolvers;

namespace MultiTenantAuth.AspNetCore.Tests.Resolvers;

public class HeaderTenantResolverTests
{
    private static IOptions<MultiTenantAuthOptions> Opts(Action<MultiTenantAuthOptions>? configure = null)
    {
        var o = new MultiTenantAuthOptions();
        configure?.Invoke(o);
        return Microsoft.Extensions.Options.Options.Create(o);
    }

    [Fact]
    public async Task ResolveAsync_WithValidHeader_ReturnsSuccess()
    {
        var resolver = new HeaderTenantResolver(Opts());
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Tenant-Id"] = "acme";

        var result = await resolver.ResolveAsync(ctx);

        Assert.True(result.Succeeded);
        Assert.Equal("acme", result.Tenant!.TenantId);
        Assert.Equal("header", result.Tenant.Source);
    }

    [Fact]
    public async Task ResolveAsync_MissingHeader_ReturnsFail()
    {
        var resolver = new HeaderTenantResolver(Opts());
        var ctx = new DefaultHttpContext();

        var result = await resolver.ResolveAsync(ctx);

        Assert.False(result.Succeeded);
        Assert.Null(result.Tenant);
    }

    [Fact]
    public async Task ResolveAsync_EmptyHeader_ReturnsFail()
    {
        var resolver = new HeaderTenantResolver(Opts());
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Tenant-Id"] = "   ";

        var result = await resolver.ResolveAsync(ctx);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ResolveAsync_WhenDisabled_ReturnsFail()
    {
        var resolver = new HeaderTenantResolver(Opts(o => o.EnableHeaderResolution = false));
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Tenant-Id"] = "acme";

        var result = await resolver.ResolveAsync(ctx);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ResolveAsync_CustomHeaderName_ResolvesTenant()
    {
        var resolver = new HeaderTenantResolver(Opts(o => o.TenantHeaderName = "X-Custom-Tenant"));
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Custom-Tenant"] = "beta";

        var result = await resolver.ResolveAsync(ctx);

        Assert.True(result.Succeeded);
        Assert.Equal("beta", result.Tenant!.TenantId);
    }
}
