using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using GodotArray = Godot.Collections.Array;

namespace Ascendere.EditorRuntime
{
    /// <summary>
    /// Registry for message handlers. Supports automatic discovery via reflection
    /// and manual registration of custom handlers.
    /// </summary>
    public class MessageHandlerRegistry
    {
        private readonly Dictionary<string, IHandlerWrapper> _handlers = new();

        /// <summary>
        /// Registers a handler for a specific message type.
        /// </summary>
        public void Register<T>(IMessageHandler<T> handler)
            where T : RuntimeMessage, new()
        {
            var messageType = new T();
            var command = messageType.Command;

            if (_handlers.ContainsKey(command))
            {
                GD.PushWarning(
                    $"[MessageHandlerRegistry] Overwriting existing handler for command: {command}"
                );
            }

            _handlers[command] = new HandlerWrapper<T>(handler);
            GD.Print($"[MessageHandlerRegistry] Registered handler for command: {command}");
        }

        /// <summary>
        /// Automatically discovers and registers all built-in handlers using reflection.
        /// </summary>
        public void RegisterBuiltInHandlers()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var handlerType = typeof(IMessageHandler<>);

            var types = assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t =>
                    t.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType)
                );

            foreach (var type in types)
            {
                try
                {
                    var instance = Activator.CreateInstance(type);
                    var interfaceType = type.GetInterfaces()
                        .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType);
                    var messageType = interfaceType.GetGenericArguments()[0];

                    var registerMethod = GetType()
                        .GetMethod(
                            nameof(RegisterDynamic),
                            BindingFlags.NonPublic | BindingFlags.Instance
                        );
                    var genericRegister = registerMethod.MakeGenericMethod(messageType);
                    genericRegister.Invoke(this, new[] { instance });
                }
                catch (Exception ex)
                {
                    GD.PrintErr(
                        $"[MessageHandlerRegistry] Failed to register handler {type.Name}: {ex.Message}"
                    );
                }
            }
        }

        private void RegisterDynamic<T>(object handler)
            where T : RuntimeMessage, new()
        {
            Register((IMessageHandler<T>)handler);
        }

        /// <summary>
        /// Attempts to process a message using registered handlers.
        /// </summary>
        /// <returns>True if a handler was found and executed, false otherwise</returns>
        public bool TryHandle(string command, GodotArray data, RuntimeBridge bridge)
        {
            if (_handlers.TryGetValue(command, out var wrapper))
            {
                return wrapper.TryHandle(command, data, bridge);
            }
            return false;
        }

        private interface IHandlerWrapper
        {
            bool TryHandle(string command, GodotArray data, RuntimeBridge bridge);
        }

        private class HandlerWrapper<T> : IHandlerWrapper
            where T : RuntimeMessage, new()
        {
            private readonly IMessageHandler<T> _handler;

            public HandlerWrapper(IMessageHandler<T> handler)
            {
                _handler = handler;
            }

            public bool TryHandle(string command, GodotArray data, RuntimeBridge bridge)
            {
                var message = new T();
                if (command == message.Command)
                {
                    try
                    {
                        message.Deserialize(data);
                        _handler.Handle(message, bridge);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr(
                            $"[MessageHandlerRegistry] Error handling {command}: {ex.Message}"
                        );
                        return false;
                    }
                }
                return false;
            }
        }
    }
}
