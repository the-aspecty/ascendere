using System;
using System.Threading.Tasks;
using Godot;

namespace Ascendere.SceneManagement
{
    /// <summary>
    /// Interface describing the public surface of a scene manager.
    /// Implement this on classes that provide scene-changing, preloading,
    /// history and transition services in the project.
    /// </summary>
    public interface ISceneManager
    {
        /// <summary>
        /// Fired when the active scene changes.
        /// </summary>
        event Action<string> OnSceneChanged;

        /// <summary>
        /// Fired while a scene is loading to report progress (0..1).
        /// </summary>
        event Action<string, float> OnSceneLoading;

        /// <summary>
        /// Default duration (seconds) used for fade transitions.
        /// </summary>
        float DefaultTransitionDuration { get; set; }

        /// <summary>
        /// Whether scene history (GoBack) is recorded.
        /// </summary>
        bool EnableHistory { get; set; }

        /// <summary>
        /// Maximum number of entries kept in history.
        /// </summary>
        int MaxHistorySize { get; set; }

        /// <summary>
        /// Change to a new scene. Await to ensure transitions complete.
        /// </summary>
        /// <param name="scenePath">Full resource path (res://...)</param>
        /// <param name="useTransition">Whether to use fade transitions</param>
        /// <param name="transitionDuration">Optional custom duration in seconds</param>
        Task ChangeSceneAsync(string scenePath, bool useTransition, float? transitionDuration);

        /// <summary>
        /// Change to a new scene without specifying transition details.
        /// </summary>
        Task ChangeSceneAsync(string scenePath);

        /// <summary>
        /// Change scene with an optional loading screen (for large scenes).
        /// </summary>
        Task ChangeSceneWithLoadingAsync(string scenePath, PackedScene loadingScene);

        /// <summary>
        /// Go back to the previous scene in history.
        /// </summary>
        Task GoBackAsync(bool useTransition);

        /// <summary>
        /// Preload a scene into memory for faster later loading.
        /// </summary>
        void PreloadScene(string scenePath);

        /// <summary>
        /// Unload a previously preloaded scene to free memory.
        /// </summary>
        void UnloadScene(string scenePath);

        /// <summary>
        /// Reload the currently active scene.
        /// </summary>
        Task ReloadSceneAsync(bool useTransition);

        /// <summary>
        /// Clear the recorded scene history.
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// Returns the count of recorded history entries.
        /// </summary>
        int GetHistoryCount();
    }
}
