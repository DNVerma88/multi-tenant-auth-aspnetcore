using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Options;
using MultiTenantAuth.AspNetCore.Resolvers;

namespace MultiTenantAuth.AspNetCore.Tests.Resolvers;

public class RouteValueTenantResolverTests
{
    private static IOptions<MultiTenantAuthOptions> Opts(Action<MultiTenantAuthOptions>? configure = null)
    {
        var o = new MultiTenantAuthOptions();
        configure?.Invoke(o);
        return Microsoft.Extensions.Options.Options.Create(o);
    }

    [Fact]
    public async Task ResolveAsync_WithRouteValue_ReturnsSuccess()
    {
        var resolver = new RouteValueTenantResolver(Opts());
        var ctx = new DefaultHttpContext();
        ctx.Request.RouteValues["tenantId"] = "contoso";

        var result = await resolver.ResolveAsync(ctx);

        Assert.True(result.Succeeded);
        Assert.Equal("contoso", result.Tenant!.TenantId);
        Assert.Equal("route", result.Tenant.Source);
    }

    [Fact]
    public async Task ResolveAsync_MissingRouteValue_ReturnsFail()
    {
        var resolver = new RouteValueTenantResolver(Opts());
        var ctx = new DefaultHttpContext();

        var result = await resolver.ResolveAsync(ctx);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ResolveAsync_WhenDisabled_ReturnsFail()
    {
        var resolver = new RouteValueTenantResolver(Opts(o => o.EnableRouteResolution = false));
        var ctx = new DefaultHttpContext();
        ctx.Request.RouteValues["tenantId"] = "contoso";

        var result = await resolver.ResolveAsync(ctx);

        Assert.False(result.Succeeded);
    }
}
