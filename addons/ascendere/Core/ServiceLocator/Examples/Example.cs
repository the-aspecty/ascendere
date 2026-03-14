// === EXAMPLE MIDDLEWARE ===

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class LoggingMiddleware : IServiceMiddleware
{
    public async Task<object> InvokeAsync(ServiceContext context, Func<Task<object>> next)
    {
        var nameInfo = string.IsNullOrEmpty(context.ServiceName) ? "" : $" ({context.ServiceName})";
        GD.Print($"[Middleware] Resolving {context.ServiceType.Name}{nameInfo}...");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await next();
        stopwatch.Stop();

        GD.Print(
            $"[Middleware] Resolved {context.ServiceType.Name} in {stopwatch.ElapsedMilliseconds}ms"
        );

        return result;
    }
}

public class CachingMiddleware : IServiceMiddleware
{
    private readonly Dictionary<string, object> _cache = new();

    public async Task<object> InvokeAsync(ServiceContext context, Func<Task<object>> next)
    {
        var cacheKey = $"{context.ServiceType.FullName}:{context.ServiceName}";

        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            GD.Print($"[Middleware] Cache hit for {context.ServiceType.Name}");
            return cached;
        }

        var result = await next();
        _cache[cacheKey] = result;

        return result;
    }
}

public class ValidationMiddleware : IServiceMiddleware
{
    public async Task<object> InvokeAsync(ServiceContext context, Func<Task<object>> next)
    {
        // Pre-validation
        if (context.ServiceType == null)
        {
            GD.PrintErr("[Middleware] Invalid service type");
            return null;
        }

        var result = await next();

        // Post-validation
        if (result == null)
        {
            GD.PrintErr($"[Middleware] Service {context.ServiceType.Name} returned null");
        }

        return result;
    }
}

// === INTERFACES ===

public interface IAudioService
{
    void PlayMusic(AudioStream stream);
    void PlaySfx(AudioStream stream);
    void SetMusicVolume(float volume);
}

public interface IGameState
{
    int Score { get; }
    int Lives { get; }
    event Action<int> OnScoreChanged;
    void AddScore(int points);
    void LoseLife();
    void Reset();
}

public interface ISaveSystem
{
    Task SaveAsync(string key, object data);
    Task<T> LoadAsync<T>(string key);
}

public interface ILogger
{
    void Log(string message);
    void LogError(string message);
}

// === SERVICE IMPLEMENTATIONS ===

[Service(typeof(IAudioService), priority: 10)]
public class AudioService : IAudioService
{
    private AudioStreamPlayer _musicPlayer;
    private AudioStreamPlayer _sfxPlayer;

    [PostInject]
    private void OnPostInject()
    {
        GD.Print("[AudioService] Dependencies injected!");
    }

    public void Initialize(AudioStreamPlayer music, AudioStreamPlayer sfx)
    {
        _musicPlayer = music;
        _sfxPlayer = sfx;
    }

    public void PlayMusic(AudioStream stream)
    {
        if (_musicPlayer != null)
        {
            _musicPlayer.Stream = stream;
            _musicPlayer.Play();
        }
    }

    public void PlaySfx(AudioStream stream)
    {
        if (_sfxPlayer != null)
        {
            _sfxPlayer.Stream = stream;
            _sfxPlayer.Play();
        }
    }

    public void SetMusicVolume(float volume)
    {
        if (_musicPlayer != null)
            _musicPlayer.VolumeDb = Mathf.LinearToDb(volume);
    }
}

// === DECORATOR EXAMPLE ===

[Decorator(typeof(IAudioService), order: 1)]
public class AudioLoggingDecorator : IServiceDecorator<IAudioService>
{
    public IAudioService Decorate(IAudioService instance)
    {
        return new LoggedAudioService(instance);
    }

    private class LoggedAudioService : IAudioService
    {
        private readonly IAudioService _inner;

        public LoggedAudioService(IAudioService inner)
        {
            _inner = inner;
        }

        public void PlayMusic(AudioStream stream)
        {
            GD.Print($"[AudioDecorator] Playing music: {stream?.ResourcePath}");
            _inner.PlayMusic(stream);
        }

        public void PlaySfx(AudioStream stream)
        {
            GD.Print($"[AudioDecorator] Playing SFX: {stream?.ResourcePath}");
            _inner.PlaySfx(stream);
        }

        public void SetMusicVolume(float volume)
        {
            GD.Print($"[AudioDecorator] Setting music volume: {volume}");
            _inner.SetMusicVolume(volume);
        }
    }
}

// === NAMED SERVICES EXAMPLE ===

[Service(typeof(ILogger), name: "FileLogger")]
public class FileLogger : ILogger
{
    public void Log(string message)
    {
        GD.Print($"[FileLogger] {message}");
        // Write to file
    }

    public void LogError(string message)
    {
        GD.PrintErr($"[FileLogger] {message}");
        // Write error to file
    }
}

[Service(typeof(ILogger), name: "ConsoleLogger")]
public class ConsoleLogger : ILogger
{
    public void Log(string message)
    {
        GD.Print($"[ConsoleLogger] {message}");
    }

    public void LogError(string message)
    {
        GD.PrintErr($"[ConsoleLogger] {message}");
    }
}

[Service(typeof(IGameState))]
public class GameStateService : IGameState
{
    [Inject]
    private ISaveSystem _saveSystem;

    [Inject(name: "FileLogger")]
    private ILogger _fileLogger;

    [Inject(name: "ConsoleLogger")]
    private ILogger _consoleLogger;

    public int Score { get; private set; }
    public int Lives { get; private set; } = 3;
    public event Action<int> OnScoreChanged;

    [Initialize]
    private async Task InitializeAsync()
    {
        Score = await _saveSystem.LoadAsync<int>("score");
        _fileLogger?.Log($"Loaded score: {Score}");
    }

    public void AddScore(int points)
    {
        Score += points;
        OnScoreChanged?.Invoke(Score);
        _consoleLogger?.Log($"Score updated: {Score}");
        _ = _saveSystem?.SaveAsync("score", Score);
    }

    public void LoseLife()
    {
        Lives--;
        _fileLogger?.Log($"Lives: {Lives}");
    }

    public void Reset()
    {
        Score = 0;
        Lives = 3;
    }
}

[Service(typeof(ISaveSystem))]
public class SaveSystem : ISaveSystem
{
    private readonly Dictionary<string, object> _data = new();

    public Task SaveAsync(string key, object data)
    {
        _data[key] = data;
        GD.Print($"[SaveSystem] Saved: {key}");
        return Task.CompletedTask;
    }

    public Task<T> LoadAsync<T>(string key)
    {
        var value = _data.TryGetValue(key, out var data) ? (T)data : default;
        return Task.FromResult(value);
    }
}

// === USAGE EXAMPLES ===
