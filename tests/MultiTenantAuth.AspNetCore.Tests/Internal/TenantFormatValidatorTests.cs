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
    [InlineData("a")]
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
}
