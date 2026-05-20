using MultiTenantAuth.AspNetCore.Models;

namespace MultiTenantAuth.AspNetCore.Abstractions;

/// <summary>
/// Provides access to the <see cref="TenantContext"/> for the current request.
/// The context is scoped to the async execution context (AsyncLocal) and cleared
/// automatically after the request completes.
/// </summary>
public interface ITenantContextAccessor
{
    /// <summary>
    /// Gets or sets the <see cref="TenantContext"/> for the current request.
    /// Returns null before the middleware has run or when resolution failed.
    /// </summary>
    TenantContext? Current { get; set; }
}
