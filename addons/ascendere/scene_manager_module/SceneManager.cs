using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

namespace Ascendere.SceneManagement
{
    /// <summary>
    /// Elegant scene manager for Godot with transitions, loading, and history management.
    ///
    /// <para><b>Setup Instructions:</b></para>
    /// <list type="number">
    /// <item>Save this file to your project (e.g., addons/scene_manager/SceneManager.cs)</item>
    /// <item>Open Project Settings → Autoload</item>
    /// <item>Add new autoload:
    ///   - Name: SceneManager
    ///   - Path: res://addons/scene_manager/SceneManager.cs
    ///   - Enable: ✓</item>
    /// <item>Access via SceneManager.Instance from anywhere in your code</item>
    /// </list>
    ///
    /// <para><b>Best Practices:</b></para>
    /// <list type="bullet">
    /// <item>Always use await with async methods to ensure transitions complete</item>
    /// <item>Preload scenes during loading screens or menu states for instant transitions</item>
    /// <item>Use history sparingly - disable for linear games to save memory</item>
    /// <item>Call UnloadScene() after using preloaded scenes to manage memory</item>
    /// <item>Use custom loading screens for large scenes (>50MB)</item>
    /// <item>Connect to signals for UI updates (loading bars, scene names)</item>
    /// <item>Disable transitions during rapid scene changes (death/respawn loops)</item>
    /// </list>
    ///
    /// <para><b>Common Patterns:</b></para>
    /// <code>
    /// // Main menu to game
    /// await SceneManager.Instance.ChangeSceneAsync("res://scenes/game.tscn");
    ///
    /// // Fast respawn without transition
    /// await SceneManager.Instance.ReloadSceneAsync(useTransition: false);
    ///
    /// // Level progression with preloading
    /// SceneManager.Instance.PreloadScene("res://levels/level_2.tscn");
    /// // ... later ...
    /// await SceneManager.Instance.ChangeSceneAsync("res://levels/level_2.tscn");
    ///
    /// // Back button functionality
    /// if (SceneManager.Instance.GetHistoryCount() > 0)
    ///     await SceneManager.Instance.GoBackAsync();
    /// </code>
    /// </summary>
    [Service(typeof(ISceneManager))]
    public partial class SceneManager : Node, ISceneManager
    {
        private static SceneManager _instance;
        public static SceneManager Instance => _instance;

        [Signal]
        public delegate void SceneChangedEventHandler(string scenePath);

        [Signal]
        public delegate void SceneLoadingEventHandler(string scenePath, float progress);

        // Backing fields for interface events (allow consumers to subscribe via ISceneManager)
        private event Action<string> _onSceneChangedHandlers;
        private event Action<string, float> _onSceneLoadingHandlers;

        // Use explicit interface event implementation to avoid name collisions with generated signal members
        event Action<string> ISceneManager.OnSceneChanged
        {
            add => _onSceneChangedHandlers += value;
            remove => _onSceneChangedHandlers -= value;
        }

        event Action<string, float> ISceneManager.OnSceneLoading
        {
            add => _onSceneLoadingHandlers += value;
            remove => _onSceneLoadingHandlers -= value;
        }

        private Node _currentScene;
        private readonly Stack<string> _sceneHistory = new();
        private readonly Dictionary<string, PackedScene> _preloadedScenes = new();
        private CanvasLayer _transitionCanvasLayer;
        private ColorRect _transitionColorRect;
        private bool _isTransitioning;
        private readonly List<Type> _registeredGameScenes = new();

        /// <summary>Default duration for fade transitions in seconds</summary>
        [Export]
        public float DefaultTransitionDuration { get; set; } = 0.3f;

        /// <summary>Enable scene history tracking for GoBack functionality</summary>
        [Export]
        public bool EnableHistory { get; set; } = true;

        /// <summary>Maximum number of scenes to keep in history</summary>
        [Export]
        public int MaxHistorySize { get; set; } = 10;

        public override void _EnterTree()
        {
            if (_instance != null)
            {
                QueueFree();
                return;
            }
            _instance = this;
            SetupTransitionLayer();
            CollectGameScenes();
        }

        public override void _Ready()
        {
            _currentScene = GetTree().CurrentScene;
            ProcessMode = ProcessModeEnum.Always;
        }

