using MultiTenantAuth.AspNetCore.Abstractions;
using MultiTenantAuth.AspNetCore.Models;

namespace MultiTenantAuth.AspNetCore.Internal;

/// <summary>
/// Default <see cref="ITenantContextAccessor"/> implementation backed by an
/// <see cref="AsyncLocal{T}"/> so that each async execution context (request)
/// gets an isolated, independently mutable slot.
/// </summary>
internal sealed class TenantContextAccessor : ITenantContextAccessor
{
    // Each AsyncLocal holder stores a wrapper so that setting Current = null
    // clears only the local slot without affecting unrelated execution contexts.
    private static readonly AsyncLocal<TenantContextHolder?> _holder = new();

    /// <inheritdoc />
    public TenantContext? Current
    {
        get => _holder.Value?.Context;
        set
        {
            if (value is not null)
            {
                // Create a new holder so child tasks see the value but setting it
                // to null in a child does not propagate back to the parent.
                _holder.Value = new TenantContextHolder { Context = value };
            }
            else
            {
                // Null the inner context first (visible to child tasks that may have
                // captured the holder reference), then drop the holder itself so
                // the GC can reclaim it.
                var holder = _holder.Value;
                if (holder is not null)
                    holder.Context = null;

                _holder.Value = null;
            }
        }
    }

    private sealed class TenantContextHolder
    {
        public TenantContext? Context;
    }
}
