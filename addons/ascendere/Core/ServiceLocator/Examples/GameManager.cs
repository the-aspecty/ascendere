using Godot;

public partial class GameManager : Node
{
    public override async void _Ready()
    {
        // Setup middleware pipeline
        ServiceLocator.UseMiddleware(new LoggingMiddleware());
        ServiceLocator.UseMiddleware(new ValidationMiddleware());

        // Custom middleware
        ServiceLocator.UseMiddleware(
            async (context, next) =>
            {
                GD.Print($"[CustomMiddleware] Before: {context.ServiceType.Name}");
                var result = await next();
                GD.Print($"[CustomMiddleware] After: {context.ServiceType.Name}");
                return result;
            }
        );

        // Initialize all services
        await ServiceLocator.InitializeServicesAsync();

        // Print registered services
        ServiceLocator.PrintServices();

        // Print dependency graph
        ServiceLocator.PrintDependencyGraph();

        // Export dependency graph
        ServiceLocator.SaveDependencyGraphToFile(
            "res://docs/dependency_graph.md",
            GraphFormat.Mermaid
        );

        // Example: Get named services
        var fileLogger = ServiceLocator.Get<ILogger>("FileLogger");
        var consoleLogger = ServiceLocator.Get<ILogger>("ConsoleLogger");

        fileLogger?.Log("Game started!");
        consoleLogger?.Log("Ready to play!");

        // Get all loggers
        var allLoggers = ServiceLocator.GetAllNamed<ILogger>();
        foreach (var kvp in allLoggers)
        {
            GD.Print($"Logger: {kvp.Key}");
        }
    }
}
