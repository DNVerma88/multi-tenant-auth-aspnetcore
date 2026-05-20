using MultiTenantAuth.AspNetCore.Abstractions;
using MultiTenantAuth.AspNetCore.Extensions;
using MultiTenantAuth.AspNetCore.Options;

// ---------------------------------------------------------------------------
// SubdomainTenantSample — resolves the tenant from the request subdomain.
// Example: acme.yoursaas.com → TenantId = "acme"
// Run with custom hosts file entries or a wildcard DNS for local testing.
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddMultiTenantAuth(options =>
{
    options.ResolutionOrder = [TenantResolutionStrategy.Subdomain];

    // Demo: do not require authentication so the sample can run standalone.
    options.RequireAuthenticatedUser = false;
    options.RequireTenantClaim = false;

    // Slug validation: letters, digits, hyphen, underscore only.
    options.AllowedTenantPattern = @"^[a-zA-Z0-9\-_]+$";
    options.MaxTenantIdLength = 63; // DNS label max length
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMultiTenantAuth();
app.UseAuthorization();

app.MapGet("/", (ITenantContextAccessor accessor, HttpContext http) =>
{
    var tenant = accessor.Current;
    return Results.Ok(new
    {
        Host = http.Request.Host.Value,
        TenantId = tenant?.TenantId,
        TenantSlug = tenant?.TenantSlug,
        ResolvedFrom = tenant?.Source,
        Message = tenant is not null
            ? $"Welcome to tenant: {tenant.TenantId}"
            : "No tenant resolved"
    });
});

app.Run();
