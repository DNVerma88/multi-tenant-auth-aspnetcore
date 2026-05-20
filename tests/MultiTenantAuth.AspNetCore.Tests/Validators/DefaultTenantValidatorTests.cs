using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Models;
using MultiTenantAuth.AspNetCore.Options;
using MultiTenantAuth.AspNetCore.Validators;

namespace MultiTenantAuth.AspNetCore.Tests.Validators;

public class DefaultTenantValidatorTests
{
    private static IOptions<MultiTenantAuthOptions> Opts(Action<MultiTenantAuthOptions>? configure = null)
    {
        var o = new MultiTenantAuthOptions();
        configure?.Invoke(o);
        return Microsoft.Extensions.Options.Options.Create(o);
    }

    private static TenantContext Tenant(string id) =>
        new() { TenantId = id, IsResolved = true };

    private static ClaimsPrincipal AuthUser(params (string type, string value)[] claims)
    {
        var identity = new ClaimsIdentity(
            claims.Select(c => new Claim(c.type, c.value)),
            "Test");
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal AnonUser() => new();

    [Fact]
    public async Task Validate_AuthenticatedUserWithMatchingClaim_Succeeds()
    {
        var validator = new DefaultTenantValidator(Opts());
        var user = AuthUser(("tenant_id", "acme"));

        var result = await validator.ValidateAsync(Tenant("acme"), user, new DefaultHttpContext());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Validate_Unauthenticated_Returns401()
    {
        var validator = new DefaultTenantValidator(Opts());
        var result = await validator.ValidateAsync(Tenant("acme"), AnonUser(), new DefaultHttpContext());

        Assert.False(result.Succeeded);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task Validate_AuthenticatedButWrongTenant_Returns403()
    {
        var validator = new DefaultTenantValidator(Opts());
        var user = AuthUser(("tenant_id", "other-tenant"));

        var result = await validator.ValidateAsync(Tenant("acme"), user, new DefaultHttpContext());

        Assert.False(result.Succeeded);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task Validate_UserInAllowedTenantsClaim_Succeeds()
    {
        var validator = new DefaultTenantValidator(Opts());
        var user = AuthUser(("allowed_tenants", "acme,beta,gamma"));

        var result = await validator.ValidateAsync(Tenant("beta"), user, new DefaultHttpContext());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Validate_UserWithMultipleTenantClaims_MatchingOne_Succeeds()
    {
        var validator = new DefaultTenantValidator(Opts());
        var identity = new ClaimsIdentity(
        [
            new Claim("tenant_id", "alpha"),
            new Claim("tenant_id", "beta"),
            new Claim("tenant_id", "gamma")
        ], "Test");
        var user = new ClaimsPrincipal(identity);

        var result = await validator.ValidateAsync(Tenant("beta"), user, new DefaultHttpContext());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Validate_AuthNotRequired_AnonymousAllowed()
    {
        var validator = new DefaultTenantValidator(Opts(o =>
        {
            o.RequireAuthenticatedUser = false;
            o.RequireTenantClaim = false;
        }));

        var result = await validator.ValidateAsync(Tenant("acme"), AnonUser(), new DefaultHttpContext());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Validate_ClaimNotRequired_SkipsClaimCheck()
    {
        var validator = new DefaultTenantValidator(Opts(o => o.RequireTenantClaim = false));
        var user = AuthUser(("tenant_id", "something-else"));

        var result = await validator.ValidateAsync(Tenant("acme"), user, new DefaultHttpContext());

        Assert.True(result.Succeeded);
    }
}
