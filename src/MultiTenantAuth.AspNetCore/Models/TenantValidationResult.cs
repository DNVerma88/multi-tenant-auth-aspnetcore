namespace MultiTenantAuth.AspNetCore.Models;

/// <summary>
/// Carries the outcome of a tenant validation attempt against the authenticated user.
/// </summary>
public sealed class TenantValidationResult
{
    /// <summary>Gets a result indicating the user is authorised for this tenant.</summary>
    public static TenantValidationResult Success() =>
        new() { Succeeded = true };

    /// <summary>Gets a result indicating the user is not authorised for this tenant.</summary>
    public static TenantValidationResult Fail(int statusCode, string reason) =>
        new() { Succeeded = false, StatusCode = statusCode, FailureReason = reason };

    /// <summary>Whether validation passed.</summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// HTTP status code to return on failure.
    /// Typically 401 (unauthenticated) or 403 (authenticated but unauthorised).
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>Internal failure reason. Never exposed directly in HTTP responses.</summary>
    public string? FailureReason { get; init; }
}
