using System;
using System.Threading.Tasks;
using Ascendere.SceneManagement;
using Godot;

#nullable enable

namespace Examples.SceneManagement
{
    public partial class Game : GameScene
    {
        [NodeReference("BackButton")]
        private Button _backButton = null!;

        public override void _Ready()
        {
            // Injection handled by GameScene._Ready()
            base._Ready();

            if (SceneManager == null)
                GD.PrintErr("[Game] ISceneManager not injected");

            // Validate node reference before connecting
            if (NodeReferenceProcessor.Instance.ValidateNodeReference(_backButton))
            {
                _backButton.Connect(
                    Button.SignalName.Pressed,
                    new Callable(this, MethodName.OnBackPressed)
                );
            }
        }

        private async void OnBackPressed()
        {
            // Prevent interaction during scene change
            if (_backButton != null && GodotObject.IsInstanceValid(_backButton))
            {
                _backButton.Disabled = true;
            }

            // Check if we're still in the tree before async operation
            if (!IsInsideTree())
                return;

            if (SceneManager != null && SceneManager.GetHistoryCount() > 0)
            {
                await SceneManager.GoBackAsync(true);
            }
            else if (SceneManager != null)
            {
                // Fallback to menu if no history
                await SceneManager.ChangeSceneAsync(
                    "res://Examples/SceneManagement/scenes/menu.tscn"
                );
            }
        }

        protected override Type? GetNextSceneType()
        {
            return typeof(Menu);
        }

        protected override string? GetScenePathForType(Type sceneType)
        {
            if (sceneType.Namespace?.StartsWith("Examples.SceneManagement") == true)
                return $"res://Examples/SceneManagement/scenes/{sceneType.Name.ToLowerInvariant()}.tscn";

            return base.GetScenePathForType(sceneType);
        }

        public override void _ExitTree()
        {
            // Disconnect button signal
            if (_backButton != null && GodotObject.IsInstanceValid(_backButton))
            {
                var callable = new Callable(this, MethodName.OnBackPressed);
                if (_backButton.IsConnected(Button.SignalName.Pressed, callable))
                {
                    _backButton.Disconnect(Button.SignalName.Pressed, callable);
                }
            }

            // Clean up node references
            NodeReferenceProcessor.Instance.CleanupNodeReferences(this);

            base._ExitTree();
        }
    }
}