        /// <summary>
        /// Collects all GameScene types via reflection for elegant scene flow management.
        /// </summary>
        private void CollectGameScenes()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var gameSceneType = typeof(GameScene);

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(gameSceneType))
                {
                    _registeredGameScenes.Add(type);
                    GD.Print($"[SceneManager] Registered GameScene: {type.Name}");
                }
            }

            GD.Print($"[SceneManager] Total GameScenes registered: {_registeredGameScenes.Count}");
        }

        /// <summary>
        /// Gets all registered GameScene types.
        /// </summary>
        public IReadOnlyList<Type> GetRegisteredGameScenes() => _registeredGameScenes.AsReadOnly();

        private void SetupTransitionLayer()
        { //add a canvas to root with high layer
            _transitionCanvasLayer = new CanvasLayer { Layer = 1000, Name = "SceneTransitionLayer" };
            GetTree().Root.AddChild(_transitionCanvasLayer);

            _transitionColorRect = new ColorRect
            {
                Color = Colors.Black,
                Modulate = new Color(1, 1, 1, 0),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            _transitionColorRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _transitionCanvasLayer.AddChild(_transitionColorRect);
        }

        /// <summary>
        /// Change to a new scene with optional transition.
        /// </summary>
        /// <param name="scenePath">Full resource path to the scene (e.g., "res://scenes/menu.tscn")</param>
        /// <param name="useTransition">Whether to fade in/out during the change</param>
        /// <param name="transitionDuration">Custom duration, or null to use DefaultTransitionDuration</param>
        /// <remarks>
        /// <b>Best Practice:</b> Always await this method to ensure the scene change completes
        /// before executing subsequent code.
        ///
        /// <b>Performance Tip:</b> For frequently accessed scenes, use PreloadScene() first
        /// for instant loading.
        ///
        /// <b>Example:</b>
        /// <code>
        /// // Standard usage
        /// await SceneManager.Instance.ChangeSceneAsync("res://scenes/level_1.tscn");
        ///
        /// // Instant change (no fade)
        /// await SceneManager.Instance.ChangeSceneAsync("res://scenes/game_over.tscn", false);
        ///
        /// // Slow dramatic fade
        /// await SceneManager.Instance.ChangeSceneAsync("res://scenes/credits.tscn", true, 2.0f);
        /// </code>
        /// </remarks>
        public async Task ChangeSceneAsync(
            string scenePath,
            bool useTransition = true,
            float? transitionDuration = null
        )
        {
            if (_isTransitioning)
                return;

            _isTransitioning = true;
            float duration = transitionDuration ?? DefaultTransitionDuration;

            if (useTransition)
                await FadeOut(duration);

            await LoadSceneInternal(scenePath);

            if (useTransition)
                await FadeIn(duration);

            _isTransitioning = false;
        }

        // Simple overload to satisfy ISceneManager.ChangeSceneAsync(string)
        public Task ChangeSceneAsync(string scenePath) => ChangeSceneAsync(scenePath, true, null);

        /// <summary>
        /// Change scene with a custom loading screen for large scenes.
        /// </summary>
        /// <param name="scenePath">Full resource path to the scene</param>
        /// <param name="loadingScene">Optional PackedScene for custom loading screen UI</param>
        /// <remarks>
        /// <b>When to Use:</b> For large scenes (>50MB) or complex levels that take time to load.
        /// Shows loading screen while scene loads in background thread.
        ///
        /// <b>Loading Screen Requirements:</b>
        /// - Must be a Control node (UI)
        /// - Keep it lightweight (small textures, simple animations)
        /// - Connect to SceneLoading signal to update progress bar
        ///
        /// <b>Example:</b>
        /// <code>
        /// var loader = GD.Load&lt;PackedScene&gt;("res://ui/loading_screen.tscn");
        /// await SceneManager.Instance.ChangeSceneWithLoadingAsync(
        ///     "res://levels/open_world.tscn",
        ///     loader
        /// );
        /// </code>
        ///
        /// <b>Loading Screen Example:</b>
        /// <code>
        /// // In your loading screen script:
        /// public override void _Ready()
        /// {
        ///     SceneManager.Instance.SceneLoading += UpdateProgress;
        /// }
        ///
        /// private void UpdateProgress(string path, float progress)
        /// {
        ///     progressBar.Value = progress * 100;
        /// }
        /// </code>
        /// </remarks>
        public async Task ChangeSceneWithLoadingAsync(
            string scenePath,
            PackedScene loadingScene = null
        )
        {
            if (_isTransitioning)
                return;

            _isTransitioning = true;

            Control loadingScreen = null;
            if (loadingScene != null)
            {
                loadingScreen = loadingScene.Instantiate<Control>();
                AddChild(loadingScreen);
            }

            await LoadSceneInternal(scenePath, true);

            if (loadingScreen != null)
            {
                loadingScreen.QueueFree();
            }

            _isTransitioning = false;
        }

        /// <summary>
        /// Go back to the previous scene in history.
        /// </summary>
        /// <param name="useTransition">Whether to fade during the change</param>
        /// <remarks>
        /// <b>Requirements:</b> EnableHistory must be true and history must not be empty.
        ///
        /// <b>Best Practice:</b> Always check GetHistoryCount() before calling to avoid warnings.
        /// Useful for pause menus, settings screens, or any back button functionality.
        ///
        /// <b>Memory Note:</b> Scenes in history are not kept in memory - they're reloaded
        /// from disk when navigating back.
        ///
        /// <b>Example:</b>
        /// <code>
        /// // Safe back button implementation
        /// public partial class PauseMenu : Control
        /// {
        ///     private async void OnBackPressed()
        ///     {
        ///         if (SceneManager.Instance.GetHistoryCount() > 0)
        ///             await SceneManager.Instance.GoBackAsync();
        ///         else
        ///             GD.Print("No previous scene");
        ///     }
        /// }
        /// </code>
        /// </remarks>
        public async Task GoBackAsync(bool useTransition = true)
        {
            if (!EnableHistory || _sceneHistory.Count == 0)
            {
                GD.PushWarning("No scene history available");
                return;
            }

            string previousScene = _sceneHistory.Pop();
            await ChangeSceneAsync(previousScene, useTransition);
        }

        /// <summary>
        /// Preload a scene into memory for instant loading later.
        /// </summary>
        /// <param name="scenePath">Full resource path to the scene</param>
        /// <remarks>
        /// <b>When to Use:</b>
        /// - During loading screens to preload next level
        /// - In main menu to preload game start
        /// - Before cutscenes to ensure smooth playback
        ///
        /// <b>Memory Management:</b>
        /// - Preloaded scenes stay in memory until loaded or manually unloaded
        /// - Call UnloadScene() when no longer needed
        /// - Use sparingly for large scenes to avoid memory issues
        ///
        /// <b>Performance:</b> Preloaded scenes load instantly with no disk I/O.
        ///
        /// <b>Example Usage Pattern:</b>
        /// <code>
        /// // During level 1 loading screen
        /// SceneManager.Instance.PreloadScene("res://levels/level_2.tscn");
        ///
        /// // When player reaches end of level 1
        /// await SceneManager.Instance.ChangeSceneAsync("res://levels/level_2.tscn"); // Instant!
        ///
        /// // After cutscene, unload unused preloads
        /// SceneManager.Instance.UnloadScene("res://cutscenes/intro.tscn");
        /// </code>
        ///
        /// <b>Best Practice:</b> Preload next scene, not all scenes. Preload during
        /// natural breaks (loading screens, menus, paused gameplay).
        /// </remarks>
        public void PreloadScene(string scenePath)
        {
            if (_preloadedScenes.ContainsKey(scenePath))
                return;

            var scene = GD.Load<PackedScene>(scenePath);
            if (scene != null)
            {
                _preloadedScenes[scenePath] = scene;
                GD.Print($"Preloaded scene: {scenePath}");
            }
        }

        /// <summary>
        /// Unload a preloaded scene to free memory.
        /// </summary>
        /// <param name="scenePath">Full resource path to the scene</param>
        /// <remarks>
        /// <b>When to Use:</b>
        /// - After loading a preloaded scene (it auto-removes but manual is clearer)
        /// - When changing game sections (e.g., exiting dungeon area)
        /// - During memory cleanup routines
        ///
        /// <b>Memory Tip:</b> Preloaded scenes are automatically removed when loaded,
        /// but manual unloading is good practice for scenes you preloaded but didn't use.
        ///
        /// <b>Example:</b>
        /// <code>
        /// // Preload multiple potential next scenes
        /// SceneManager.Instance.PreloadScene("res://levels/forest.tscn");
        /// SceneManager.Instance.PreloadScene("res://levels/cave.tscn");
        ///
        /// // Player chose forest, unload the cave
        /// SceneManager.Instance.UnloadScene("res://levels/cave.tscn");
        /// await SceneManager.Instance.ChangeSceneAsync("res://levels/forest.tscn");
        /// </code>
        /// </remarks>
        public void UnloadScene(string scenePath)
        {
            if (_preloadedScenes.Remove(scenePath))
            {
                GD.Print($"Unloaded scene: {scenePath}");
            }
        }

        /// <summary>
        /// Reload the current scene from disk.
        /// </summary>
        /// <param name="useTransition">Whether to fade during reload</param>
        /// <remarks>
        /// <b>Common Use Cases:</b>
        /// - Player death/respawn
        /// - Level restart
        /// - Try again functionality
        ///
        /// <b>Performance Tip:</b> For quick respawns (like death loops), disable
        /// transitions: ReloadSceneAsync(false)
        ///
        /// <b>State Warning:</b> All scene state is lost on reload. Save important
        /// data to autoload singletons or files before reloading.
        ///
        /// <b>Example:</b>
        /// <code>
        /// // Quick respawn on death
        /// private async void OnPlayerDied()
        /// {
        ///     GameState.Instance.DeathCount++;
        ///     await SceneManager.Instance.ReloadSceneAsync(useTransition: false);
        /// }
        ///
        /// // Restart from pause menu
        /// private async void OnRestartPressed()
        /// {
        ///     await SceneManager.Instance.ReloadSceneAsync();
        /// }
        /// </code>
        /// </remarks>
        public async Task ReloadSceneAsync(bool useTransition = true)
        {
            if (_currentScene == null)
                return;
            string currentPath = _currentScene.SceneFilePath;
            await ChangeSceneAsync(currentPath, useTransition);
        }

        private async Task LoadSceneInternal(string scenePath, bool showProgress = false)
        {
            // Save current scene to history
            if (EnableHistory && _currentScene != null)
            {
                string currentPath = _currentScene.SceneFilePath;
                if (_sceneHistory.Count >= MaxHistorySize)
                {
                    var temp = _sceneHistory.ToArray();
                    _sceneHistory.Clear();
                    for (int i = 1; i < temp.Length; i++)
                        _sceneHistory.Push(temp[i]);
                }
                _sceneHistory.Push(currentPath);
            }

            PackedScene newScene;

            // Check if scene is preloaded
            if (_preloadedScenes.TryGetValue(scenePath, out var preloaded))
            {
                newScene = preloaded;
                _preloadedScenes.Remove(scenePath);
            }
            else
            {
                if (showProgress)
                {
                    newScene = await LoadSceneWithProgressAsync(scenePath);
                }
                else
                {
                    newScene = GD.Load<PackedScene>(scenePath);
                }
            }

            if (newScene == null)
            {
                GD.PushError($"Failed to load scene: {scenePath}");
                return;
            }

            // Remove old scene
            if (_currentScene != null)
            {
                _currentScene.QueueFree();
            }

            // Add new scene
            _currentScene = newScene.Instantiate();
            GetTree().Root.AddChild(_currentScene);
            GetTree().CurrentScene = _currentScene;

            EmitSignal(SignalName.SceneChanged, scenePath);
            _onSceneChangedHandlers?.Invoke(scenePath);
        }

        private async Task<PackedScene> LoadSceneWithProgressAsync(string scenePath)
        {
            var loader = ResourceLoader.LoadThreadedRequest(scenePath);

            while (true)
            {
                var progress = new Godot.Collections.Array();
                var status = ResourceLoader.LoadThreadedGetStatus(scenePath, progress);

                if (status == ResourceLoader.ThreadLoadStatus.InProgress)
                {
                    float progressValue = progress.Count > 0 ? (float)progress[0] : 0f;
                    EmitSignal(SignalName.SceneLoading, scenePath, progressValue);
                    _onSceneLoadingHandlers?.Invoke(scenePath, progressValue);
                    await Task.Delay(100);
                }
                else if (status == ResourceLoader.ThreadLoadStatus.Loaded)
                {
                    EmitSignal(SignalName.SceneLoading, scenePath, 1f);
                    _onSceneLoadingHandlers?.Invoke(scenePath, 1f);
                    return ResourceLoader.LoadThreadedGet(scenePath) as PackedScene;
                }
                else
                {
                    GD.PushError($"Failed to load scene: {scenePath}");
                    return null;
                }
            }
        }

        private async Task FadeOut(float duration)
        {
            _transitionColorRect.MouseFilter = Control.MouseFilterEnum.Stop;
            var tween = CreateTween();
            tween.TweenProperty(_transitionColorRect, "modulate:a", 1f, duration);
            await ToSignal(tween, Tween.SignalName.Finished);
        }

        private async Task FadeIn(float duration)
        {
            var tween = CreateTween();
            tween.TweenProperty(_transitionColorRect, "modulate:a", 0f, duration);
            await ToSignal(tween, Tween.SignalName.Finished);
            _transitionColorRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        }

        /// <summary>
        /// Clear all scene history.
        /// </summary>
        /// <remarks>
        /// <b>When to Use:</b>
        /// - When starting a new game (clear menu history)
        /// - After major game state changes
        /// - To prevent returning to invalid scenes
        ///
        /// <b>Example:</b>
        /// <code>
        /// // Starting new game - don't allow back to main menu
        /// private async void OnNewGamePressed()
        /// {
        ///     SceneManager.Instance.ClearHistory();
        ///     await SceneManager.Instance.ChangeSceneAsync("res://levels/level_1.tscn");
        /// }
        /// </code>
        /// </remarks>
        public void ClearHistory()
        {
            _sceneHistory.Clear();
        }

        /// <summary>
        /// Get the number of scenes in history.
        /// </summary>
        /// <returns>Number of previous scenes available</returns>
        /// <remarks>
        /// <b>Use Case:</b> Check before calling GoBackAsync() to prevent warnings.
        /// Enable/disable back buttons based on history availability.
        ///
        /// <b>Example:</b>
        /// <code>
        /// // Update UI based on history
        /// public override void _Process(double delta)
        /// {
        ///     backButton.Disabled = SceneManager.Instance.GetHistoryCount() == 0;
        /// }
        /// </code>
        /// </remarks>
        public int GetHistoryCount() => _sceneHistory.Count;
    }
}

