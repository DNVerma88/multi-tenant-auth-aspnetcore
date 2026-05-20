using Microsoft.AspNetCore.Builder;
using MultiTenantAuth.AspNetCore.Middleware;

namespace MultiTenantAuth.AspNetCore.Extensions;

/// <summary>
/// Extension methods for adding <see cref="MultiTenantAuthMiddleware"/> to the pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the <see cref="MultiTenantAuthMiddleware"/> to the request pipeline.
    /// <para>
    /// Call this after <c>UseAuthentication()</c> and before <c>UseAuthorization()</c>:
    /// </para>
    /// <code>
    /// app.UseRouting();
    /// app.UseAuthentication();
    /// app.UseMultiTenantAuth();
    /// app.UseAuthorization();
    /// app.MapControllers();
    /// </code>
    /// </summary>
    public static IApplicationBuilder UseMultiTenantAuth(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<MultiTenantAuthMiddleware>();
    }
}
