using MultiTenantAuth.AspNetCore.Internal;
using MultiTenantAuth.AspNetCore.Models;

namespace MultiTenantAuth.AspNetCore.Tests.Internal;

public class TenantContextAccessorTests
{
    [Fact]
    public void Current_DefaultsToNull()
    {
        var accessor = new TenantContextAccessor();
        Assert.Null(accessor.Current);
    }

    [Fact]
    public void Current_CanBeSet()
    {
        var accessor = new TenantContextAccessor();
        var tenant = new TenantContext { TenantId = "acme", IsResolved = true };
        accessor.Current = tenant;

        Assert.Equal("acme", accessor.Current?.TenantId);
    }

    [Fact]
    public void Current_CanBeCleared()
    {
        var accessor = new TenantContextAccessor();
        accessor.Current = new TenantContext { TenantId = "acme", IsResolved = true };
        accessor.Current = null;

        Assert.Null(accessor.Current);
    }

    [Fact]
    public async Task Current_IsIsolatedAcrossAsyncContexts()
    {
        var accessor = new TenantContextAccessor();

        var task1Tenant = new TenantContext { TenantId = "tenant-1", IsResolved = true };
        var task2Tenant = new TenantContext { TenantId = "tenant-2", IsResolved = true };

        string? task1Read = null;
        string? task2Read = null;

        var t1 = Task.Run(async () =>
        {
            accessor.Current = task1Tenant;
            await Task.Delay(20);
            task1Read = accessor.Current?.TenantId;
        });

        var t2 = Task.Run(async () =>
        {
            accessor.Current = task2Tenant;
            await Task.Delay(20);
            task2Read = accessor.Current?.TenantId;
        });

        await Task.WhenAll(t1, t2);

        // Each async context should have its own isolated value.
        Assert.Equal("tenant-1", task1Read);
        Assert.Equal("tenant-2", task2Read);
    }
}
