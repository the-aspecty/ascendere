using System;
using Godot;

namespace Ascendere.Utils.Bindings;

/// <summary>
/// Shared storage unit for a single registered reactive binding.
/// Used by every module that implements the fluent binding pattern.
///
/// <para>Fields are mutable so the dispatch loop can set <see cref="Alive"/> to <c>false</c>
/// as a soft-delete without touching the list mid-iteration.</para>
///
/// <typeparam name="TContext">
/// The event / context value the binding reacts to.
/// Example: <c>FlagChangedEvent</c>, <c>InventoryEvent</c>, <c>InputCommand</c>.
/// </typeparam>
/// </summary>
public sealed class BindingRecord<TContext>
{
    // ─── Lifecycle ────────────────────────────────────────────────────────────

    /// <summary>
    /// Set to <c>false</c> to mark this record for removal on the next dispatch cycle
    /// without mutating the list mid-iteration. The dispatch loop removes it when it
    /// encounters it.
    /// </summary>
    public bool Alive = true;

    /// <summary>
    /// When <c>true</c> the binding removes itself after the first successful invocation.
    /// </summary>
    public bool Once;

    /// <summary>
    /// Optional Node owner. When set, the binding is automatically pruned once
    /// <see cref="Godot.GodotObject.IsInstanceValid"/> returns <c>false</c> for this node.
    /// For proactive cleanup (before the prune cycle) use
    /// <see cref="OwnerLifetimeTracker"/> to hook <c>TreeExiting</c>.
    /// </summary>
    public Node Owner;

    // ─── Routing ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Optional filter predicate. When <c>null</c> the binding matches every context.
    /// The predicate receives the dispatch context and returns <c>true</c> to allow
    /// invocation.
    ///
    /// <para>
    /// <b>Tip</b>: capture only what you need (string key, enum kind, etc.) rather than
    /// the whole builder object to avoid keeping large objects alive via closure.
    /// </para>
    /// </summary>
    public Func<TContext, bool> Filter;

    /// <summary>
    /// The user callback. Always invoked on the main thread (same thread as dispatch).
    /// </summary>
    public Action<TContext> Callback;

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when this record should be discarded by the dispatch loop:
    /// either explicitly soft-deleted (<see cref="Alive"/> is <c>false</c>) or its
    /// owner Node has been freed from the scene.
    /// </summary>
    public bool ShouldPrune() => !Alive || (Owner != null && !GodotObject.IsInstanceValid(Owner));
}
