namespace MultiTenantAuth.AspNetCore.Models;

/// <summary>
/// Carries the outcome of a tenant resolution attempt.
/// </summary>
public sealed class TenantResolutionResult
{
    /// <summary>Gets a result that indicates successful resolution.</summary>
    public static TenantResolutionResult Success(TenantContext tenant) =>
        new() { Succeeded = true, Tenant = tenant };

    /// <summary>Gets a result that indicates the tenant could not be resolved.</summary>
    public static TenantResolutionResult Fail(string reason) =>
        new() { Succeeded = false, FailureReason = reason };

    /// <summary>Whether the tenant was resolved.</summary>
    public bool Succeeded { get; init; }

    /// <summary>The resolved tenant context; null when <see cref="Succeeded"/> is false.</summary>
    public TenantContext? Tenant { get; init; }

    /// <summary>Internal failure reason. Never exposed directly in HTTP responses.</summary>
    public string? FailureReason { get; init; }
}
