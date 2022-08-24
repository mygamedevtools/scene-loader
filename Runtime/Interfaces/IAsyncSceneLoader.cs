/**
 * IAsyncSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/15/2022 (en-US)
 */

using System.Threading.Tasks;

namespace MyUnityTools.SceneLoading
{
    /// <summary>
    /// Interface to standardize async scene operations.
    /// </summary>
    public interface IAsyncSceneLoader : ISceneLoader
    {
        /// <summary>
        /// Triggers an async scene transition.
        /// The transition will move from scene A to scene B with an intermediate loading scene.
        /// </summary>
        /// <param name="sceneInfo">Target scene info. Can be the scene's build index or name.</param>
        /// <returns>The transition <see cref="Task"/>.</returns>
        Task TransitionToSceneAsync(ILoadSceneInfo sceneInfo);

        /// <summary>
        /// Switches the current scene with the given scene asynchronously.
        /// Unlike <see cref="TransitionToSceneAsync(ILoadSceneInfo)"/>, scene switching happens without any intermediate scene.
        /// </summary>
        /// <param name="sceneInfo">Target scene info. Can be the scene's build index or name.</param>
        /// <returns>The switch <see cref="Task"/>.</returns>
        Task SwitchToSceneAsync(ILoadSceneInfo sceneInfo);

        /// <summary>
        /// Unloads a scene asynchronously.
        /// </summary>
        /// <param name="sceneInfo">Target scene info. Can be the scene's build index or name.</param>
        /// <returns>The unload <see cref="Task"/>.</returns>
        Task UnloadSceneAsync(ILoadSceneInfo sceneInfo);

        /// <summary>
        /// Loads a scene asyncrhonously and additively, on top of the current scene structure.
        /// </summary>
        /// <param name="sceneInfo">Target scene info. Can be the scene's build index or name.</param>
        /// <returns>The load <see cref="UniTask"/>.</returns>
        Task LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false);
    }
}