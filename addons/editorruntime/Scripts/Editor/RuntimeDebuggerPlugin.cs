#if TOOLS
using Godot;
using Godot.Collections;

namespace Ascendere.EditorRuntime.Editor
{
    public partial class RuntimeDebuggerPlugin : EditorDebuggerPlugin
    {
        [Signal]
        public delegate void MessageReceivedEventHandler(string message, Array data, int sessionId);

        public override bool _HasCapture(string capture)
        {
            return capture == RuntimeConstants.ProtocolPrefix;
        }

        public override bool _Capture(string message, Array data, int sessionId)
        {
            if (message.StartsWith($"{RuntimeConstants.ProtocolPrefix}:"))
            {
                string cmd = message.Substring(RuntimeConstants.ProtocolPrefix.Length + 1);
                EmitSignal(SignalName.MessageReceived, cmd, data, sessionId);
                return true;
            }
            return false;
        }

        public void SendCommand(int sessionId, string command, Array data = null)
        {
            var session = GetSession(sessionId);
            if (session != null && session.IsActive())
            {
                session.SendMessage(
                    $"{RuntimeConstants.ProtocolPrefix}:{command}",
                    data ?? new Array()
                );
                GD.Print($"[RuntimeDebuggerPlugin] Sent to session {sessionId}: {command}");
            }
            else
            {
                GD.Print($"[RuntimeDebuggerPlugin] Session {sessionId} is null or inactive.");
            }
        }

        public void BroadcastCommand(string command, Array data = null)
        {
            var sessions = GetSessions();
            GD.Print(
                $"[RuntimeDebuggerPlugin] Broadcasting '{command}' to {sessions.Count} sessions."
            );
            foreach (var sessionVariant in sessions)
            {
                // In Godot 4, GetSessions returns an array of EditorDebuggerSession objects
                var session = sessionVariant.As<EditorDebuggerSession>();

                if (session != null)
                {
                    if (session.IsActive())
                    {
                        session.SendMessage(
                            $"{RuntimeConstants.ProtocolPrefix}:{command}",
                            data ?? new Array()
                        );
                        GD.Print($"[RuntimeDebuggerPlugin] Sent to session: {command}");
                    }
                }
                else
                {
                    GD.Print(
                        $"[RuntimeDebuggerPlugin] Session object is not EditorDebuggerSession: {sessionVariant.VariantType}"
                    );
                }
            }
        }

        /// <summary>
        /// Broadcasts a typed message to all active sessions.
        /// </summary>
        public void Broadcast<T>(T message)
            where T : RuntimeMessage
        {
            BroadcastCommand(message.Command, message.Serialize());
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
    }
}
#endif
