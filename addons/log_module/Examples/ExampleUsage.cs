using Ascendere.Log;
using Godot;

// Example 1: Class with logging enabled (default)
[Log(true)]
public partial class PlayerController : Node
{
    public override void _Ready()
    {
        // These will all be logged
        this.LogInfo("PlayerController initialized");
        this.LogDebug($"Starting position: {Position}");
    }

    public void TakeDamage(int amount)
    {
        this.LogWarning($"Player took {amount} damage!");

        if (Health <= 0)
        {
            this.LogError("Player died!");
        }
    }

    private int Health { get; set; } = 100;
    private Vector3 Position { get; set; } = Vector3.Zero;
}

// Example 2: Class with logging disabled
[Log(false)]
public partial class PerformanceCriticalSystem : Node
{
    public override void _Process(double delta)
    {
        // This won't log anything (good for performance)
        this.LogDebug("Processing frame...");

        // Do performance-critical work
    }
}

// Example 3: Class with conditional logging
[Log(true)]
public partial class EnemyAI : Node
{
    public void UpdateAI()
    {
        // Check if logging is enabled before expensive operations
        if (this.IsLoggingEnabled())
        {
            var stateReport = GenerateDetailedStateReport();
            this.LogDebug(stateReport);
        }
    }

    private string GenerateDetailedStateReport()
    {
        // Expensive string building operation
        return $"Enemy state: Position={Position}, Target={Target}, Health={Health}";
    }

    private Vector3 Position { get; set; }
    private Node Target { get; set; }
    private int Health { get; set; } = 50;
}

// Example 4: Using direct Logger API
public partial class ExampleUsage : Node
{
    public override void _Ready()
    {
        var uiController = new UIController();
        AddChild(uiController);

        // Configure logger


        // DirecLogModule.t API usage
        Ascendere.Log.Logger.Instance.Info(this, "Game started");
        Ascendere.Log.Logger.Instance.Debug(this, "Loading configuration...");
    }
}

// Example 5: Class with no attribute (defaults to enabled)
[Log(false)]
public partial class UIController : Control
{
    public override void _Ready()
    {
        // Logging is enabled by default
        this.LogInfo("UI initialized");
    }
}
