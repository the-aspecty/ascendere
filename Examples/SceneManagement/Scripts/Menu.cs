using System;
using Godot;

#nullable enable

namespace Examples.SceneManagement
{
    public partial class Menu : GameScene
    {
        [NodeReference("StartButton")]
        private Button _startButton = null!;

        [NodeReference("StatusLabel")]
        private Label _statusLabel = null!;

        public override void _Ready()
        {
            ServiceLocator.InjectMembers(this);

            // Subscribe to scene change event via injected ISceneManager
            if (SceneManager != null)
            {
                SceneManager.OnSceneChanged += OnSceneChanged;
            }
            else
            {
                GD.PrintErr("[Menu] ISceneManager not injected");
            }

            // Validate node reference before connecting
            if (NodeReferenceProcessor.Instance.ValidateNodeReference(_startButton))
            {
                _startButton.Connect(
                    Button.SignalName.Pressed,
                    new Callable(this, MethodName.OnStartPressed)
                );
            }
        }

        private void OnStartPressed()
        {
            // Use GameScene flow to proceed to the Game scene
            if (!ProceedToNext())
            {
                GD.PrintErr("[Menu] Cannot proceed to Game - validation failed or path missing");
            }
        }

        protected override Type? GetNextSceneType()
        {
            return typeof(Game);
        }

        protected override string? GetScenePathForType(Type sceneType)
        {
            if (
                sceneType.Namespace != null
                && sceneType.Namespace.StartsWith("Examples.SceneManagement")
            )
                return $"res://Examples/SceneManagement/scenes/{sceneType.Name.ToLowerInvariant()}.tscn";

            return base.GetScenePathForType(sceneType);
        }

        private void OnSceneChanged(string scenePath)
        {
            _statusLabel.Text = $"Status: {scenePath}";
        }

        public override void _ExitTree()
        {
            // Unsubscribe from scene manager events
            if (SceneManager != null)
                SceneManager.OnSceneChanged -= OnSceneChanged;

            // Disconnect button signal
            if (_startButton != null && GodotObject.IsInstanceValid(_startButton))
            {
                var callable = new Callable(this, MethodName.OnStartPressed);
                if (_startButton.IsConnected(Button.SignalName.Pressed, callable))
                {
                    _startButton.Disconnect(Button.SignalName.Pressed, callable);
                }
            }

            // Clean up node references
            NodeReferenceProcessor.Instance.CleanupNodeReferences(this);

            base._ExitTree();
        }
    }
}
