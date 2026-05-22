using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace MultiTenantAuth.AspNetCore.Options;

/// <summary>
/// Validates <see cref="MultiTenantAuthOptions"/> at application startup so that
/// misconfigurations cause an immediate, descriptive error rather than a silent
/// runtime failure during request processing.
/// </summary>
internal sealed class MultiTenantAuthOptionsValidator : IValidateOptions<MultiTenantAuthOptions>
{
    public ValidateOptionsResult Validate(string? name, MultiTenantAuthOptions options)
    {
        var failures = new List<string>();

        if (options.MaxTenantIdLength <= 0)
            failures.Add($"{nameof(options.MaxTenantIdLength)} must be greater than zero.");

        if (options.MaxTenantSlugLength <= 0)
            failures.Add($"{nameof(options.MaxTenantSlugLength)} must be greater than zero.");

        if (string.IsNullOrWhiteSpace(options.TenantHeaderName))
            failures.Add($"{nameof(options.TenantHeaderName)} must not be null or whitespace.");

        if (string.IsNullOrWhiteSpace(options.TenantRouteValueName))
            failures.Add($"{nameof(options.TenantRouteValueName)} must not be null or whitespace.");

        if (string.IsNullOrWhiteSpace(options.TenantClaimType))
            failures.Add($"{nameof(options.TenantClaimType)} must not be null or whitespace.");

        if (string.IsNullOrWhiteSpace(options.AllowedTenantsClaimType))
            failures.Add($"{nameof(options.AllowedTenantsClaimType)} must not be null or whitespace.");

        if (options.AllowedTenantPattern is not null)
        {
            try
            {
                // Verify the pattern compiles; use a short timeout to catch catastrophic patterns.
                _ = new Regex(options.AllowedTenantPattern,
                    RegexOptions.None,
                    TimeSpan.FromMilliseconds(100));
            }
            catch (ArgumentException ex)
            {
                failures.Add($"{nameof(options.AllowedTenantPattern)} is not a valid regex: {ex.Message}");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
