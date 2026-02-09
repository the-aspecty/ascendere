using Ascendere;
using Ascendere.Events;
using Ascendere.Modular;
using Godot;

/// <summary>
/// Main singleton that initializes and manages the MetaFramework context for the application.
/// </summary>
public partial class AscendereManager : Node
{
    // Singleton instance
    private static AscendereManager _instance;
    public static AscendereManager Instance => _instance;

    public override void _EnterTree()
    {
        if (_instance != null)
        {
            QueueFree();
            return;
        }
        _instance = this;

        //var metaContext = new MetaContext();
        //metaContext.Name = "MetaContext";
        //AddChild(metaContext);
        SetupAutoProcessor();
        SetupModules();
        SetupEvents();
        SetupServices();

        EventBus.Instance.Subscribe(this);

        EventBus.Instance.Publish(new StartedEvent());
    }

    public override void _ExitTree()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void SetupServices()
    {
        const string serviceLocatorDesc = "Service Locator for Ascendere MetaFramework";
        ServiceLocator serviceLocator = new ServiceLocator() { Name = "ServiceLocator" };
        serviceLocator.SetMeta("description", serviceLocatorDesc);
        serviceLocator.EditorDescription = serviceLocatorDesc;
        AddChild(serviceLocator);
    }

    private void SetupEvents()
    {
        const string eventBusDesc = "Event Bus for Event driven communication";
        EventBus eventBus = EventBus.Instance;
        eventBus.Name = "EventBus";
        eventBus.SetMeta("description", eventBusDesc);
        eventBus.EditorDescription = eventBusDesc;
        AddChild(eventBus);
    }

    private void SetupModules()
    {
        const string moduleManagerDesc = "Module Manager for Ascendere MetaFramework";
        ModuleManager moduleManager = new ModuleManager() { Name = "ModuleManager" };
        moduleManager.SetMeta("description", moduleManagerDesc);
        moduleManager.EditorDescription = moduleManagerDesc;
        AddChild(moduleManager);
    }

    private void SetupAutoProcessor()
    {
        //SceneAutoProcessor
        const string sceneAutoProcessorDesc =
            "Scene Auto Processor for automatic scene/node processing";
        SceneAutoProcessor sceneAutoProcessor = new SceneAutoProcessor()
        {
            Name = "SceneAutoProcessor",
        };
        sceneAutoProcessor.SetMeta("description", sceneAutoProcessorDesc);
        sceneAutoProcessor.EditorDescription = sceneAutoProcessorDesc;
        AddChild(sceneAutoProcessor);
    }

    [EventHandler(typeof(StartedEvent), Priority = 100)]
    private void Started(StartedEvent evt)
    {
        GD.Print($"[AscendereManager] Started Event received");
    }
}

public struct StartedEvent : IEvent
{
    public Godot.Collections.Dictionary ToGodotDict()
    {
        return new Godot.Collections.Dictionary { };
    }

    public void FromGodotDict(Godot.Collections.Dictionary dict) { }
}
