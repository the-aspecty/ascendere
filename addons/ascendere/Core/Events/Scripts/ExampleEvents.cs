// Example event structs
using Ascendere.Events;
using Godot;

public struct PlayerHealthChangedEvent : IEvent
{
    public int OldHealth;
    public int NewHealth;
    public string PlayerName;

    public Godot.Collections.Dictionary ToGodotDict()
    {
        return new Godot.Collections.Dictionary
        {
            { "OldHealth", OldHealth },
            { "NewHealth", NewHealth },
            { "PlayerName", PlayerName },
        };
    }

    public void FromGodotDict(Godot.Collections.Dictionary dict)
    {
        OldHealth = dict["OldHealth"].AsInt32();
        NewHealth = dict["NewHealth"].AsInt32();
        PlayerName = dict["PlayerName"].AsString();
    }
}

public struct PlayerDamageEvent : ICancellableEvent
{
    public string PlayerName;
    public int Damage;
    public string DamageSource;
    public bool IsCancelled { get; set; }

    public Godot.Collections.Dictionary ToGodotDict()
    {
        return new Godot.Collections.Dictionary
        {
            { "PlayerName", PlayerName },
            { "Damage", Damage },
            { "DamageSource", DamageSource },
            { "IsCancelled", IsCancelled },
        };
    }

    public void FromGodotDict(Godot.Collections.Dictionary dict)
    {
        PlayerName = dict["PlayerName"].AsString();
        Damage = dict["Damage"].AsInt32();
        DamageSource = dict["DamageSource"].AsString();
        IsCancelled = dict["IsCancelled"].AsBool();
    }
}

public struct ItemCollectedEvent : IEvent
{
    public string ItemName;
    public int Quantity;
    public Vector2 Position;

    public Godot.Collections.Dictionary ToGodotDict()
    {
        return new Godot.Collections.Dictionary
        {
            { "ItemName", ItemName },
            { "Quantity", Quantity },
            { "Position", Position },
        };
    }

    public void FromGodotDict(Godot.Collections.Dictionary dict)
    {
        ItemName = dict["ItemName"].AsString();
        Quantity = dict["Quantity"].AsInt32();
        Position = dict["Position"].AsVector2();
    }
}

// Example: Player with priority handlers and cancellable events
public partial class Player : Node2D
{
    private int _health = 100;
    private bool _isInvulnerable = false;

    public override void _Ready()
    {
        EventBus.Instance.Subscribe(this);

        // Runtime subscription example
        EventBus.Instance.SubscribeToEvent<ItemCollectedEvent>(OnItemCollectedRuntime, priority: 5);
    }

    public override void _ExitTree()
    {
        EventBus.Instance.Unsubscribe(this);
    }

    public void TakeDamage(int damage, string source)
    {
        // Use cancellable event
        var damageEvt = EventBus.Instance.PublishCancellable(
            new PlayerDamageEvent
            {
                PlayerName = "Player1",
                Damage = damage,
                DamageSource = source,
                IsCancelled = false,
            }
        );

        if (!damageEvt.IsCancelled)
        {
            var oldHealth = _health;
            _health -= damageEvt.Damage;

            EventBus.Instance.Publish(
                new PlayerHealthChangedEvent
                {
                    OldHealth = oldHealth,
                    NewHealth = _health,
                    PlayerName = "Player1",
                }
            );
        }
    }

    // High priority handler - runs first, can cancel damage
    [EventHandler(typeof(PlayerDamageEvent), Priority = 100)]
    private PlayerDamageEvent OnDamageReceived(PlayerDamageEvent evt)
    {
        if (_isInvulnerable)
        {
            GD.Print("Player is invulnerable - damage cancelled");
            evt.IsCancelled = true;
        }
        return evt;
    }

    [EventHandler(typeof(ItemCollectedEvent))]
    private void OnItemCollected(ItemCollectedEvent evt)
    {
        GD.Print($"[Attribute] Item collected: {evt.ItemName}");
    }

    private void OnItemCollectedRuntime(ItemCollectedEvent evt)
    {
        GD.Print($"[Runtime] Item collected: {evt.ItemName}");
    }
}

// Example: Shield system that cancels damage
public partial class ShieldSystem : Node
{
    private int _shieldHealth = 50;

    public override void _Ready()
    {
        EventBus.Instance.Subscribe(this);
    }

    public override void _ExitTree()
    {
        EventBus.Instance.Unsubscribe(this);
    }

    // Higher priority than player - runs before player takes damage
    [EventHandler(typeof(PlayerDamageEvent), Priority = 200)]
    private PlayerDamageEvent OnPlayerDamage(PlayerDamageEvent evt)
    {
        if (_shieldHealth > 0)
        {
            var absorb = Mathf.Min(evt.Damage, _shieldHealth);
            _shieldHealth -= absorb;
            evt.Damage -= absorb;

            GD.Print($"Shield absorbed {absorb} damage. Remaining: {evt.Damage}");

            if (evt.Damage <= 0)
            {
                evt.IsCancelled = true;
            }
        }
        return evt;
    }
}

// Example: Debug UI showing event statistics
public partial class DebugEventUI : Control
{
    private RichTextLabel _statsLabel;

    public override void _Ready()
    {
        _statsLabel = GetNode<RichTextLabel>("StatsLabel");
    }

    public override void _Process(double delta)
    {
        UpdateStats();
    }

    private void UpdateStats()
    {
        var stats = EventBus.Instance.GetEventStats();
        var text = "[b]Event Statistics:[/b]\n\n";

        foreach (var kvp in stats)
        {
            text += $"[color=yellow]{kvp.Key}[/color]\n";
            text += $"  Calls: {kvp.Value.TotalCalls}\n";
            text += $"  Avg Time: {kvp.Value.AverageExecutionTimeMicros}μs\n";
            text += $"  Subscribers: {kvp.Value.LastSubscriberCount}\n\n";
        }

        _statsLabel.Text = text;
    }

    [SignalHandler("pressed", "ClearHistoryButton")]
    private void OnClearHistory()
    {
        EventBus.Instance.ClearHistory();
        EventBus.Instance.ClearStats();
    }
}
