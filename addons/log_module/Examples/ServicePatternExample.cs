using Ascendere.Log;
using Godot;

/// <summary>
/// Example showing how to register LogService with the Ascendere ServiceLocator
/// </summary>
public partial class ServiceLocatorExample : Node
{
    public override void _Ready()
    {
        // Register LogService with the ServiceLocator
        ServiceLocator.Register<ILogService>(new LogService());

        // Get the log service from ServiceLocator
        var logService = ServiceLocator.Get<ILogService>();

        // Configure the logger through the service
        logService.EnableTimestamps = true;
        logService.EnableTypeNames = true;
        logService.EnableLogLevel = true;
        logService.GlobalMinimumLevel = LogLevel.Debug;

        // Use the service for logging
        logService.Info(this, "ServiceLocatorExample initialized");
        logService.Debug(this, "Debug information");
        logService.Warning(this, "Warning message");
        logService.Error(this, "Error message");

        // Demonstrate configuration changes
        DemoConfigurationChanges(logService);
    }

    private void DemoConfigurationChanges(ILogService logService)
    {
        logService.Info(this, "Standard format with all options enabled");

        // Disable log level display
        logService.EnableLogLevel = false;
        logService.Info(this, "Log level hidden");

        // Disable timestamps
        logService.EnableTimestamps = false;
        logService.Info(this, "No timestamp or level");

        // Disable type names
        logService.EnableTypeNames = false;
        logService.Info(this, "Only the message");

        // Re-enable everything
        logService.EnableLogLevel = true;
        logService.EnableTimestamps = true;
        logService.EnableTypeNames = true;
        logService.Info(this, "Back to full format");
    }
}

/// <summary>
/// Example of a game system that depends on ILogService
/// </summary>
public partial class GameSystem : Node
{
    private readonly ILogService _log;

    // Constructor injection pattern
    public GameSystem(ILogService logService)
    {
        _log = logService;
    }

    // Default constructor for Godot - gets service from ServiceLocator
    public GameSystem()
        : this(ServiceLocator.Get<ILogService>()) { }

    public override void _Ready()
    {
        _log.Info(this, "GameSystem initialized");
    }

    public void ProcessGameLogic()
    {
        if (_log.IsLoggingEnabled(this))
        {
            _log.Debug(this, "Processing game logic...");
        }

        // Game logic here
    }

    public void OnError(string errorMessage)
    {
        _log.Error(this, $"Critical error: {errorMessage}");
    }
}

/// <summary>
/// Example using automatic service registration with [Service] attribute
/// </summary>
[Service(typeof(ILogService), ServiceLifetime.Singleton)]
public partial class AutoRegisteredLogService : LogService
{
    // This will be automatically registered with the ServiceLocator
    // when the ServiceLocator initializes
}

/// <summary>
/// Example using the service locator pattern directly
/// </summary>
[Log(true)]
public partial class PlayerSystem : Node
{
    private ILogService _log;

    public override void _Ready()
    {
        // Get service from ServiceLocator
        _log = ServiceLocator.Get<ILogService>();

        _log.Info(this, "PlayerSystem initialized using ServiceLocator");

        // Configure through service
        _log.EnableLogLevel = false;
        _log.Info(this, "Clean message without log level");
    }

    public void TakeDamage(int amount)
    {
        _log?.Warning(this, $"Player took {amount} damage");
    }
}
