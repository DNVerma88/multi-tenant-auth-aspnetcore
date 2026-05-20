using MultiTenantAuth.AspNetCore.Models;
using Microsoft.AspNetCore.Http;

namespace MultiTenantAuth.AspNetCore.Abstractions;

/// <summary>
/// Resolves a tenant from the current HTTP request.
/// Implement this interface and register it in DI to supply a custom resolution strategy.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Attempts to resolve a tenant from the given <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The current HTTP request context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="TenantResolutionResult"/> describing whether resolution succeeded.
    /// </returns>
    ValueTask<TenantResolutionResult> ResolveAsync(
        HttpContext context,
        CancellationToken cancellationToken = default);
}
