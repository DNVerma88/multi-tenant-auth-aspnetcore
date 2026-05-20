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
                _logger.LogInformation("Tenant resolution failed. Reason (internal): {Reason}",
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

        // --- Format validation ---
        if (!TenantFormatValidator.IsValid(tenant.TenantId, _options))
        {
            _logger.LogInformation("Tenant id failed format validation.");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (tenant.TenantSlug is not null
            && !TenantFormatValidator.IsValidSlug(tenant.TenantSlug, _options))
        {
            _logger.LogInformation("Tenant slug failed format validation.");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // --- Store context ---
        tenantContextAccessor.Current = tenant;

        // --- Validation ---
        var validator = GetValidator(serviceProvider);
        var validationResult = await validator.ValidateAsync(
            tenant, context.User, context, context.RequestAborted);

        if (!validationResult.Succeeded)
        {
            _logger.LogInformation("Tenant validation failed. Reason (internal): {Reason}",
                validationResult.FailureReason);

            context.Response.StatusCode =
                validationResult.StatusCode ?? StatusCodes.Status403Forbidden;
            return;
        }

        await _next(context);

        // Clear context after response to prevent cross-context leakage.
        tenantContextAccessor.Current = null;
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private async ValueTask<TenantResolutionResult> ResolveAsync(
        HttpContext context,
        IServiceProvider serviceProvider)
    {
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
        return strategy switch
        {
            TenantResolutionStrategy.Header =>
                new HeaderTenantResolver(Microsoft.Extensions.Options.Options.Create(_options)),
            TenantResolutionStrategy.RouteValue =>
                new RouteValueTenantResolver(Microsoft.Extensions.Options.Options.Create(_options)),
            TenantResolutionStrategy.Subdomain =>
                new SubdomainTenantResolver(Microsoft.Extensions.Options.Options.Create(_options)),
            TenantResolutionStrategy.Claim =>
                new ClaimTenantResolver(Microsoft.Extensions.Options.Options.Create(_options)),
            TenantResolutionStrategy.QueryString =>
                new QueryStringTenantResolver(Microsoft.Extensions.Options.Options.Create(_options)),
            TenantResolutionStrategy.Custom =>
                ResolveCustomResolver(sp),
            _ => null
        };
    }

    private ITenantResolver? ResolveCustomResolver(IServiceProvider sp)
    {
        if (_options.CustomResolverType is null)
            return null;

        // Try the exact registered type first, then fall back to ITenantResolver.
        return (sp.GetService(_options.CustomResolverType) as ITenantResolver)
            ?? sp.GetService<ITenantResolver>();
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
