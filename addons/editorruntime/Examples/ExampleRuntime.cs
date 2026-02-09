using Ascendere.EditorRuntime;
using Godot;

namespace Ascendere.EditorRuntime.Examples
{
    public partial class ExampleRuntime : Control
    {
        private Button _sendButton;
        private LineEdit _messageInput;
        private RichTextLabel _messageLog;

        public override void _Ready()
        {
            _sendButton = GetNode<Button>("VBoxContainer/SendButton");
            _messageInput = GetNode<LineEdit>("VBoxContainer/MessageInput");
            _messageLog = GetNode<RichTextLabel>("VBoxContainer/MessageLog");

            _sendButton.Connect(
                Button.SignalName.Pressed,
                new Callable(this, MethodName.OnSendPressed)
            );

            // Connect to the bridge signal to receive messages from editor
            if (RuntimeBridge.Instance != null)
            {
                RuntimeBridge.Instance.Connect(
                    RuntimeBridge.SignalName.MessageReceived,
                    new Callable(this, MethodName.OnBridgeMessageReceived)
                );
            }
        }

        public override void _ExitTree()
        {
            if (RuntimeBridge.Instance != null)
            {
                if (
                    RuntimeBridge.Instance.IsConnected(
                        RuntimeBridge.SignalName.MessageReceived,
                        new Callable(this, MethodName.OnBridgeMessageReceived)
                    )
                )
                {
                    RuntimeBridge.Instance.Disconnect(
                        RuntimeBridge.SignalName.MessageReceived,
                        new Callable(this, MethodName.OnBridgeMessageReceived)
                    );
                }
            }
        }

        private void OnSendPressed()
        {
            string msg = _messageInput.Text;
            if (!string.IsNullOrEmpty(msg))
            {
                // Send message to editor
                RuntimeBridge.Instance.Send(new TextMessage(msg));
                _messageLog.AddText($"Sent: {msg}\n");
                _messageInput.Text = "";
            }
        }

        private void OnBridgeMessageReceived(string command, Godot.Collections.Array data)
        {
            if (RuntimeBridge.Instance.TryDeserialize(command, data, out PingMessage pingMsg))
            {
                _messageLog.AddText($"Received from Editor: Ping - {pingMsg.Text}\n");
                // Auto-reply to ping
                RuntimeBridge.Instance.Send(new PingMessage { Text = "Pong from Game" });
            }
            else if (RuntimeBridge.Instance.TryDeserialize(command, data, out TextMessage textMsg))
            {
                _messageLog.AddText($"Received from Editor: Message - {textMsg.Text}\n");
            }
            else
            {
                string dataStr = data != null ? data.ToString() : "null";
                _messageLog.AddText($"Received from Editor: {command} - {dataStr}\n");
            }
            GD.Print($"[ExampleRuntime] Received: {command}");
        }
    }
}
