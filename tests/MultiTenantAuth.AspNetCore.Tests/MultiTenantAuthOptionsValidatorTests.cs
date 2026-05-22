using Microsoft.Extensions.Options;
using MultiTenantAuth.AspNetCore.Abstractions;
using MultiTenantAuth.AspNetCore.Models;
using MultiTenantAuth.AspNetCore.Options;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

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

    // ── MinTenantIdLength ────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MinTenantIdLength_LessThanOrEqualZero_Fails(int value)
    {
        var result = Validate(o => o.MinTenantIdLength = value);
        Assert.NotEqual(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void MinTenantIdLength_ExceedsMaxTenantIdLength_Fails()
    {
        var result = Validate(o =>
        {
            o.MinTenantIdLength = 100;
            o.MaxTenantIdLength = 10;
        });
        Assert.NotEqual(ValidateOptionsResult.Success, result);
    }

    // ── String name properties ───────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TenantRouteValueName_NullOrWhitespace_Fails(string? value)
    {
        var result = Validate(o => o.TenantRouteValueName = value!);
        Assert.NotEqual(ValidateOptionsResult.Success, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AllowedTenantsClaimType_NullOrWhitespace_Fails(string? value)
    {
        var result = Validate(o => o.AllowedTenantsClaimType = value!);
        Assert.NotEqual(ValidateOptionsResult.Success, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TenantQueryStringName_NullOrWhitespace_Fails(string? value)
    {
        var result = Validate(o => o.TenantQueryStringName = value!);
        Assert.NotEqual(ValidateOptionsResult.Success, result);
    }

    // ── CustomResolverType / CustomValidatorType ─────────────────────────────

    [Fact]
    public void CustomResolverType_NotImplementingITenantResolver_Fails()
    {
        // typeof(string) does not implement ITenantResolver.
        var result = Validate(o => o.CustomResolverType = typeof(string));
        Assert.NotEqual(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void CustomResolverType_ImplementingITenantResolver_Passes()
    {
        var result = Validate(o => o.CustomResolverType = typeof(OptionsValidatorTestDoubles.StubResolver));
        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void CustomValidatorType_NotImplementingITenantValidator_Fails()
    {
        // typeof(int) does not implement ITenantValidator.
        var result = Validate(o => o.CustomValidatorType = typeof(int));
        Assert.NotEqual(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void CustomValidatorType_ImplementingITenantValidator_Passes()
    {
        var result = Validate(o => o.CustomValidatorType = typeof(OptionsValidatorTestDoubles.StubValidator));
        Assert.Equal(ValidateOptionsResult.Success, result);
    }
}

// ---------------------------------------------------------------------------
// Minimal test doubles — used only for typeof(); never instantiated.
// ---------------------------------------------------------------------------
internal static class OptionsValidatorTestDoubles
{
    internal sealed class StubResolver : ITenantResolver
    {
        public ValueTask<TenantResolutionResult> ResolveAsync(
            HttpContext context,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    internal sealed class StubValidator : ITenantValidator
    {
        public ValueTask<TenantValidationResult> ValidateAsync(
            TenantContext tenant,
            ClaimsPrincipal user,
            HttpContext context,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
