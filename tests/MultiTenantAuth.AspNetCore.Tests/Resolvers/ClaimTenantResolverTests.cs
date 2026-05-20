using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Options;
using MultiTenantAuth.AspNetCore.Resolvers;

namespace MultiTenantAuth.AspNetCore.Tests.Resolvers;

public class ClaimTenantResolverTests
{
    private static IOptions<MultiTenantAuthOptions> Opts(Action<MultiTenantAuthOptions>? configure = null)
    {
        var o = new MultiTenantAuthOptions();
        configure?.Invoke(o);
        return Microsoft.Extensions.Options.Options.Create(o);
    }

    private static HttpContext AuthenticatedContext(string claimType, string tenantId)
    {
        var ctx = new DefaultHttpContext();
        var identity = new ClaimsIdentity(
            [new Claim(claimType, tenantId)],
            "Test");
        ctx.User = new ClaimsPrincipal(identity);
        return ctx;
    }

    [Fact]
    public async Task ResolveAsync_WithTenantClaim_ReturnsSuccess()
    {
        var resolver = new ClaimTenantResolver(Opts());
        var ctx = AuthenticatedContext("tenant_id", "fabrikam");

        var result = await resolver.ResolveAsync(ctx);

        Assert.True(result.Succeeded);
        Assert.Equal("fabrikam", result.Tenant!.TenantId);
        Assert.Equal("claim", result.Tenant.Source);
    }

    [Fact]
    public async Task ResolveAsync_Unauthenticated_ReturnsFail()
    {
        var resolver = new ClaimTenantResolver(Opts());
        var ctx = new DefaultHttpContext();

        var result = await resolver.ResolveAsync(ctx);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ResolveAsync_MissingClaim_ReturnsFail()
    {
        var resolver = new ClaimTenantResolver(Opts());
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity([], "Test"));

        var result = await resolver.ResolveAsync(ctx);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ResolveAsync_WhenDisabled_ReturnsFail()
    {
        var resolver = new ClaimTenantResolver(Opts(o => o.EnableClaimResolution = false));
        var ctx = AuthenticatedContext("tenant_id", "fabrikam");

        var result = await resolver.ResolveAsync(ctx);

        Assert.False(result.Succeeded);
    }
}
