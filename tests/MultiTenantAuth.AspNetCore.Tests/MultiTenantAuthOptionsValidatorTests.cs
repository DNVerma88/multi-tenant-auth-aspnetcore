using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Options;

namespace MultiTenantAuth.AspNetCore.Tests;

public class MultiTenantAuthOptionsValidatorTests
{
    private static ValidateOptionsResult Validate(Action<MultiTenantAuthOptions> configure)
    {
        var opts = new MultiTenantAuthOptions();
        configure(opts);
        return new MultiTenantAuthOptionsValidator().Validate(null, opts);
    }

    [Fact]
    public void DefaultOptions_AreValid()
    {
        var result = Validate(_ => { });
        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxTenantIdLength_LessThanOrEqualZero_Fails(int value)
    {
        var result = Validate(o => o.MaxTenantIdLength = value);
        Assert.NotEqual(ValidateOptionsResult.Success, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void MaxTenantSlugLength_LessThanOrEqualZero_Fails(int value)
    {
        var result = Validate(o => o.MaxTenantSlugLength = value);
        Assert.NotEqual(ValidateOptionsResult.Success, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TenantHeaderName_NullOrWhitespace_Fails(string? value)
    {
        var result = Validate(o => o.TenantHeaderName = value!);
        Assert.NotEqual(ValidateOptionsResult.Success, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TenantClaimType_NullOrWhitespace_Fails(string? value)
    {
        var result = Validate(o => o.TenantClaimType = value!);
        Assert.NotEqual(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void AllowedTenantPattern_InvalidRegex_Fails()
    {
        var result = Validate(o => o.AllowedTenantPattern = "[invalid(regex");
        Assert.NotEqual(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void AllowedTenantPattern_Null_IsValid()
    {
        var result = Validate(o => o.AllowedTenantPattern = null);
        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void AllowedTenantPattern_ValidRegex_IsValid()
    {
        var result = Validate(o => o.AllowedTenantPattern = @"^[a-z]+$");
        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void MultipleInvalidValues_ReportsAllFailures()
    {
        var result = Validate(o =>
        {
            o.MaxTenantIdLength = -1;
            o.TenantHeaderName = "";
            o.AllowedTenantPattern = "[bad";
        });

        Assert.NotEqual(ValidateOptionsResult.Success, result);
        // Should report all three failures, not just the first.
        Assert.True(result.Failures?.Count() >= 3);
    }
}
