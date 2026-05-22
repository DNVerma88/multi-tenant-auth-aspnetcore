using MultiTenantAuth.AspNetCore.Internal;
using MultiTenantAuth.AspNetCore.Options;

namespace MultiTenantAuth.AspNetCore.Tests.Internal;

public class TenantFormatValidatorTests
{
    private static MultiTenantAuthOptions DefaultOpts() => new();

    [Theory]
    [InlineData("acme")]
    [InlineData("ACME-corp")]
    [InlineData("tenant_123")]
    [InlineData("ab")]  // minimum valid length (MinTenantIdLength = 2)
    public void IsValid_ValidIds_ReturnsTrue(string id)
    {
        Assert.True(TenantFormatValidator.IsValid(id, DefaultOpts()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("tenant with spaces")]
    [InlineData("tenant;drop")]
    [InlineData("tenant@bad")]
    public void IsValid_InvalidIds_ReturnsFalse(string? id)
    {
        Assert.False(TenantFormatValidator.IsValid(id, DefaultOpts()));
    }

    [Fact]
    public void IsValid_ExceedsMaxLength_ReturnsFalse()
    {
        var longId = new string('a', 65);
        Assert.False(TenantFormatValidator.IsValid(longId, DefaultOpts()));
    }

    [Fact]
    public void IsValid_BelowMinLength_ReturnsFalse()
    {
        // Single-char IDs are rejected by default (MinTenantIdLength = 2).
        Assert.False(TenantFormatValidator.IsValid("a", DefaultOpts()));
    }

    [Fact]
    public void IsValid_ExactMaxLength_ReturnsTrue()
    {
        var id = new string('a', 64);
        Assert.True(TenantFormatValidator.IsValid(id, DefaultOpts()));
    }

    [Fact]
    public void IsValid_NullPattern_SkipsPatternCheck()
    {
        var opts = DefaultOpts();
        opts.AllowedTenantPattern = null;

        Assert.True(TenantFormatValidator.IsValid("tenant with spaces!", opts));
    }

    [Fact]
    public void IsValidSlug_ValidSlug_ReturnsTrue()
    {
        Assert.True(TenantFormatValidator.IsValidSlug("my-tenant", DefaultOpts()));
    }

    [Fact]
    public void IsValidSlug_ExceedsMaxLength_ReturnsFalse()
    {
        var slug = new string('a', 101);
        Assert.False(TenantFormatValidator.IsValidSlug(slug, DefaultOpts()));
    }

    [Fact]
    public void IsValidSlug_BelowMinLength_ReturnsFalse()
    {
        // Single-char slugs are rejected (MinTenantIdLength = 2 applies to slugs too).
        Assert.False(TenantFormatValidator.IsValidSlug("a", DefaultOpts()));
    }

    [Fact]
    public void IsValid_CustomPattern_MatchesCustomRule()
    {
        // Exercises the non-default regex path in MatchesPattern.
        var opts = DefaultOpts();
        opts.AllowedTenantPattern = @"^[a-z]+$";  // lowercase only

        Assert.True(TenantFormatValidator.IsValid("acme", opts));
        Assert.False(TenantFormatValidator.IsValid("ACME", opts));
    }
}
