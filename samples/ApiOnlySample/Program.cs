using MultiTenantAuth.AspNetCore.Abstractions;
using MultiTenantAuth.AspNetCore.Extensions;
using MultiTenantAuth.AspNetCore.Options;

// ---------------------------------------------------------------------------
// ApiOnlySample — demonstrates header + claim-based tenant validation.
// Send X-Tenant-Id header and a bearer token whose tenant_id claim must match.
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication("Bearer");
    // In a real application, add JWT bearer authentication:
    // .AddJwtBearer(options => { ... });
    // Requires Microsoft.AspNetCore.Authentication.JwtBearer package.

builder.Services.AddAuthorization();

builder.Services.AddMultiTenantAuth(options =>
{
    // Accept tenant from header first; fall back to claim on the token.
    options.ResolutionOrder =
    [
        TenantResolutionStrategy.Header,
        TenantResolutionStrategy.Claim
    ];

    options.TenantHeaderName = "X-Tenant-Id";
    options.TenantClaimType = "tenant_id";
    options.AllowedTenantsClaimType = "allowed_tenants";

    options.RequireAuthenticatedUser = true;
    options.RequireTenantClaim = true;
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMultiTenantAuth();
app.UseAuthorization();

app.MapGet("/api/projects", (ITenantContextAccessor accessor) =>
{
    return Results.Ok(new
    {
        Tenant = accessor.Current?.TenantId,
        ResolvedFrom = accessor.Current?.Source,
        Data = new[] { "Project X", "Project Y" }
    });
}).RequireAuthorization();

app.Run();
