using Godot;

namespace Ascendere.EditorRuntime
{
    /// <summary>
    /// Built-in handler for text messages. Forwards content as command.
    /// </summary>
    public class TextMessageHandler : IMessageHandler<TextMessage>
    {
        public void Handle(TextMessage message, RuntimeBridge bridge)
        {
            GD.Print($"[TextMessageHandler] Received: {message.Text} - forwarding as command");
            // Forward the text content as the actual command
            bridge.ForwardAsCommand(message.Text);
        }
    }
}
