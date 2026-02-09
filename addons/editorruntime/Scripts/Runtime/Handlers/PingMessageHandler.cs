using Godot;

namespace Ascendere.EditorRuntime
{
    /// <summary>
    /// Built-in handler for ping messages. Automatically responds with pong.
    /// </summary>
    public class PingMessageHandler : IMessageHandler<PingMessage>
    {
        public void Handle(PingMessage message, RuntimeBridge bridge)
        {
            GD.Print($"[PingMessageHandler] Received: {message.Text}");
            bridge.Send(new PingMessage { Text = $"Pong: {message.Text}" });
        }
    }
}
