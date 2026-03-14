namespace Ascendere.Events;

public interface IEventit
{
    string Name { get; }
}

public struct PlayerJoinedEvent : IEventit
{
    public int PlayerId;
    public string PlayerName;

    public string Name => "PlayerJoinedEvent";
}
