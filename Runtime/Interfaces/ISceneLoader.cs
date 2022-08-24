/**
 * ISceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/16/2022 (en-US)
 */

namespace MyUnityTools.SceneLoading
{
    /// <summary>
    /// Interface to standardize scene operations.
    /// </summary>
    public interface ISceneLoader
    {
        /// <summary>
        /// Triggers a scene transition.
        /// The transition will move from scene A to scene B with an intermediate loading scene.
        /// </summary>
        /// <param name="sceneInfo">Target scene info. Can be the scene's build index or name.</param>
        void TransitionToScene(ILoadSceneInfo sceneInfo);

        /// <summary>
        /// Switches the current scene with the given scene.
        /// Unlike <see cref="TransitionToScene(ILoadSceneInfo)"/>, scene switching happens without any intermediate scene.
        /// </summary>
        /// <param name="sceneInfo">Target scene info. Can be the scene's build index or name.</param>
        void SwitchToScene(ILoadSceneInfo sceneInfo);

        /// <summary>
        /// Unloads a scene.
        /// </summary>
        /// <param name="sceneInfo">Target scene info. Can be the scene's build index or name.</param>
        void UnloadScene(ILoadSceneInfo sceneInfo);

        /// <summary>
        /// Loads a scene additively, on top of the current scene structure.
        /// </summary>
        /// <param name="sceneInfo">Target scene info. Can be the scene's build index or name.</param>
        void LoadScene(ILoadSceneInfo sceneInfo, bool setActive = false);
    }
}