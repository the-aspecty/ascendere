using Godot;

namespace Ascendere.Utils.Bindings;

/// <summary>
/// CRTP base class for fluent binding builder chains.
/// Provides the <see cref="Once"/> and <see cref="OwnedBy"/> modifiers that are
/// identical across every module's binding API, eliminating duplicated code.
///
/// <para>
/// Concrete builders inherit this and add module-specific filter methods before
/// a terminal <c>Do()</c> that commits the binding to its registry.
/// </para>
///
/// <example>
/// <code>
/// public sealed class MyBinding : FluentBuilderBase&lt;MyBinding&gt;
/// {
///     private string _filter;
///
///     internal MyBinding(string filter) => _filter = filter;
///
///     public MyBinding WithFilter(string f) { _filter = f; return this; }
///
///     // Terminal — commits to registry using inherited IsOnce / Owner.
///     public MyBinding Do(Action&lt;MyContext&gt; callback)
///     {
///         var filter = _filter; // capture only the needed value, not 'this'
///         MyRegistry.Register(Owner, ctx => ctx.Key == filter, callback, IsOnce);
///         return this;
///     }
/// }
/// </code>
/// </example>
///
/// <typeparam name="TSelf">
/// The concrete builder type. Must match the class that extends this base
/// (the CRTP / Curiously Recurring Template Pattern).
/// </typeparam>
/// </summary>
public abstract class FluentBuilderBase<TSelf>
    where TSelf : FluentBuilderBase<TSelf>
{
    /// <summary>
    /// Whether the binding should auto-remove itself after its first invocation.
    /// Set by <see cref="Once"/>. Consumed in the terminal <c>Do()</c> call.
    /// </summary>
    protected bool IsOnce;

    /// <summary>
    /// The Node owner that scopes this binding's lifetime.
    /// Set by <see cref="OwnedBy"/>. Consumed in the terminal <c>Do()</c> call.
    /// </summary>
    protected Node Owner;

    // ─── Shared modifiers ─────────────────────────────────────────────────────

    /// <summary>
    /// Makes this binding auto-remove itself after the first successful invocation.
    /// Useful for one-shot events: tutorial steps, achievement triggers, first-pickup
    /// callbacks, etc.
    /// </summary>
    public TSelf Once()
    {
        IsOnce = true;
        return (TSelf)this;
    }

    /// <summary>
    /// Associates this binding with a Godot Node owner.
    /// The binding is automatically removed when the owner leaves the scene tree,
    /// preventing callbacks from firing on freed or logically-dead nodes.
    ///
    /// <para>
    /// The registry's <see cref="OwnerLifetimeTracker"/> handles the <c>TreeExiting</c>
    /// hookup — no manual signal management is needed.
    /// </para>
    /// </summary>
    public TSelf OwnedBy(Node owner)
    {
        Owner = owner;
        return (TSelf)this;
    }
}
