using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Abstractions;
using MultiTenantAuth.AspNetCore.Constants;
using MultiTenantAuth.AspNetCore.Internal;
using MultiTenantAuth.AspNetCore.Models;
using MultiTenantAuth.AspNetCore.Options;
using MultiTenantAuth.AspNetCore.Resolvers;
using MultiTenantAuth.AspNetCore.Validators;

namespace MultiTenantAuth.AspNetCore.Middleware;

/// <summary>
/// ASP.NET Core middleware that resolves the tenant context and enforces
/// tenant-aware authentication / authorization for every request.
/// </summary>
/// <remarks>
/// Recommended pipeline order:
/// <code>
/// app.UseRouting();
/// app.UseAuthentication();
/// app.UseMultiTenantAuth();   // ← here
/// app.UseAuthorization();
/// app.MapControllers();
/// </code>
/// </remarks>
public sealed class MultiTenantAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MultiTenantAuthMiddleware> _logger;
    private readonly MultiTenantAuthOptions _options;

    /// <summary>Initialises a new instance of <see cref="MultiTenantAuthMiddleware"/>.</summary>
    public MultiTenantAuthMiddleware(
        RequestDelegate next,
        IOptions<MultiTenantAuthOptions> options,
        ILogger<MultiTenantAuthMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;

        WarnIfInsecureConfiguration();
    }

    private void WarnIfInsecureConfiguration()
    {
        if (!_options.RequireAuthenticatedUser)
            _logger.LogWarning(
                "MultiTenantAuth: {Setting} is disabled. All requests will pass " +
                "through without authentication checks. Do NOT use this in production.",
                nameof(_options.RequireAuthenticatedUser));

        if (!_options.RequireResolvedTenant)
            _logger.LogWarning(
                "MultiTenantAuth: {Setting} is disabled. Requests without a " +
                "resolvable tenant will proceed. Ensure downstream code handles null tenant context.",
                nameof(_options.RequireResolvedTenant));

        if (_options.EnableQueryStringResolution)
            _logger.LogWarning(
                "MultiTenantAuth: {Setting} is enabled. Query-string tenant values " +
                "are easily manipulated. Ensure additional protections are in place.",
                nameof(_options.EnableQueryStringResolution));
    }

    /// <summary>Processes the request.</summary>
    public async Task InvokeAsync(
        HttpContext context,
        ITenantContextAccessor tenantContextAccessor,
        IServiceProvider serviceProvider)
    {
        // Clear any pre-existing context to prevent cross-request leakage.
        tenantContextAccessor.Current = null;

        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // --- Resolution ---
        var resolutionResult = await ResolveAsync(context, serviceProvider);

        if (!resolutionResult.Succeeded || resolutionResult.Tenant is null)
        {
            if (_options.RequireResolvedTenant)
            {
                _logger.LogWarning("Tenant resolution failed. Reason (internal): {Reason}",
                    resolutionResult.FailureReason);

                var statusCode = _options.ReturnNotFoundForUnknownTenant
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest;

                context.Response.StatusCode = statusCode;
                return;
            }

            // Tenant not required – continue without context.
            await _next(context);
            return;
        }

        var tenant = resolutionResult.Tenant;

        // --- Normalise tenant ID to prevent case-confusion attacks ---
        if (_options.NormalizeTenantIdToLowercase
            && !string.IsNullOrEmpty(tenant.TenantId)
            && tenant.TenantId != tenant.TenantId.ToLowerInvariant())
        {
            tenant = new TenantContext
            {
                TenantId = tenant.TenantId.ToLowerInvariant(),
                TenantSlug = tenant.TenantSlug?.ToLowerInvariant(),
                Source = tenant.Source,
                IsResolved = tenant.IsResolved
            };
        }

        // --- Format validation ---
        if (!TenantFormatValidator.IsValid(tenant.TenantId, _options))
        {
            _logger.LogWarning("Tenant id failed format validation.");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (tenant.TenantSlug is not null
            && !TenantFormatValidator.IsValidSlug(tenant.TenantSlug, _options))
        {
            _logger.LogWarning("Tenant slug failed format validation.");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // --- Store context ---
        tenantContextAccessor.Current = tenant;

        try
        {
            // --- Validation ---
            var validator = GetValidator(serviceProvider);
            var validationResult = await validator.ValidateAsync(
                tenant, context.User, context, context.RequestAborted);

            if (!validationResult.Succeeded)
            {
                _logger.LogWarning("Tenant validation failed. Reason (internal): {Reason}",
                    validationResult.FailureReason);

                context.Response.StatusCode =
                    validationResult.StatusCode ?? StatusCodes.Status403Forbidden;
                return;
            }

            await _next(context);
        }
        finally
        {
            // Always clear context — on success, failure, or exception — to prevent
            // any downstream middleware or error handler from observing a stale tenant.
            tenantContextAccessor.Current = null;
        }
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private async ValueTask<TenantResolutionResult> ResolveAsync(
        HttpContext context,
        IServiceProvider serviceProvider)
    {
        if (_options.ResolutionOrder is not { Length: > 0 })
            return TenantResolutionResult.Fail("ResolutionOrder is empty or null.");

        foreach (var strategy in _options.ResolutionOrder)
        {
            var resolver = GetResolver(strategy, serviceProvider);
            if (resolver is null)
                continue;

            var result = await resolver.ResolveAsync(context, context.RequestAborted);
            if (result.Succeeded)
                return result;
        }

        return TenantResolutionResult.Fail("No strategy resolved a tenant.");
    }

    private ITenantResolver? GetResolver(TenantResolutionStrategy strategy, IServiceProvider sp)
    {
        switch (strategy)
        {
            case TenantResolutionStrategy.Header:
                return new HeaderTenantResolver(Microsoft.Extensions.Options.Options.Create(_options));
            case TenantResolutionStrategy.RouteValue:
                return new RouteValueTenantResolver(Microsoft.Extensions.Options.Options.Create(_options));
            case TenantResolutionStrategy.Subdomain:
                return new SubdomainTenantResolver(Microsoft.Extensions.Options.Options.Create(_options));
            case TenantResolutionStrategy.Claim:
                return new ClaimTenantResolver(Microsoft.Extensions.Options.Options.Create(_options));
            case TenantResolutionStrategy.QueryString:
                return new QueryStringTenantResolver(Microsoft.Extensions.Options.Options.Create(_options));
            case TenantResolutionStrategy.Custom:
                return ResolveCustomResolver(sp);
            case TenantResolutionStrategy.None:
                return null;  // Explicit no-op; callers skip null resolvers.
            default:
                _logger.LogWarning(
                    "Unknown TenantResolutionStrategy value {Strategy} in ResolutionOrder — skipping.",
                    strategy);
                return null;
        }
    }

    private ITenantResolver? ResolveCustomResolver(IServiceProvider sp)
    {
        if (_options.CustomResolverType is null)
            return null;

        // Resolve only the exact registered type — no silent fallback to a different
        // ITenantResolver, which could lead to the wrong resolver being invoked.
        var resolver = sp.GetService(_options.CustomResolverType) as ITenantResolver;
        if (resolver is null)
            _logger.LogWarning(
                "CustomResolverType '{Type}' is not registered in DI. " +
                "Register it with services.AddSingleton<{TypeShort}>(...).",
                _options.CustomResolverType.FullName,
                _options.CustomResolverType.Name);
        return resolver;
    }

    private ITenantValidator GetValidator(IServiceProvider sp)
    {
        if (_options.CustomValidatorType is not null)
        {
            var custom = (sp.GetService(_options.CustomValidatorType) as ITenantValidator)
                ?? sp.GetService<ITenantValidator>();
            if (custom is not null)
                return custom;
        }

        return new DefaultTenantValidator(Microsoft.Extensions.Options.Options.Create(_options));
    }
}
