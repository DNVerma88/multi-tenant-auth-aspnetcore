using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MultiTenantAuth.AspNetCore.Abstractions;
using MultiTenantAuth.AspNetCore.Internal;
using MultiTenantAuth.AspNetCore.Options;

namespace MultiTenantAuth.AspNetCore.Extensions;

/// <summary>
/// Extension methods for registering multi-tenant auth services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers multi-tenant auth services with default options.
    /// </summary>
    public static IServiceCollection AddMultiTenantAuth(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddMultiTenantAuth(_ => { });
    }

    /// <summary>
    /// Registers multi-tenant auth services with the provided configuration delegate.
    /// </summary>
    public static IServiceCollection AddMultiTenantAuth(
        this IServiceCollection services,
        Action<MultiTenantAuthOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        // Register the accessor as singleton; AsyncLocal gives per-request isolation.
        services.TryAddSingleton<ITenantContextAccessor, TenantContextAccessor>();

        return services;
    }
}
