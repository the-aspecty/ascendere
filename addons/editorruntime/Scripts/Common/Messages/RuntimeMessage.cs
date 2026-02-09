using Godot.Collections;

namespace Ascendere.EditorRuntime
{
    /// <summary>
    /// Base class for all typed messages exchanged between Editor and Runtime.
    /// </summary>
    public abstract class RuntimeMessage
    {
        /// <summary>
        /// The unique command string identifier for this message type.
        /// </summary>
        public abstract string Command { get; }

        /// <summary>
        /// Serializes the message data into a Godot Array.
        /// </summary>
        public abstract Array Serialize();

        /// <summary>
        /// Deserializes the message data from a Godot Array.
        /// </summary>
        public abstract void Deserialize(Array data);
    }
}
