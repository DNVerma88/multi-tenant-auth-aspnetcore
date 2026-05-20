using System.Text.RegularExpressions;
using MultiTenantAuth.AspNetCore.Options;

namespace MultiTenantAuth.AspNetCore.Internal;

/// <summary>
/// Validates tenant identifier format to prevent injection, spoofing, or oversized inputs.
/// Uses simple length checks first to avoid regex overhead on obviously invalid values.
/// </summary>
internal static class TenantFormatValidator
{
    // Compiled static regex for performance on the hot path.
    // Recompiled only if the pattern changes from the default.
    private static readonly Regex _defaultPattern = new(
        MultiTenantAuthOptions_DefaultPattern,
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(50));

    private const string MultiTenantAuthOptions_DefaultPattern = @"^[a-zA-Z0-9\-_]+$";

    /// <summary>
    /// Returns true when <paramref name="value"/> is a well-formed tenant id.
    /// </summary>
    internal static bool IsValid(string? value, MultiTenantAuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.Length > options.MaxTenantIdLength)
            return false;

        return MatchesPattern(value, options.AllowedTenantPattern);
    }

    /// <summary>
    /// Returns true when <paramref name="value"/> is a well-formed tenant slug.
    /// </summary>
    internal static bool IsValidSlug(string? value, MultiTenantAuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.Length > options.MaxTenantSlugLength)
            return false;

        return MatchesPattern(value, options.AllowedTenantPattern);
    }

    private static bool MatchesPattern(string value, string? pattern)
    {
        if (pattern is null)
            return true;

        if (pattern == MultiTenantAuthOptions_DefaultPattern)
            return _defaultPattern.IsMatch(value);

        // Custom pattern – create a one-off Regex with a timeout to prevent ReDoS.
        try
        {
            return Regex.IsMatch(value, pattern,
                RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(50));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
