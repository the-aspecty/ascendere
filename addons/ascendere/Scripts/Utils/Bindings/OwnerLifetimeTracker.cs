using System;
using System.Collections.Generic;
using Godot;

namespace Ascendere.Utils.Bindings;

/// <summary>
/// Tracks Godot Node owners and hooks their <c>TreeExiting</c> signal to run a
/// cleanup action exactly once when the Node leaves the scene tree.
///
/// <para>
/// This is the canonical replacement for the recurring inline pattern:
/// <code>owner.TreeExiting += () => RemoveOwner(owner);</code>
/// which creates untracked lambda closures that cannot be disconnected.
/// </para>
///
/// <para>
/// Calling <see cref="Track"/> for the same node multiple times is safe — the cleanup
/// actions are combined. Only one <c>TreeExiting</c> subscription is ever made per node.
/// </para>
///
/// <para><b>Usage</b>:</para>
/// <code>
/// private readonly OwnerLifetimeTracker _tracker = new();
///
/// // When registering a binding:
/// _tracker.Track(owner, () => _list.RemoveAllByOwner(owner));
///
/// // When the owner explicitly cleans up (e.g. _ExitTree):
/// _tracker.Forget(owner);   // prevents the TreeExiting callback from double-running
/// </code>
/// </summary>
public sealed class OwnerLifetimeTracker
{
    // Stores the combined cleanup action per node.
    private readonly Dictionary<Node, Action> _cleanupActions = new();

    // Stores the actual delegate connected to TreeExiting so it can be disconnected.
    private readonly Dictionary<Node, Action> _connectedHandlers = new();

    // ─── API ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures <paramref name="onExit"/> runs when <paramref name="owner"/> leaves the
    /// scene tree. Multiple calls for the same node accumulate actions — only one
    /// <c>TreeExiting</c> subscription is made.
    /// </summary>
    /// <param name="owner">The Node to watch. Null or already-invalid nodes are ignored.</param>
    /// <param name="onExit">The cleanup action to run.</param>
    public void Track(Node owner, Action onExit)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner))
            return;

        if (_cleanupActions.TryGetValue(owner, out var existing))
        {
            // Append to the existing combined action — no new signal connection needed.
            _cleanupActions[owner] = existing + onExit;
        }
        else
        {
            _cleanupActions[owner] = onExit;

            // Connect exactly once, storing the handler so we can disconnect later.
            Action handler = () => RunAndForget(owner);
            _connectedHandlers[owner] = handler;
            owner.TreeExiting += handler;
        }
    }

    /// <summary>
    /// Removes tracking for <paramref name="owner"/> and disconnects the
    /// <c>TreeExiting</c> signal subscription.
    /// Call from explicit cleanup paths (e.g. <c>_ExitTree</c>) to prevent the
    /// auto-cleanup from running a second time when the Node eventually exits tree.
    /// </summary>
    /// <param name="owner">The tracked Node. Null is silently ignored.</param>
    public void Forget(Node owner)
    {
        if (owner == null)
            return;

        _cleanupActions.Remove(owner);

        if (_connectedHandlers.TryGetValue(owner, out var handler))
        {
            if (GodotObject.IsInstanceValid(owner))
                owner.TreeExiting -= handler;

            _connectedHandlers.Remove(owner);
        }
    }

    /// <summary>
    /// Removes all tracked nodes and disconnects all <c>TreeExiting</c> subscriptions.
    /// </summary>
    public void ForgetAll()
    {
        foreach (var kv in _connectedHandlers)
        {
            if (GodotObject.IsInstanceValid(kv.Key))
                kv.Key.TreeExiting -= kv.Value;
        }

        _cleanupActions.Clear();
        _connectedHandlers.Clear();
    }

    /// <summary>Number of currently tracked node owners.</summary>
    public int TrackedCount => _cleanupActions.Count;

    // ─── Internal ─────────────────────────────────────────────────────────────

    private void RunAndForget(Node owner)
    {
        // Remove lookup entries first so the action cannot re-register for the same owner.
        _connectedHandlers.Remove(owner);

        if (_cleanupActions.TryGetValue(owner, out var action))
        {
            _cleanupActions.Remove(owner);
            action?.Invoke();
        }
    }
}
