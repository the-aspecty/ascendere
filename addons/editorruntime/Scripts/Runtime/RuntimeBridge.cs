using Godot;
using Godot.Collections;

namespace Ascendere.EditorRuntime
{
    public partial class RuntimeBridge : Node
    {
        public static RuntimeBridge Instance { get; private set; }

        private MessageHandlerRegistry _handlerRegistry;

        [Signal]
        public delegate void MessageReceivedEventHandler(string command, Array data);

        public override void _EnterTree()
        {
            Instance = this;

            // Initialize handler registry and register built-in handlers
            _handlerRegistry = new MessageHandlerRegistry();
            _handlerRegistry.RegisterBuiltInHandlers();

            if (OS.IsDebugBuild())
            {
                EngineDebugger.RegisterMessageCapture(
                    RuntimeConstants.ProtocolPrefix,
                    new Callable(this, MethodName.OnMessageCaptured)
                );
            }
        }

        public override void _ExitTree()
        {
            if (OS.IsDebugBuild())
            {
                EngineDebugger.UnregisterMessageCapture(RuntimeConstants.ProtocolPrefix);
            }
            if (Instance == this)
                Instance = null;
        }

        private bool OnMessageCaptured(string message, Array data)
        {
            GD.Print($"[RuntimeBridge] Captured: '{message}'");

            // Handle message if it starts with prefix (standard behavior)
            if (message.StartsWith($"{RuntimeConstants.ProtocolPrefix}:"))
            {
                string cmd = message.Substring(RuntimeConstants.ProtocolPrefix.Length + 1);
                ProcessCommand(cmd, data);
                return true;
            }
            // Handle message if it is just the command (if Godot strips prefix)
            else
            {
                ProcessCommand(message, data);
                return true;
            }
        }

        private void ProcessCommand(string cmd, Array data)
        {
            GD.Print(
                $"[RuntimeBridge] Processing command: '{cmd}' with data count: {data?.Count ?? 0}"
            );

            // Try to handle using registered handlers
            if (!_handlerRegistry.TryHandle(cmd, data, this))
            {
                // No handler found, emit signal for manual handling
                GD.Print($"[RuntimeBridge] No handler registered for '{cmd}', emitting signal");
                CallDeferred(MethodName.EmitMessageSignal, cmd, data);
            }
        }

        private void EmitMessageSignal(string command, Array data)
        {
            EmitSignal(SignalName.MessageReceived, command, data);
        }

        public void SendToEditor(string command, Array data = null)
        {
            if (OS.IsDebugBuild())
            {
                EngineDebugger.SendMessage(
                    $"{RuntimeConstants.ProtocolPrefix}:{command}",
                    data ?? new Array()
                );
            }
        }

        /// <summary>
        /// Sends a typed message to the Editor.
        /// </summary>
        public void Send<T>(T message)
            where T : RuntimeMessage
        {
            SendToEditor(message.Command, message.Serialize());
        }

        /// <summary>
        /// Tries to deserialize a received command into a typed message.
        /// </summary>
        public bool TryDeserialize<T>(string command, Array data, out T message)
            where T : RuntimeMessage, new()
        {
            message = new T();
            if (command == message.Command)
            {
                message.Deserialize(data);
                return true;
            }
            message = null;
            return false;
        }

        /// <summary>
        /// Registers a custom message handler. Call this from your game code to handle custom commands.
        /// Example: RuntimeBridge.Instance.RegisterHandler(new MyCustomHandler());
        /// </summary>
        public void RegisterHandler<T>(IMessageHandler<T> handler)
            where T : RuntimeMessage, new()
        {
            _handlerRegistry?.Register(handler);
        }

        /// <summary>
        /// Internal method used by TextMessageHandler to forward text content as command.
        /// </summary>
        internal void ForwardAsCommand(string command)
        {
            CallDeferred(MethodName.EmitMessageSignal, command, new Array());
        }
    }
}
