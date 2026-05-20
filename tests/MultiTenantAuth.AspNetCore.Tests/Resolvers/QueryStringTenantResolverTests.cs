using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Options;
using MultiTenantAuth.AspNetCore.Resolvers;

namespace MultiTenantAuth.AspNetCore.Tests.Resolvers;

public class QueryStringTenantResolverTests
{
    private static IOptions<MultiTenantAuthOptions> Opts(Action<MultiTenantAuthOptions>? configure = null)
    {
        var o = new MultiTenantAuthOptions();
        configure?.Invoke(o);
        return Microsoft.Extensions.Options.Options.Create(o);
    }

    [Fact]
    public async Task ResolveAsync_DisabledByDefault_ReturnsFail()
    {
        var resolver = new QueryStringTenantResolver(Opts());
        var ctx = new DefaultHttpContext();
        ctx.Request.QueryString = new QueryString("?tenantId=alpha");

        var result = await resolver.ResolveAsync(ctx);

        // Must be disabled by default – security requirement.
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ResolveAsync_WhenEnabled_ReturnsSuccess()
    {
        var resolver = new QueryStringTenantResolver(Opts(o => o.EnableQueryStringResolution = true));
        var ctx = new DefaultHttpContext();
        ctx.Request.QueryString = new QueryString("?tenantId=alpha");

        var result = await resolver.ResolveAsync(ctx);

        Assert.True(result.Succeeded);
        Assert.Equal("alpha", result.Tenant!.TenantId);
        Assert.Equal("querystring", result.Tenant.Source);
    }

    [Fact]
    public async Task ResolveAsync_WhenEnabledButMissing_ReturnsFail()
    {
        var resolver = new QueryStringTenantResolver(Opts(o => o.EnableQueryStringResolution = true));
        var ctx = new DefaultHttpContext();

        var result = await resolver.ResolveAsync(ctx);

        Assert.False(result.Succeeded);
    }
}
