using MultiTenantAuth.AspNetCore.Abstractions;
using MultiTenantAuth.AspNetCore.Extensions;
using MultiTenantAuth.AspNetCore.Options;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------
// Register multi-tenant auth services
// Tenant resolved from route value → header → subdomain, in that order.
// RequireAuthenticatedUser = false here so the sample runs without a real
// authentication provider; in production, always set this to true.
// -----------------------------------------------------------------------
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddMultiTenantAuth(options =>
{
    options.ResolutionOrder =
    [
        TenantResolutionStrategy.RouteValue,
        TenantResolutionStrategy.Header,
        TenantResolutionStrategy.Subdomain
    ];

    // Disable authentication requirement for this demo sample only.
    options.RequireAuthenticatedUser = false;
    options.RequireTenantClaim = false;
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();

// UseMultiTenantAuth must come after UseAuthentication and before UseAuthorization.
app.UseMultiTenantAuth();
app.UseAuthorization();

// -----------------------------------------------------------------------
// Endpoints
// -----------------------------------------------------------------------

// Route-based tenant resolution: GET /api/{tenantId}/projects
app.MapGet("/api/{tenantId}/projects", (
    ITenantContextAccessor tenantContextAccessor) =>
{
    var tenant = tenantContextAccessor.Current;
    return Results.Ok(new
    {
        TenantId = tenant?.TenantId,
        Source = tenant?.Source,
        Projects = new[] { "Project A", "Project B" }
    });
});

// Header-based tenant resolution: GET /api/data (send X-Tenant-Id header)
app.MapGet("/api/data", (ITenantContextAccessor tenantContextAccessor) =>
{
    var tenant = tenantContextAccessor.Current;
    return Results.Ok(new
    {
        TenantId = tenant?.TenantId,
        Source = tenant?.Source,
        Message = "Data for tenant"
    });
});

app.Run();


record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
