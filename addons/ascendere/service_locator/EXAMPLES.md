# Service Locator - Practical Examples

Comprehensive real-world examples for using the Godot C# Service Locator. Each example demonstrates different features and patterns.

---

## Table of Contents

1. [Basic Service Registration](#basic-service-registration)
2. [Game State Management](#game-state-management)
3. [Audio System](#audio-system)
4. [Configuration Service](#configuration-service)
5. [Save/Load System](#saveload-system)
6. [Event Broadcasting](#event-broadcasting)
7. [Multiple Named Services](#multiple-named-services)
8. [Service Scopes](#service-scopes)
9. [Async Initialization](#async-initialization)
10. [Decorators & Wrappers](#decorators--wrappers)
11. [Middleware Pipeline](#middleware-pipeline)
12. [Transient Services](#transient-services)

---

## Basic Service Registration

### Define the Service Interface

```csharp
public interface IScoreService
{
    int Score { get; set; }
    void AddScore(int points);
    void ResetScore();
}
```

### Implement with [Service] Attribute

```csharp
[Service(typeof(IScoreService))]
public class ScoreService : IScoreService
{
    public int Score { get; set; }

    public void AddScore(int points)
    {
        if (points < 0)
        {
            GD.PrintErr("Cannot add negative points");
            return;
        }
        Score += points;
        GD.Print($"Score increased to {Score}");
    }

    public void ResetScore()
    {
        Score = 0;
        GD.Print("Score reset");
    }
}
```

### Inject and Use

```csharp
public partial class CoinCollector : Area2D
{
    [Inject] private IScoreService _scoreService;

    public override void _Ready()
    {
        ServiceLocator.InjectMembers(this);
        AreaEntered += OnAreaEntered;
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area.IsInGroup("coins"))
        {
            _scoreService?.AddScore(10);
            area.QueueFree();
        }
    }
}
```

---

## Game State Management

Perfect for managing persistent game state across scenes.

### Interface Definition

```csharp
public interface IGameState
{
    int Score { get; }
    int Lives { get; }
    int Level { get; }
    event Action<int> OnScoreChanged;
    event Action<int> OnLivesChanged;
    event Action<int> OnLevelChanged;
    
    void AddScore(int points);
    void LoseLife();
    void GainLife();
    void NextLevel();
    void Reset();
}
```

### Implementation

```csharp
[Service(typeof(IGameState), ServiceLifetime.Singleton)]
public class GameState : IGameState
{
    private int _score;
    private int _lives;
    private int _level;

    public int Score => _score;
    public int Lives => _lives;
    public int Level => _level;

    public event Action<int> OnScoreChanged;
    public event Action<int> OnLivesChanged;
    public event Action<int> OnLevelChanged;

    [Initialize]
    public void Initialize()
    {
        Reset();
        GD.Print("[GameState] Initialized");
    }

    public void AddScore(int points)
    {
        _score += Mathf.Max(0, points);
        OnScoreChanged?.Invoke(_score);
    }

    public void LoseLife()
    {
        _lives--;
        OnLivesChanged?.Invoke(_lives);
        
        if (_lives <= 0)
        {
            GD.Print("[GameState] Game Over!");
        }
    }

    public void GainLife()
    {
        _lives = Mathf.Min(_lives + 1, 3); // Max 3 lives
        OnLivesChanged?.Invoke(_lives);
    }

    public void NextLevel()
    {
        _level++;
        OnLevelChanged?.Invoke(_level);
        GD.Print($"[GameState] Advanced to level {_level}");
    }

    public void Reset()
    {
        _score = 0;
        _lives = 3;
        _level = 1;
    }
}
```

### Usage in UI

```csharp
public partial class HUD : CanvasLayer
{
    [Inject] private IGameState _gameState;
    
    private Label _scoreLabel;
    private Label _livesLabel;
    private Label _levelLabel;

    public override void _Ready()
    {
        ServiceLocator.InjectMembers(this);
        
        _scoreLabel = GetNode<Label>("%ScoreLabel");
        _livesLabel = GetNode<Label>("%LivesLabel");
        _levelLabel = GetNode<Label>("%LevelLabel");

        // Subscribe to events
        _gameState.OnScoreChanged += UpdateScore;
        _gameState.OnLivesChanged += UpdateLives;
        _gameState.OnLevelChanged += UpdateLevel;

        // Initial update
        UpdateScore(_gameState.Score);
        UpdateLives(_gameState.Lives);
        UpdateLevel(_gameState.Level);
    }

    private void UpdateScore(int newScore) => _scoreLabel.Text = $"Score: {newScore}";
    private void UpdateLives(int newLives) => _livesLabel.Text = $"Lives: {newLives}";
    private void UpdateLevel(int newLevel) => _levelLabel.Text = $"Level: {newLevel}";

    public override void _ExitTree()
    {
        if (_gameState != null)
        {
            _gameState.OnScoreChanged -= UpdateScore;
            _gameState.OnLivesChanged -= UpdateLives;
            _gameState.OnLevelChanged -= UpdateLevel;
        }
    }
}
```

---

## Audio System

Managing audio playback with a dedicated service.

### Audio Service Interfaces

```csharp
public interface IAudioService
{
    void PlayMusic(string trackName, bool loop = true);
    void StopMusic(float fadeOutDuration = 0.5f);
    void PlaySfx(string sfxName, float pitchVariance = 0.05f);
    void SetMusicVolume(float volume);
    void SetSfxVolume(float volume);
    float GetMusicVolume();
    float GetSfxVolume();
}
```

### Implementation

```csharp
[Service(typeof(IAudioService), ServiceLifetime.Singleton)]
public class AudioService : IAudioService
{
    private AudioStreamPlayer _musicPlayer;
    private Node _sfxContainer;
    private float _musicVolume = 0.8f;
    private float _sfxVolume = 0.7f;
    private Dictionary<string, AudioStream> _audioCache = new();

    [Initialize]
    public void Initialize()
    {
        // Create music player
        _musicPlayer = new AudioStreamPlayer { BusName = "Music" };
        _musicPlayer.VolumeDb = Mathf.LinearToDb(_musicVolume);

        // Create SFX container
        _sfxContainer = new Node { Name = "SFX" };
    }

    public void PlayMusic(string trackName, bool loop = true)
    {
        var track = GD.Load<AudioStream>($"res://assets/audio/music/{trackName}.ogg");
        
        if (track == null)
        {
            GD.PrintErr($"[AudioService] Music not found: {trackName}");
            return;
        }

        _musicPlayer.Stream = track;
        _musicPlayer.Bus = "Music";
        _musicPlayer.Play();
        
        GD.Print($"[AudioService] Playing music: {trackName}");
    }

    public void StopMusic(float fadeOutDuration = 0.5f)
    {
        if (fadeOutDuration <= 0)
        {
            _musicPlayer.Stop();
        }
        else
        {
            var tween = _musicPlayer.CreateTween();
            tween.SetTrans(Tween.TransitionType.Linear);
            tween.SetEase(Tween.EaseType.InOut);
            tween.TweenProperty(_musicPlayer, "volume_db", -80f, fadeOutDuration);
            tween.TweenCallback(Callable.From(() => _musicPlayer.Stop()));
        }
    }

    public void PlaySfx(string sfxName, float pitchVariance = 0.05f)
    {
        var sfxPath = $"res://assets/audio/sfx/{sfxName}.ogg";
        
        if (!_audioCache.TryGetValue(sfxPath, out var sfx))
        {
            sfx = GD.Load<AudioStream>(sfxPath);
            if (sfx == null)
            {
                GD.PrintErr($"[AudioService] SFX not found: {sfxName}");
                return;
            }
            _audioCache[sfxPath] = sfx;
        }

        var player = new AudioStreamPlayer { Bus = "SFX" };
        player.Stream = sfx;
        player.VolumeDb = Mathf.LinearToDb(_sfxVolume);
        player.PitchScale = 1.0f + GD.Randf() * pitchVariance * (GD.Randf() > 0.5f ? 1 : -1);
        
        _sfxContainer.AddChild(player);
        player.Play();
        
        // Clean up after playback
        var duration = sfx.GetLength();
        var timer = new Timer();
        _sfxContainer.AddChild(timer);
        timer.WaitTime = duration + 0.1f;
        timer.OneShot = true;
        timer.Timeout += () => 
        {
            player.QueueFree();
            timer.QueueFree();
        };
        timer.Start();
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp(volume, 0f, 1f);
        _musicPlayer.VolumeDb = Mathf.LinearToDb(_musicVolume);
    }

    public void SetSfxVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp(volume, 0f, 1f);
    }

    public float GetMusicVolume() => _musicVolume;
    public float GetSfxVolume() => _sfxVolume;
}
```

### Usage in Game

```csharp
public partial class GameLevel : Node2D
{
    [Inject] private IAudioService _audioService;

    public override async void _Ready()
    {
        ServiceLocator.InjectMembers(this);
        
        _audioService?.PlayMusic("level_theme");
        await Task.Delay(100); // Small delay
        _audioService?.PlaySfx("start_sfx");
    }

    private void OnEnemyDefeated()
    {
        _audioService?.PlaySfx("enemy_defeat");
    }

    public async void LevelComplete()
    {
        _audioService?.PlaySfx("victory_fanfare");
        await Task.Delay(2000);
        _audioService?.StopMusic(fadeOutDuration: 1.0f);
    }
}
```

---

## Configuration Service

Centralized game configuration management.

### Configuration Interface

```csharp
public interface IConfigService
{
    float PlayerSpeed { get; }
    float PlayerJumpForce { get; }
    int MaxEnemies { get; }
    float DifficultyMultiplier { get; }
    string GameVersion { get; }
    T GetSetting<T>(string key, T defaultValue);
}
```

### Implementation

```csharp
[Service(typeof(IConfigService), ServiceLifetime.Singleton)]
public class ConfigService : IConfigService
{
    private readonly Dictionary<string, object> _settings = new();

    public float PlayerSpeed => GetSetting("player_speed", 300f);
    public float PlayerJumpForce => GetSetting("player_jump_force", 500f);
    public int MaxEnemies => GetSetting("max_enemies", 20);
    public float DifficultyMultiplier => GetSetting("difficulty_multiplier", 1.0f);
    public string GameVersion => GetSetting("version", "1.0.0");

    [Initialize]
    public void Initialize()
    {
        LoadConfigFile("res://config/game_config.json");
        GD.Print("[ConfigService] Configuration loaded");
    }

    private void LoadConfigFile(string path)
    {
        if (!ResourceLoader.Exists(path))
        {
            GD.PrintWarning($"[ConfigService] Config file not found: {path}, using defaults");
            return;
        }

        var json = Json.Stringify(Json.Parse(FileAccess.GetFileAsString(path)));
        var config = Json.ParseString(json);
        
        if (config.Type == Variant.Type.Dictionary)
        {
            var dict = (Dictionary)config;
            foreach (var key in dict.Keys)
            {
                _settings[key.ToString()] = dict[key];
            }
        }
    }

    public T GetSetting<T>(string key, T defaultValue)
    {
        if (!_settings.TryGetValue(key, out var value))
            return defaultValue;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
}
```

### Usage in Player

```csharp
public partial class Player : CharacterBody2D
{
    [Inject] private IConfigService _configService;
    [Inject] private IGameState _gameState;

    private float _speed;
    private float _jumpForce;

    public override void _Ready()
    {
        ServiceLocator.InjectMembers(this);
        
        _speed = _configService?.PlayerSpeed ?? 300f;
        _jumpForce = _configService?.PlayerJumpForce ?? 500f;
    }

    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetAxis("ui_left", "ui_right");
        Velocity = new Vector2(direction * _speed, Velocity.Y);
        
        if (Input.IsActionJustPressed("ui_accept"))
        {
            Velocity = Velocity with { Y = -_jumpForce };
        }

        MoveAndSlide();
    }
}
```

---

## Save/Load System

Persisting game data with async operations.

### Save System Interface

```csharp
public interface ISaveSystem
{
    Task SaveGameAsync(string slot, GameSaveData data);
    Task<GameSaveData> LoadGameAsync(string slot);
    Task DeleteSaveAsync(string slot);
    Task<bool> HasSaveAsync(string slot);
    Task<List<GameSaveData>> GetAllSavesAsync();
}

public class GameSaveData
{
    public int Score { get; set; }
    public int Lives { get; set; }
    public int Level { get; set; }
    public DateTime SaveTime { get; set; }
    public Vector2 PlayerPosition { get; set; }
}
```

### Implementation

```csharp
[Service(typeof(ISaveSystem), ServiceLifetime.Singleton)]
public class SaveSystem : ISaveSystem
{
    private const string SaveDirectory = "user://saves/";

    [Initialize]
    public void Initialize()
    {
        if (!DirAccess.Exists(SaveDirectory))
        {
            DirAccess.MakeDirRecursiveAbsolute(SaveDirectory);
        }
    }

    public async Task SaveGameAsync(string slot, GameSaveData data)
    {
        try
        {
            data.SaveTime = DateTime.Now;
            var json = Json.Stringify(ToJsonDict(data));
            
            var file = FileAccess.Open($"{SaveDirectory}save_{slot}.json", FileAccess.ModeFlags.Write);
            file.StoreString(json);
            
            GD.Print($"[SaveSystem] Game saved to slot {slot}");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SaveSystem] Failed to save: {ex.Message}");
        }
    }

    public async Task<GameSaveData> LoadGameAsync(string slot)
    {
        try
        {
            var filePath = $"{SaveDirectory}save_{slot}.json";
            if (!ResourceLoader.Exists(filePath))
            {
                GD.PrintWarning($"[SaveSystem] Save file not found: {slot}");
                return null;
            }

            var json = FileAccess.GetFileAsString(filePath);
            var data = Json.ParseString(json);
            
            GD.Print($"[SaveSystem] Game loaded from slot {slot}");
            return FromJsonDict(data);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SaveSystem] Failed to load: {ex.Message}");
            return null;
        }
    }

    public async Task DeleteSaveAsync(string slot)
    {
        try
        {
            var filePath = $"{SaveDirectory}save_{slot}.json";
            DirAccess.RemoveAbsolute(filePath);
            GD.Print($"[SaveSystem] Save deleted: {slot}");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SaveSystem] Failed to delete: {ex.Message}");
        }
    }

    public async Task<bool> HasSaveAsync(string slot)
    {
        return ResourceLoader.Exists($"{SaveDirectory}save_{slot}.json");
    }

    public async Task<List<GameSaveData>> GetAllSavesAsync()
    {
        var saves = new List<GameSaveData>();
        
        if (!DirAccess.Exists(SaveDirectory))
            return saves;

        var dir = DirAccess.Open(SaveDirectory);
        if (dir == null)
            return saves;

        dir.ListDirBegin();
        var fileName = dir.GetNextFile();
        
        while (!string.IsNullOrEmpty(fileName))
        {
            if (fileName.EndsWith(".json"))
            {
                var data = await LoadGameAsync(fileName.TrimSuffix(".json").TrimPrefix("save_"));
                if (data != null)
                    saves.Add(data);
            }
            fileName = dir.GetNextFile();
        }

        return saves;
    }

    private Dictionary ToJsonDict(GameSaveData data) => new()
    {
        { "score", data.Score },
        { "lives", data.Lives },
        { "level", data.Level },
        { "save_time", data.SaveTime.ToString("o") },
        { "player_x", data.PlayerPosition.X },
        { "player_y", data.PlayerPosition.Y }
    };

    private GameSaveData FromJsonDict(Variant jsonVariant)
    {
        var dict = (Dictionary)jsonVariant;
        return new GameSaveData
        {
            Score = (int)dict.GetValueOrDefault("score", 0),
            Lives = (int)dict.GetValueOrDefault("lives", 3),
            Level = (int)dict.GetValueOrDefault("level", 1),
            SaveTime = DateTime.Parse((string)dict.GetValueOrDefault("save_time", DateTime.Now.ToString("o"))),
            PlayerPosition = new Vector2((float)dict.GetValueOrDefault("player_x", 0f), (float)dict.GetValueOrDefault("player_y", 0f))
        };
    }
}
```

### Usage

```csharp
public partial class GameMenu : Control
{
    [Inject] private ISaveSystem _saveSystem;
    [Inject] private IGameState _gameState;

    public async void SaveGame()
    {
        var saveData = new GameSaveData
        {
            Score = _gameState.Score,
            Lives = _gameState.Lives,
            Level = _gameState.Level,
            PlayerPosition = GetTree().CurrentScene.FindChild("Player") is Node2D p ? p.GlobalPosition : Vector2.Zero
        };

        await _saveSystem.SaveGameAsync("slot_1", saveData);
        GD.Print("Game saved!");
    }

    public async void LoadGame()
    {
        var saveData = await _saveSystem.LoadGameAsync("slot_1");
        if (saveData != null)
        {
            _gameState.AddScore(saveData.Score);
            GD.Print($"Game loaded! Score: {saveData.Score}");
        }
    }
}
```

---

## Event Broadcasting

Using services to broadcast game-wide events.

### Event Service

```csharp
public interface IEventBus
{
    void Subscribe<T>(Action<T> handler) where T : IGameEvent;
    void Unsubscribe<T>(Action<T> handler) where T : IGameEvent;
    void Publish<T>(T eventData) where T : IGameEvent;
}

public interface IGameEvent { }

public class PlayerDeathEvent : IGameEvent { public int Lives { get; set; } }
public class EnemySpawnEvent : IGameEvent { public int EnemyId { get; set; } }
public class BossDefeatedEvent : IGameEvent { public string BossName { get; set; } }
```

### Implementation

```csharp
[Service(typeof(IEventBus), ServiceLifetime.Singleton)]
public class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

    public void Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        var eventType = typeof(T);
        if (!_subscribers.ContainsKey(eventType))
            _subscribers[eventType] = new List<Delegate>();

        _subscribers[eventType].Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
    {
        var eventType = typeof(T);
        if (_subscribers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);
        }
    }

    public void Publish<T>(T eventData) where T : IGameEvent
    {
        var eventType = typeof(T);
        if (_subscribers.TryGetValue(eventType, out var handlers))
        {
            foreach (var handler in handlers)
            {
                ((Action<T>)handler)?.Invoke(eventData);
            }
        }
    }
}
```

### Usage

```csharp
public partial class Player : CharacterBody2D
{
    [Inject] private IEventBus _eventBus;

    public override void _Ready()
    {
        ServiceLocator.InjectMembers(this);
        _eventBus?.Subscribe<BossDefeatedEvent>(OnBossDefeated);
    }

    private void Die()
    {
        _eventBus?.Publish(new PlayerDeathEvent { Lives = 2 });
    }

    private void OnBossDefeated(BossDefeatedEvent @event)
    {
        GD.Print($"Boss {event.BossName} has been defeated!");
        // React to boss defeat globally
    }

    public override void _ExitTree()
    {
        _eventBus?.Unsubscribe<BossDefeatedEvent>(OnBossDefeated);
    }
}
```

---

## Multiple Named Services

Register multiple implementations of the same interface.

### Logger Service Example

```csharp
public interface ILogger
{
    void Log(string message);
    void LogWarning(string message);
    void LogError(string message);
}

[Service(typeof(ILogger), name: "Console")]
public class ConsoleLogger : ILogger
{
    public void Log(string message) => GD.Print($"[CONSOLE] {message}");
    public void LogWarning(string message) => GD.PrintWarning($"[CONSOLE] {message}");
    public void LogError(string message) => GD.PrintErr($"[CONSOLE] {message}");
}

[Service(typeof(ILogger), name: "File")]
public class FileLogger : ILogger
{
    private const string LogFile = "user://game.log";

    public void Log(string message) => AppendToFile($"[LOG] {message}");
    public void LogWarning(string message) => AppendToFile($"[WARN] {message}");
    public void LogError(string message) => AppendToFile($"[ERROR] {message}");

    private void AppendToFile(string message)
    {
        var file = FileAccess.Open(LogFile, FileAccess.ModeFlags.ReadWrite);
        file?.Seek(0, FileAccess.SeekEnd);
        file?.StoreString($"{message}\n");
    }
}

[Service(typeof(ILogger), name: "Remote")]
public class RemoteLogger : ILogger
{
    public void Log(string message) => SendToServer("log", message);
    public void LogWarning(string message) => SendToServer("warn", message);
    public void LogError(string message) => SendToServer("error", message);

    private void SendToServer(string level, string message)
    {
        // Send to remote logging service
        GD.Print($"[REMOTE] {level}: {message}");
    }
}
```

### Usage

```csharp
public partial class GameDebugger : Node
{
    [Inject] private ILogger _consoleLogger;
    [Inject(name: "File")] private ILogger _fileLogger;
    [Inject(name: "Remote")] private ILogger _remoteLogger;

    public override void _Ready()
    {
        ServiceLocator.InjectMembers(this);

        _consoleLogger?.Log("Game started - console output");
        _fileLogger?.Log("Game started - file output");
        _remoteLogger?.Log("Game started - remote output");

        // Get all loggers at once
        var allLoggers = ServiceLocator.GetAllNamed<ILogger>();
        foreach (var kvp in allLoggers)
        {
            GD.Print($"Available logger: {kvp.Key}");
        }
    }
}
```

---

## Service Scopes

Creating isolated service instances for specific contexts.

```csharp
public interface IInventoryService
{
    void AddItem(string itemId);
    List<string> GetItems();
}

[Service(typeof(IInventoryService), ServiceLifetime.Scoped)]
public class InventoryService : IInventoryService
{
    private List<string> _items = new();

    public void AddItem(string itemId) => _items.Add(itemId);
    public List<string> GetItems() => new(_items);
}
```

### Using Scopes

```csharp
public partial class ShopScene : Node2D
{
    public override void _Ready()
    {
        // Create a scope for this shop scene
        var shopScope = ServiceLocator.CreateScope("shop_inventory");
        
        var playerInventory = ServiceLocator.Get<IInventoryService>();
        var shopInventory = shopScope.Resolve<IInventoryService>();

        playerInventory.AddItem("sword");
        shopInventory.AddItem("shield");

        GD.Print($"Player items: {string.Join(", ", playerInventory.GetItems())}"); // sword
        GD.Print($"Shop items: {string.Join(", ", shopInventory.GetItems())}"); // shield
    }
}
```

---

## Async Initialization

Services that require async setup.

```csharp
public interface INetworkService
{
    Task<bool> ConnectAsync(string serverAddress);
    void SendMessage(string message);
    Task DisconnectAsync();
}

[Service(typeof(INetworkService), ServiceLifetime.Singleton)]
public class NetworkService : INetworkService
{
    private bool _connected;

    [Initialize]
    public async Task InitializeAsync()
    {
        GD.Print("[NetworkService] Initializing...");
        await Task.Delay(1000); // Simulate connection setup
        GD.Print("[NetworkService] Initialized");
    }

    public async Task<bool> ConnectAsync(string serverAddress)
    {
        GD.Print($"[NetworkService] Connecting to {serverAddress}...");
        await Task.Delay(500);
        _connected = true;
        return true;
    }

    public void SendMessage(string message)
    {
        if (!_connected)
        {
            GD.PrintWarning("[NetworkService] Not connected");
            return;
        }
        GD.Print($"[NetworkService] Sent: {message}");
    }

    public async Task DisconnectAsync()
    {
        _connected = false;
        await Task.CompletedTask;
    }
}
```

### Initializing All Services

```csharp
public partial class GameManager : Node
{
    public override async void _Ready()
    {
        // Wait for all services to initialize
        await ServiceLocator.InitializeServicesAsync();
        GD.Print("All services initialized");
    }
}
```

---

## Decorators & Wrappers

Wrapping services with additional behavior.

```csharp
[Decorator(typeof(IScoreService), order: 0)]
public class ScoreServiceLogger : IServiceDecorator
{
    public object Decorate(object instance)
    {
        return new LoggingScoreServiceWrapper((IScoreService)instance);
    }
}

public class LoggingScoreServiceWrapper : IScoreService
{
    private readonly IScoreService _inner;

    public int Score => _inner.Score;

    public LoggingScoreServiceWrapper(IScoreService inner) => _inner = inner;

    public void AddScore(int points)
    {
        GD.Print($"[Decorator] Adding {points} points");
        _inner.AddScore(points);
        GD.Print($"[Decorator] Total score: {_inner.Score}");
    }

    public void ResetScore()
    {
        GD.Print("[Decorator] Resetting score");
        _inner.ResetScore();
    }
}
```

---

## Middleware Pipeline

Processing service resolution through middleware.

```csharp
public class TimingMiddleware : IServiceMiddleware
{
    public async Task<object> InvokeAsync(ServiceContext context, Func<Task<object>> next)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await next();
        sw.Stop();
        
        GD.Print($"[Timing] {context.ServiceType.Name} resolved in {sw.ElapsedMilliseconds}ms");
        return result;
    }
}

public class CachingMiddleware : IServiceMiddleware
{
    private readonly Dictionary<string, object> _cache = new();

    public async Task<object> InvokeAsync(ServiceContext context, Func<Task<object>> next)
    {
        var key = $"{context.ServiceType.FullName}:{context.ServiceName}";

        if (_cache.TryGetValue(key, out var cached))
        {
            GD.Print($"[Caching] Cache hit for {context.ServiceType.Name}");
            return cached;
        }

        var result = await next();
        _cache[key] = result;
        return result;
    }
}

public class ValidationMiddleware : IServiceMiddleware
{
    public async Task<object> InvokeAsync(ServiceContext context, Func<Task<object>> next)
    {
        if (context.ServiceType == null)
            throw new InvalidOperationException("Service type cannot be null");

        var result = await next();

        if (result == null)
            GD.PrintWarning($"[Validation] Service {context.ServiceType.Name} returned null");

        return result;
    }
}
```

### Setup

```csharp
public partial class GameManager : Node
{
    public override async void _Ready()
    {
        ServiceLocator.UseMiddleware(new ValidationMiddleware());
        ServiceLocator.UseMiddleware(new CachingMiddleware());
        ServiceLocator.UseMiddleware(new TimingMiddleware());
        
        await ServiceLocator.InitializeServicesAsync();
    }
}
```

---

## Transient Services

Creating new instances for each resolution.

```csharp
[Service(ServiceLifetime.Transient)]
public class BulletPool
{
    private readonly List<Bullet> _bullets = new();

    public BulletPool()
    {
        GD.Print("New BulletPool created");
    }

    public void CreateBullet() => _bullets.Add(new Bullet());
}

public partial class Player : CharacterBody2D
{
    public override void _Ready()
    {
        // Each call gets a new instance
        var pool1 = ServiceLocator.Get<BulletPool>();
        var pool2 = ServiceLocator.Get<BulletPool>();
        
        GD.Print(pool1 == pool2); // false - different instances
    }
}
```

---

## Best Practices Summary

1. **Use Interfaces**: Always define service interfaces for better testability
2. **Keep Services Stateless When Possible**: Makes them easier to reason about
3. **Use Singletons for Shared State**: Game state, audio, config
4. **Use Scopes for Isolated Contexts**: Different game levels or rooms
5. **Implement Cleanup**: Disconnect events in `_ExitTree()`
6. **Use Named Services**: For multiple implementations of same interface
7. **Leverage Middleware**: For cross-cutting concerns like logging, timing
8. **Document Service Contracts**: Clear interfaces and initialization requirements
9. **Validate Injected Services**: Always check for null before use
10. **Profile Performance**: Use middleware to identify bottlenecks

