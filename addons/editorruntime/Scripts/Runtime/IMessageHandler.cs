namespace Ascendere.EditorRuntime
{
    /// <summary>
    /// Interface for handling specific message types from the editor.
    /// </summary>
    public interface IMessageHandler<T>
        where T : RuntimeMessage, new()
    {
        /// <summary>
        /// Handles the deserialized message.
        /// </summary>
        /// <param name="message">The typed message</param>
        /// <param name="bridge">Reference to RuntimeBridge for sending responses</param>
        void Handle(T message, RuntimeBridge bridge);
    }
}
