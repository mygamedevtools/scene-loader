/**
 * ISceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/16/2022 (en-US)
 */

using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Interface to standardize scene loading operations.
    /// </summary>
    public interface ISceneLoader
    {
        /// <summary>
        /// Reference to the <see cref="ISceneManager"/>, responsible for performing the scene loading operations.
        /// You can retrieve the manager to listen to the <see cref="ISceneManager.SceneLoaded"/>, <see cref="ISceneManager.SceneUnloaded"/> and <see cref="ISceneManager.ActiveSceneChanged"/> events.
        /// </summary>
        ISceneManager Manager { get; }

        /// <summary>
        /// Triggers a scene transition.
        /// It will transition from the current active scene (<see cref="ISceneManager.GetActiveScene()"/>)
        /// to the target scene (<paramref name="targetSceneInfo"/>), with an optional intermediate loading scene (<paramref name="intermediateSceneInfo"/>).
        /// If the <paramref name="intermediateSceneInfo"/> is not set, the transition will have no intermediate loading scene and will instead simply load the target scene directly.
        /// The complete transition flow is:
        /// <br/><br/>
        /// 1. Load the intermediate scene (if provided).<br/>
        /// 2. Unload the previous scene. <br/>
        /// 3. Load the target scene.<br/>
        /// 4. Unload the intermediate scene (if provided).<br/>
        /// </summary>
        /// <param name="targetSceneInfo">
        /// A reference to the scene that's going to be transitioned to.
        /// </param>
        /// <param name="intermediateSceneInfo">
        /// A reference to the scene that's going to be loaded as the transition intermediate (as a loading scene).
        /// If null, the transition will not have an intermediate loading scene.
        /// </param>
        void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null, Scene externalOriginScene = default);

        /// <summary>
        /// Unloads the given scene from the current scene stack.
        /// </summary>
        /// <param name="sceneInfo">
        /// Reference to the desired scene to be unloaded.
        /// </param>
        void UnloadScene(ILoadSceneInfo sceneInfo);

        /// <summary>
        /// Loads a scene additively on top of the current scene stack, optionally marking it as the active scene
        /// (<see cref="ISceneManager.SetActiveScene(UnityEngine.SceneManagement.Scene)"/>).
        /// </summary>
        /// <param name="sceneInfo">
        /// Reference to the scene that's going to be loaded.
        /// </param>
        /// <param name="setActive">Should the loaded scene be marked as active? Equivalent to calling <see cref="ISceneManager.SetActiveScene(UnityEngine.SceneManagement.Scene)"/>.</param>
        void LoadScene(ILoadSceneInfo sceneInfo, bool setActive = false);
    }
}