// ============================================
// COMPREHENSIVE USAGE GUIDE
// ============================================

/*
╔════════════════════════════════════════════════════════════════════════╗
║                            SETUP GUIDE                                  ║
╚════════════════════════════════════════════════════════════════════════╝

1. INSTALLATION:
   - Save this file to: addons/scene_manager/SceneManager.cs
   - Open Project → Project Settings → Autoload
   - Add new entry:
     • Name: SceneManager
     • Path: res://addons/scene_manager/SceneManager.cs
     • Enable: [✓]
   - Click "Add" and "Close"

2. VERIFY INSTALLATION:
   - Create a test script with: SceneManager.Instance
   - No errors = successfully installed

╔════════════════════════════════════════════════════════════════════════╗
║                          BASIC EXAMPLES                                 ║
╚════════════════════════════════════════════════════════════════════════╝

// Simple scene change with fade
await SceneManager.Instance.ChangeSceneAsync("res://scenes/main_menu.tscn");

// Instant scene change (no transition)
await SceneManager.Instance.ChangeSceneAsync("res://scenes/game.tscn", useTransition: false);

// Custom fade duration (2 seconds)
await SceneManager.Instance.ChangeSceneAsync("res://scenes/credits.tscn", true, 2.0f);

// Reload current scene (respawn/retry)
await SceneManager.Instance.ReloadSceneAsync();

// Go back to previous scene
if (SceneManager.Instance.GetHistoryCount() > 0)
    await SceneManager.Instance.GoBackAsync();

╔════════════════════════════════════════════════════════════════════════╗
║                        ADVANCED PATTERNS                                ║
╚════════════════════════════════════════════════════════════════════════╝

// ---- PATTERN 1: Main Menu System ----
public partial class MainMenu : Control
{
    private async void OnNewGamePressed()
    {
        // Clear menu navigation history
        SceneManager.Instance.ClearHistory();
        
        // Preload first level during loading
        SceneManager.Instance.PreloadScene("res://levels/level_1.tscn");
        
        // Show loading screen
        var loader = GD.Load<PackedScene>("res://ui/loading.tscn");
        await SceneManager.Instance.ChangeSceneWithLoadingAsync(
            "res://scenes/game.tscn",
            loader
        );
    }
    
    private async void OnContinuePressed()
    {
        // Load saved game scene
        string savedScene = SaveSystem.GetLastScene();
        await SceneManager.Instance.ChangeSceneAsync(savedScene);
    }
}

// ---- PATTERN 2: Level Progression ----
public partial class LevelComplete : Control
{
    private async void OnNextLevelPressed()
    {
        // Preload next level for instant transition
        SceneManager.Instance.PreloadScene("res://levels/level_2.tscn");
        
        // Quick transition to next level
        await SceneManager.Instance.ChangeSceneAsync("res://levels/level_2.tscn");
    }
}

// ---- PATTERN 3: Death/Respawn System ----
public partial class Player : CharacterBody2D
{
    private async void OnDeath()
    {
        // Save death statistics
        GameStats.Instance.Deaths++;
        
        // Quick respawn without transition
        await SceneManager.Instance.ReloadSceneAsync(useTransition: false);
    }
}

// ---- PATTERN 4: Pause Menu with Back ----
public partial class PauseMenu : Control
{
    public override void _Ready()
    {
        UpdateBackButton();
    }
    
    private async void OnResumePressed()
    {
        Hide();
    }
    
    private async void OnMainMenuPressed()
    {
        await SceneManager.Instance.ChangeSceneAsync("res://scenes/main_menu.tscn");
    }
    
    private async void OnRestartPressed()
    {
        await SceneManager.Instance.ReloadSceneAsync();
    }
    
    private void UpdateBackButton()
    {
        var backButton = GetNode<Button>("BackButton");
        backButton.Disabled = SceneManager.Instance.GetHistoryCount() == 0;
    }
}

// ---- PATTERN 5: Loading Screen with Progress ----
public partial class LoadingScreen : Control
{
    private ProgressBar _progressBar;
    private Label _statusLabel;
    
    public override void _Ready()
    {
        _progressBar = GetNode<ProgressBar>("ProgressBar");
        _statusLabel = GetNode<Label>("StatusLabel");
        
        // Connect to loading signal
        SceneManager.Instance.SceneLoading += UpdateProgress;
    }
    
    private void UpdateProgress(string scenePath, float progress)
    {
        _progressBar.Value = progress * 100;
        _statusLabel.Text = $"Loading... {progress * 100:F0}%";
    }
    
    public override void _ExitTree()
    {
        // Clean up signal connection
        SceneManager.Instance.SceneLoading -= UpdateProgress;
    }
}

// ---- PATTERN 6: Scene Preloading Strategy ----
public partial class GameManager : Node
{
    public override void _Ready()
    {
        // Preload commonly accessed scenes during startup
        PreloadCommonScenes();
        
        // Listen for scene changes to preload next scenes
        SceneManager.Instance.SceneChanged += OnSceneChanged;
    }
    
    private void PreloadCommonScenes()
    {
        // Preload pause menu for instant access
        SceneManager.Instance.PreloadScene("res://ui/pause_menu.tscn");
        
        // Preload game over screen
        SceneManager.Instance.PreloadScene("res://ui/game_over.tscn");
    }
    
    private void OnSceneChanged(string scenePath)
    {
        // Smart preloading based on current scene
        if (scenePath.Contains("level_1"))
        {
            SceneManager.Instance.PreloadScene("res://levels/level_2.tscn");
        }
        else if (scenePath.Contains("level_2"))
        {
            SceneManager.Instance.PreloadScene("res://levels/level_3.tscn");
            // Unload level 1 to free memory
            SceneManager.Instance.UnloadScene("res://levels/level_1.tscn");
        }
    }
}

// ---- PATTERN 7: Cutscene System ----
public partial class CutscenePlayer : Node
{
    private async void PlayCutscene(string cutscenePath, string nextScenePath)
    {
        // Preload next scene during cutscene
        SceneManager.Instance.PreloadScene(nextScenePath);
        
        // Play cutscene
        await SceneManager.Instance.ChangeSceneAsync(cutscenePath, true, 1.0f);
        
        // ... cutscene logic ...
        
        // Instant transition to game (already preloaded)
        await SceneManager.Instance.ChangeSceneAsync(nextScenePath, false);
    }
}

╔════════════════════════════════════════════════════════════════════════╗
║                          BEST PRACTICES                                 ║
╚════════════════════════════════════════════════════════════════════════╝

1. ASYNC/AWAIT:
   ✓ ALWAYS use await with scene changes
   ✗ Don't fire-and-forget: ChangeSceneAsync(...); // Wrong!
   ✓ Correct: await SceneManager.Instance.ChangeSceneAsync(...);

2. MEMORY MANAGEMENT:
   ✓ Preload only 2-3 scenes at a time
   ✓ Unload unused preloaded scenes
   ✗ Don't preload every scene in your game
   ✓ Preload during natural breaks (loading, menus)

3. HISTORY USAGE:
   ✓ Check GetHistoryCount() before GoBackAsync()
   ✓ Clear history when starting new games
   ✗ Don't rely on history for critical navigation
   ✓ Use history for back buttons and undo-like features

4. TRANSITIONS:
   ✓ Use transitions for major scene changes (menu to game)
   ✗ Skip transitions for rapid changes (death loops)
   ✓ Longer transitions (1-2s) for dramatic moments
   ✓ Quick transitions (0.2-0.3s) for regular gameplay

5. LOADING SCREENS:
   ✓ Use for scenes >50MB or complex levels
   ✓ Connect to SceneLoading signal for progress bars
   ✓ Keep loading screens lightweight
   ✗ Don't use loading screens for small scenes

6. ERROR HANDLING:
   ✓ Verify scene paths are correct
   ✓ Check console for error messages
   ✓ Test scene changes in different scenarios
   
7. PERFORMANCE:
   ✓ Preload next level during current gameplay
   ✓ Use threaded loading for large scenes
   ✗ Don't load many scenes synchronously
   ✓ Profile memory usage with preloading

╔════════════════════════════════════════════════════════════════════════╗
║                      COMMON ISSUES & SOLUTIONS                          ║
╚════════════════════════════════════════════════════════════════════════╝

ISSUE: "Scene doesn't change"
→ Solution: Make sure you're using await
→ Check console for error messages
→ Verify scene path is correct (res://...)

ISSUE: "Memory usage keeps growing"
→ Solution: Call UnloadScene() for unused preloads
→ Don't preload too many scenes at once
→ Clear history periodically

ISSUE: "Transition feels too slow/fast"
→ Solution: Adjust DefaultTransitionDuration in autoload
→ Or use custom duration per change

ISSUE: "GoBackAsync() doesn't work"
→ Solution: Check EnableHistory is true
→ Verify GetHistoryCount() > 0
→ History only works for scenes loaded via ChangeSceneAsync

ISSUE: "Loading screen doesn't show progress"
→ Solution: Connect to SceneLoading signal
→ Use ChangeSceneWithLoadingAsync, not ChangeSceneAsync
→ Ensure loading screen script is attached

╔════════════════════════════════════════════════════════════════════════╗
║                         SIGNAL EXAMPLES                                 ║
╚════════════════════════════════════════════════════════════════════════╝

// Track all scene changes
SceneManager.Instance.SceneChanged += (scenePath) => {
    GD.Print($"Now in: {scenePath}");
    Analytics.TrackSceneChange(scenePath);
};

// Update loading UI
SceneManager.Instance.SceneLoading += (scenePath, progress) => {
    loadingBar.Value = progress;
    statusText.Text = $"Loading {scenePath.GetFile()}...";
};

// React to specific scenes
SceneManager.Instance.SceneChanged += (scenePath) => {
    if (scenePath.Contains("game_over"))
        PlayGameOverMusic();
    else if (scenePath.Contains("victory"))
        PlayVictoryMusic();
};
*/
