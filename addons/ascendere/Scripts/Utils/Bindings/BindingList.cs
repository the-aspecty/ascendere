using System;
using System.Collections.Generic;
using Godot;

namespace Ascendere.Utils.Bindings;

/// <summary>
/// Allocation-aware list of <see cref="BindingRecord{TContext}"/> with a safe,
/// backwards-iterate dispatch loop.
///
/// <para>Features provided out of the box:</para>
/// <list type="bullet">
///   <item>Dead-record pruning (both <see cref="BindingRecord{T}.Alive"/> = false and freed Node owners).</item>
///   <item><see cref="BindingRecord{T}.Once"/> auto-removal after first invocation.</item>
///   <item>Re-entrancy guard: a callback that mutates the same data source is blocked
///         with a logged error instead of silently overflowing the stack.</item>
///   <item>Per-callback exception isolation: one failing callback cannot interrupt others.</item>
/// </list>
///
/// <para>
/// <b>Thread safety</b>: not thread-safe. All calls must be on the same thread
/// (the Godot main thread for usual game logic).
/// </para>
///
/// <typeparam name="TContext">The event / context type. Must be a value or reference type
/// that carries all data needed by filter predicates and callbacks.</typeparam>
/// </summary>
public sealed class BindingList<TContext>
{
    private readonly List<BindingRecord<TContext>> _items = new();

    // Re-entrancy depth counter. >0 means we are already inside Dispatch.
    private int _dispatchDepth;

    /// <summary>Current number of registered records (including any stale ones not yet pruned).</summary>
    public int Count => _items.Count;

    // ─── Mutation ─────────────────────────────────────────────────────────────

    /// <summary>Appends a new binding record.</summary>
    public void Add(BindingRecord<TContext> record)
    {
        if (record == null)
            throw new ArgumentNullException(nameof(record));
        _items.Add(record);
    }

    /// <summary>
    /// Immediately removes all records whose <see cref="BindingRecord{T}.Owner"/> matches
    /// <paramref name="owner"/>. Intended to be called from <see cref="OwnerLifetimeTracker"/>
    /// cleanup actions or explicit <c>_ExitTree</c> paths.
    /// </summary>
    public void RemoveAllByOwner(Node owner)
    {
        if (owner == null)
            return;

        for (int i = _items.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(_items[i].Owner, owner))
                _items.RemoveAt(i);
        }
    }

    /// <summary>
    /// Soft-deletes all records whose <see cref="BindingRecord{T}.Owner"/> matches
    /// <paramref name="owner"/> without mutating the list. The records will be pruned
    /// on the next <see cref="Dispatch"/> call.
    /// Safe to call during a dispatch iteration.
    /// </summary>
    public void SoftRemoveAllByOwner(Node owner)
    {
        if (owner == null)
            return;

        foreach (var b in _items)
        {
            if (ReferenceEquals(b.Owner, owner))
                b.Alive = false;
        }
    }

    /// <summary>Removes all registered records.</summary>
    public void Clear()
    {
        _items.Clear();
        _dispatchDepth = 0;
    }

    // ─── Dispatch ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Dispatches <paramref name="context"/> to all matching live bindings.
    ///
    /// <para>Iteration is backwards so <c>RemoveAt(i)</c> is O(1) and does not skip items.</para>
    /// <para>Re-entrant calls (a callback that triggers another dispatch) are blocked
    ///      and logged. To intentionally defer a mutation, schedule it with
    ///      <c>Callable.From(...).CallDeferred()</c>.</para>
    /// </summary>
    public void Dispatch(TContext context)
    {
        if (_dispatchDepth > 0)
        {
            GD.PrintErr(
                $"[BindingList<{typeof(TContext).Name}>] Re-entrant dispatch detected and skipped. "
                    + "Avoid mutating the triggering data source from inside a binding callback. "
                    + "Use CallDeferred() to schedule deferred mutations."
            );
            return;
        }

        _dispatchDepth++;
        try
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var b = _items[i];

                if (b.ShouldPrune())
                {
                    _items.RemoveAt(i);
                    continue;
                }

                if (b.Filter != null && !b.Filter(context))
                    continue;

                try
                {
                    b.Callback(context);
                }
                catch (Exception ex)
                {
                    GD.PrintErr(
                        $"[BindingList<{typeof(TContext).Name}>] Binding callback threw an exception: {ex.Message}"
                    );
                }

                if (b.Once)
                    _items.RemoveAt(i);
            }
        }
        finally
        {
            _dispatchDepth--;
        }
    }
}
