using Ascendere.Log;
using Godot;

// Example: Runtime Override Demo
[Log(true)]
public partial class RuntimeOverrideDemo : Node
{
    public override void _Ready()
    {
        // Initially, logging is enabled via [Log(true)] attribute
        this.LogInfo("1. Logging is enabled by attribute");

        // Disable logging at runtime
        this.SetLoggingOverride(false);
        this.LogInfo("2. This won't be logged");
        this.LogDebug("3. This won't be logged either");

        // Re-enable logging at runtime
        this.SetLoggingOverride(true);
        this.LogInfo("4. Logging re-enabled!");

        // Remove the override (revert to attribute behavior)
        this.RemoveLoggingOverride();
        this.LogInfo("5. Back to attribute-based logging");

        // Type-based override example
        DemoTypeLevelOverride();
    }

    private void DemoTypeLevelOverride()
    {
        // Create multiple instances
        var obj1 = new TestClass();
        var obj2 = new TestClass();

        obj1.LogInfo("6. TestClass logging enabled by default");
        obj2.LogInfo("7. TestClass logging enabled by default");

        // Disable logging for ALL TestClass instances
        Ascendere.Log.Logger.Instance.SetLoggingOverride(typeof(TestClass), false);

        obj1.LogInfo("8. This won't log (type override)");
        obj2.LogInfo("9. This won't log (type override)");

        // Remove the type-level override
        Ascendere.Log.Logger.Instance.RemoveLoggingOverride(typeof(TestClass));

        obj1.LogInfo("10. Logging restored for all TestClass instances");
        obj2.LogInfo("11. Logging restored for all TestClass instances");
    }
}

public partial class TestClass : RefCounted
{
    // No [Log] attribute, defaults to enabled
}
