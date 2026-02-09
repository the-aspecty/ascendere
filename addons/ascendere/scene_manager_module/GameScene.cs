using System;
using Ascendere.SceneManagement;
using Godot;

#nullable enable

/// <summary>
/// Base class for game scenes with elegant next-scene navigation.
/// Single responsibility: scene lifecycle management with flow control.
/// </summary>
public abstract partial class GameScene : Node
{
    [Inject]
    protected ISceneManager SceneManager { get; set; } = null!;

    // Signals
    [Signal]
    public delegate void SceneLoadedEventHandler(PackedScene scene, Node instance);

    [Signal]
    public delegate void SceneUnloadedEventHandler(PackedScene scene);

    [Signal]
    public delegate void ReadyToProceedEventHandler();

    /// <summary>
    /// Currently loaded PackedScene (nullable).
    /// </summary>
    public PackedScene? CurrentPackedScene { get; private set; }

    /// <summary>
    /// Root node instance of the currently loaded scene (nullable).
    /// </summary>
    public Node? CurrentInstance { get; private set; }

    /// <summary>
    /// True when a scene is currently loaded and its instance is inside the scene tree.
    /// </summary>
    public bool IsSceneLoaded => IsInstanceValid(CurrentInstance);

    public override void _Ready()
    {
        ServiceLocator.InjectMembers(this);
        base._Ready();
    }

    /// <summary>
    /// Override this to define the next scene in the flow.
    /// Return null if this is the final scene.
    /// </summary>
    protected abstract Type? GetNextSceneType();

    /// <summary>
    /// Override to validate whether the scene can proceed to the next.
    /// Default implementation returns true.
    /// </summary>
    protected virtual bool CanProceedToNext() => true;

    /// <summary>
    /// Proceeds to the next scene if validation passes.
    /// Returns true if transition started successfully.
    /// </summary>
    public bool ProceedToNext()
    {
        if (!CanProceedToNext())
        {
            GD.PushWarning($"[{GetType().Name}] Cannot proceed to next - validation failed.");
            return false;
        }

        var nextType = GetNextSceneType();
        if (nextType == null)
        {
            GD.Print($"[{GetType().Name}] No next scene defined - this is the final scene.");
            return false;
        }

        var scenePath = GetScenePathForType(nextType);
        if (string.IsNullOrEmpty(scenePath))
        {
            GD.PrintErr($"[{GetType().Name}] Failed to find scene path for type: {nextType.Name}");
            return false;
        }

        CallDeferred(nameof(TransitionToNextScene), scenePath);
        return true;
    }

    private async void TransitionToNextScene(string scenePath)
    {
        if (SceneManager != null)
        {
            await SceneManager.ChangeSceneAsync(scenePath);
        }
        else
        {
            GD.PrintErr($"[{GetType().Name}] SceneManager not injected - cannot transition.");
        }
    }

    /// <summary>
    /// Gets the scene file path for a GameScene type.
    /// Override to customize scene path resolution.
    /// </summary>
    protected virtual string? GetScenePathForType(Type sceneType)
    {
        // Default convention: res://scenes/{TypeName}.tscn
        var sceneName = sceneType.Name;
        return $"res://scenes/{sceneName}.tscn";
    }

    /// <summary>
    /// Loads a PackedScene from the given resource path and adds its instance as a child of this node.
    /// If a scene is already loaded it will be unloaded first.
    /// </summary>
    /// <param name="path">Resource path to a .tscn/.scn file (e.g. "res://scenes/MyScene.tscn").</param>
    public void LoadScene(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            GD.PushWarning($"[{GetType().Name}] LoadScene called with empty path.");
            return;
        }

        var packed = ResourceLoader.Load<PackedScene>(path);
        if (packed == null)
        {
            GD.PrintErr($"[{GetType().Name}] Failed to load PackedScene at path '{path}'.");
            return;
        }

        // Unload previous
        UnloadCurrentScene();

        // Instantiate and add
        var instance = packed.Instantiate();
        if (!IsInstanceValid(instance))
        {
            GD.PrintErr($"[{GetType().Name}] Failed to instantiate scene at '{path}'.");
            return;
        }

        AddChild(instance);
        CurrentPackedScene = packed;
        CurrentInstance = instance;

        EmitSignal(SignalName.SceneLoaded, packed, instance);
        GD.Print($"[{GetType().Name}] Loaded scene '{path}'.");
    }

    /// <summary>
    /// Unloads the currently loaded scene (if any) and emits SceneUnloaded.
    /// </summary>
    public void UnloadCurrentScene()
    {
        if (!IsInstanceValid(CurrentInstance) || CurrentPackedScene == null)
        {
            CurrentInstance = null;
            CurrentPackedScene = null;
            return;
        }

        var packed = CurrentPackedScene;
        var instance = CurrentInstance;

        // Free the instance safely
        try
        {
            instance!.QueueFree();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[{GetType().Name}] Exception while freeing instance: {ex.Message}");
        }

        CurrentInstance = null;
        CurrentPackedScene = null;

        EmitSignal(SignalName.SceneUnloaded, packed);
        GD.Print($"[{GetType().Name}] Unloaded scene.");
    }

    /// <summary>
    /// Convenience method to swap to another scene path. Returns true on success.
    /// </summary>
    /// <param name="path">Resource path to load next.</param>
    public bool SwapToScene(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            GD.PushWarning($"[{GetType().Name}] SwapToScene called with empty path.");
            return false;
        }

        LoadScene(path);
        return IsSceneLoaded;
    }

    public override void _ExitTree()
    {
        // Ensure cleanup on tree exit / plugin disable / assembly reloads
        UnloadCurrentScene();
        base._ExitTree();
    }
}
