# MultiTenantAuth.AspNetCore

[![NuGet](https://img.shields.io/nuget/v/MultiTenantAuth.AspNetCore.svg)](https://www.nuget.org/packages/MultiTenantAuth.AspNetCore)
[![CI](https://github.com/DNVerma88/multi-tenant-auth-aspnetcore/actions/workflows/ci.yml/badge.svg)](https://github.com/DNVerma88/multi-tenant-auth-aspnetcore/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Lightweight, production-ready **multi-tenant authentication and authorization middleware** for ASP.NET Core.

Resolve tenant context from subdomain, header, route value, claim, or a fully custom strategy. Enforce tenant-aware access control in SaaS applications — without third-party runtime dependencies.

---

## When to use this package

- You are building a **SaaS application** where users belong to one or more tenants.
- You need to **resolve the tenant** from each HTTP request automatically.
- You need to **validate** that the authenticated user is allowed to access the resolved tenant.
- You want a **lightweight, dependency-free** solution that plugs into standard ASP.NET Core middleware.

## When NOT to use this package

- You need a full-featured multi-tenancy framework (database-per-tenant, full DI scoping, etc.) — consider Finbuckle.MultiTenant or similar.
- You are building a single-tenant application.
- You need tenant resolution before routing (e.g. to select a connection string) — the tenant is resolved inside middleware, which runs after routing.

---

## Installation

```bash
dotnet add package MultiTenantAuth.AspNetCore
```

---

## Quick Start

```csharp
// Program.cs

builder.Services.AddAuthentication(/* ... */);
builder.Services.AddAuthorization();

builder.Services.AddMultiTenantAuth();   // ← default options

var app = builder.Build();

app.UseAuthentication();
app.UseMultiTenantAuth();                // ← must be after UseAuthentication
app.UseAuthorization();

app.MapControllers();
```

---

## Middleware Order

> **Important**: middleware order matters.

```csharp
app.UseRouting();

app.UseAuthentication();     // 1. Authenticate the user (populate HttpContext.User)

app.UseMultiTenantAuth();    // 2. Resolve tenant; validate user belongs to tenant

app.UseAuthorization();      // 3. Enforce [Authorize] policies

app.MapControllers();        // 4. Route to endpoints
```

`UseAuthentication` must run first so that the user's claims are available when the tenant is validated.  
`UseMultiTenantAuth` must run before `UseAuthorization` so the tenant context is available to authorization policies.

---

## Configuration

```csharp
builder.Services.AddMultiTenantAuth(options =>
{
    // Resolution order: first strategy that finds a tenant wins.
    options.ResolutionOrder = new[]
    {
        TenantResolutionStrategy.RouteValue,
        TenantResolutionStrategy.Header,
        TenantResolutionStrategy.Subdomain,
        TenantResolutionStrategy.Claim
    };

    options.TenantHeaderName        = "X-Tenant-Id";     // default
    options.TenantRouteValueName    = "tenantId";         // default
    options.TenantClaimType         = "tenant_id";        // default
    options.AllowedTenantsClaimType = "allowed_tenants";  // default

    options.RequireAuthenticatedUser = true;   // 401 if user is not authenticated
    options.RequireResolvedTenant    = true;   // 400 if tenant cannot be resolved
    options.RequireTenantClaim       = true;   // 403 if user's claim doesn't match tenant

    options.MaxTenantIdLength    = 64;
    options.MaxTenantSlugLength  = 100;
    options.AllowedTenantPattern = @"^[a-zA-Z0-9\-_]+$";  // default
});
```

---

## Accessing the Tenant in Code

Inject `ITenantContextAccessor` anywhere in your application:

```csharp
public class ProjectService
{
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public ProjectService(ITenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<IEnumerable<Project>> GetProjectsAsync()
    {
        var tenantId = _tenantContextAccessor.Current?.TenantId
            ?? throw new InvalidOperationException("No tenant context.");

        return await _dbContext.Projects
            .Where(p => p.TenantId == tenantId)
            .ToListAsync();
    }
}
```

---

## Examples by Strategy

### Header-based tenant

```csharp
options.ResolutionOrder = [TenantResolutionStrategy.Header];
options.TenantHeaderName = "X-Tenant-Id";
```

HTTP request:
```
GET /api/projects HTTP/1.1
X-Tenant-Id: acme
Authorization: Bearer <token>
```

> **Security note**: Header values can be forged. Always combine header-based resolution with token claim validation (`RequireTenantClaim = true`).

### Subdomain-based tenant

```csharp
options.ResolutionOrder = [TenantResolutionStrategy.Subdomain];
```

Request to `acme.yoursaas.com` → `TenantId = "acme"`, `TenantSlug = "acme"`.

Works for any subdomain format. Single-label hosts (e.g. `localhost`) are skipped.

### Route-based tenant

```csharp
options.ResolutionOrder = [TenantResolutionStrategy.RouteValue];
options.TenantRouteValueName = "tenantId"; // default
```

```csharp
app.MapGet("/api/{tenantId}/projects", ...);
```

### Claims-based tenant (token-only)

```csharp
options.ResolutionOrder = [TenantResolutionStrategy.Claim];
options.TenantClaimType = "tenant_id";
```

The tenant is read from the `tenant_id` claim in the user's authenticated identity. Useful when every request carries a JWT with the tenant embedded.

---

## Claims Validation

By default, after resolving the tenant, the middleware checks that the authenticated user has a matching claim:

- **Primary claim** (`TenantClaimType`, default `tenant_id`) — exact match.
- **Multiple claims** — if the user has multiple `tenant_id` claims, any match succeeds.
- **Allowed tenants claim** (`AllowedTenantsClaimType`, default `allowed_tenants`) — may be a single value or a comma-separated list.

```json
{
  "sub": "user123",
  "tenant_id": "acme",
  "allowed_tenants": "acme,beta,gamma"
}
```

---

## Custom Resolver

```csharp
public class DatabaseTenantResolver : ITenantResolver
{
    private readonly ITenantRepository _repo;

    public DatabaseTenantResolver(ITenantRepository repo) => _repo = repo;

    public async ValueTask<TenantResolutionResult> ResolveAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        var host = context.Request.Host.Host;
        var tenant = await _repo.FindByHostAsync(host, cancellationToken);

        if (tenant is null)
            return TenantResolutionResult.Fail("Tenant not found for host.");

        return TenantResolutionResult.Success(new TenantContext
        {
            TenantId = tenant.Id,
            TenantSlug = tenant.Slug,
            Source = "database",
            IsResolved = true
        });
    }
}
```

Register and configure:

```csharp
builder.Services.AddSingleton<ITenantResolver, DatabaseTenantResolver>();

builder.Services.AddMultiTenantAuth(options =>
{
    options.ResolutionOrder = [TenantResolutionStrategy.Custom];
    options.CustomResolverType = typeof(DatabaseTenantResolver);
});
```

---

## Custom Validator

```csharp
public class RoleTenantValidator : ITenantValidator
{
    public ValueTask<TenantValidationResult> ValidateAsync(
        TenantContext tenant,
        ClaimsPrincipal user,
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole($"tenant:{tenant.TenantId}"))
        {
            return new(TenantValidationResult.Fail(
                StatusCodes.Status403Forbidden,
                "User does not have the required tenant role."));
        }

        return new(TenantValidationResult.Success());
    }
}
```

Register:

```csharp
builder.Services.AddSingleton<ITenantValidator, RoleTenantValidator>();

builder.Services.AddMultiTenantAuth(options =>
{
    options.CustomValidatorType = typeof(RoleTenantValidator);
});
```

---

## Query String Warning

> **Warning**: Query string tenant resolution is **disabled by default** and should remain so in most applications.

Query string values are:
- Visible in server logs, browser history, and referrer headers.
- Easily manipulated by end users and attackers.
- Susceptible to CSRF and link-sharing attacks.

Only enable if you have additional protections:

```csharp
// Only enable this when you fully understand the implications.
options.EnableQueryStringResolution = true;
options.ResolutionOrder = [TenantResolutionStrategy.QueryString];
```

---

## HTTP Status Behaviour

| Situation | Default status |
|-----------|---------------|
| Tenant format invalid | 400 Bad Request |
| Tenant not resolved (when required) | 400 Bad Request |
| Tenant not resolved, `ReturnNotFoundForUnknownTenant = true` | 404 Not Found |
| User not authenticated (when required) | 401 Unauthorized |
| User authenticated but not in tenant | 403 Forbidden |
| Custom validator returns status | As configured |

---

## SaaS Architecture Notes

- **Tenant isolation**: This library resolves and validates the tenant; it does not enforce data isolation. Apply tenant filters in your repositories/queries.
- **Database per tenant**: Combine this package with a custom `ITenantResolver` that selects a connection string based on the resolved tenant.
- **Caching**: The default resolvers do not cache. For subdomain lookups against a database, implement caching in your custom resolver.
- **Scoped DI**: The tenant context is stored in an `AsyncLocal` — it is automatically scoped to the current async execution context (one per request).

---

## Version Compatibility

| Package version | .NET version |
|-----------------|-------------|
| 1.x | .NET 8, .NET 10 |

The library is designed with stable ASP.NET Core abstractions so adding .NET 11+ should require only a `TargetFrameworks` change.

---

## Publishing to NuGet

```bash
dotnet pack src/MultiTenantAuth.AspNetCore/MultiTenantAuth.AspNetCore.csproj -c Release -o ./artifacts

dotnet nuget push ./artifacts/MultiTenantAuth.AspNetCore.1.0.0.nupkg \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

---

## Contributing

Contributions are welcome. Open issues and pull requests at [github.com/DNVerma88/multi-tenant-auth-aspnetcore](https://github.com/DNVerma88/multi-tenant-auth-aspnetcore). Please read [SECURITY.md](SECURITY.md) before submitting security-related changes.

---

## License

[MIT](LICENSE)
