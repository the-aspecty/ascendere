using Godot;

public partial class PlayerServiceExample : CharacterBody2D
{
    [Inject]
    private IAudioService _audio;

    [Inject]
    private IGameState _gameState;

    [Inject(name: "ConsoleLogger")]
    private ILogger _logger;

    private int _score => _gameState.Score;

    public override void _Ready()
    {
        ServiceLocator.InjectMembers(this);
        _logger?.Log("Player initialized, coins: " + _score);
        CollectCoin();
        _logger?.Log("Score after collecting coin: " + _score);
    }

    public void CollectCoin()
    {
        _gameState?.AddScore(10);
    }

    [PostInject]
    private void AfterInjection()
    {
        GD.Print("Post-injection method called in Player.");
    }
}
