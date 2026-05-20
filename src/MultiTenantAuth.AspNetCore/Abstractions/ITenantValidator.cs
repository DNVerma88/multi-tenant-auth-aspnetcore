using System.Security.Claims;
using MultiTenantAuth.AspNetCore.Models;
using Microsoft.AspNetCore.Http;

namespace MultiTenantAuth.AspNetCore.Abstractions;

/// <summary>
/// Validates that the authenticated user is permitted to access the resolved tenant.
/// Implement this interface to replace the built-in claims-based validator.
/// </summary>
public interface ITenantValidator
{
    /// <summary>
    /// Validates that <paramref name="user"/> is authorised for <paramref name="tenant"/>.
    /// </summary>
    /// <param name="tenant">The resolved tenant context.</param>
    /// <param name="user">The authenticated (or anonymous) claims principal.</param>
    /// <param name="context">The current HTTP request context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="TenantValidationResult"/> describing the outcome.
    /// </returns>
    ValueTask<TenantValidationResult> ValidateAsync(
        TenantContext tenant,
        ClaimsPrincipal user,
        HttpContext context,
        CancellationToken cancellationToken = default);
}
