using System;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Interface to standardize scene loading operations.
    /// </summary>
    public interface ISceneLoader : IDisposable
    {
        /// <summary>
        /// Reference to the <see cref="ISceneManager"/>, responsible for performing the scene loading operations.
        /// You can retrieve the manager to listen to the <see cref="ISceneManager.SceneLoaded"/>, <see cref="ISceneManager.SceneUnloaded"/> and <see cref="ISceneManager.ActiveSceneChanged"/> events.
        /// </summary>
        ISceneManager Manager { get; }

        /// <summary>
        /// Triggers a transition to a group of scenes from the active scene.
        /// It will transition from the current active scene (<see cref="ISceneManager.GetActiveScene()"/>)
        /// to a group of scenes (<paramref name="targetScenes"/>), with an optional intermediate loading scene (<paramref name="intermediateSceneInfo"/>).
        /// If the <paramref name="intermediateSceneInfo"/> is not set, the transition will have no intermediate loading scene and will instead simply load the target scene directly.
        /// The complete transition flow is:
        /// <br/><br/>
        /// 1. Load the intermediate scene (if provided).<br/>
        /// 2. Unload the source scene (if any).<br/>
        /// 3. Load all target scenes.<br/>
        /// 4. Unload the intermediate scene (if provided).<br/>
        /// </summary>
        /// <param name="targetScenes">
        /// A reference to all scenes that will be transitioned to.
        /// </param>
        /// <param name="setIndexActive">
        /// Index of the scene in the <paramref name="targetScenes"/> to be set as the active scene.
        /// </param>
        /// <param name="intermediateSceneInfo">
        /// A reference to the scene that's going to be loaded as the transition intermediate (as a loading scene).
        /// If null, the transition will not have an intermediate loading scene.
        /// </param>
        void TransitionToScenes(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null);

        /// <summary>
        /// Triggers a transition to the target scene from the active scene.
        /// It will transition from the current active scene (<see cref="ISceneManager.GetActiveScene()"/>)
        /// to the target scene (<paramref name="targetSceneInfo"/>), with an optional intermediate loading scene (<paramref name="intermediateSceneInfo"/>).
        /// If the <paramref name="intermediateSceneInfo"/> is not set, the transition will have no intermediate loading scene and will instead simply load the target scene directly.
        /// The complete transition flow is:
        /// <br/><br/>
        /// 1. Load the intermediate scene (if provided).<br/>
        /// 2. Unload the source scene (if any).<br/>
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
        void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null);

        /// <summary>
        /// Triggers a transition to a group of scenes from another group of scenes.
        /// It will transition from the provided group of scenes (<paramref name="fromScenes"/>)
        /// to a group of scenes (<paramref name="targetScenes"/>), with an optional intermediate loading scene (<paramref name="intermediateSceneInfo"/>).
        /// If the <paramref name="intermediateSceneInfo"/> is not set, the transition will have no intermediate loading scene and will instead simply load the target scene directly.
        /// The complete transition flow is:
        /// <br/><br/>
        /// 1. Load the intermediate scene (if provided).<br/>
        /// 2. Unload all provided scenes.<br/>
        /// 3. Load all target scenes.<br/>
        /// 4. Unload the intermediate scene (if provided).<br/>
        /// </summary>
        /// <param name="targetScenes">
        /// A reference to all scenes that will be transitioned to.
        /// </param>
        /// <param name="fromScenes">
        /// A reference to all scenes that will be unloaded in the transition.
        /// </param>
        /// <param name="setIndexActive">
        /// Index of the scene in the <paramref name="targetScenes"/> to be set as the active scene.
        /// </param>
        /// <param name="intermediateSceneInfo">
        /// A reference to the scene that's going to be loaded as the transition intermediate (as a loading scene).
        /// If null, the transition will not have an intermediate loading scene.
        /// </param>
        void TransitionToScenesFromScenes(ILoadSceneInfo[] targetScenes, ILoadSceneInfo[] fromScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null);

        /// <summary>
        /// Triggers a transition to the target scene from a group of scens.
        /// It will transition from the provided group of scenes (<paramref name="fromScenes"/>)
        /// to the target scene (<paramref name="targetSceneInfo"/>), with an optional intermediate loading scene (<paramref name="intermediateSceneInfo"/>).
        /// If the <paramref name="intermediateSceneInfo"/> is not set, the transition will have no intermediate loading scene and will instead simply load the target scene directly.
        /// The complete transition flow is:
        /// <br/><br/>
        /// 1. Load the intermediate scene (if provided).<br/>
        /// 2. Unload all provided scenes.<br/>
        /// 3. Load the target scene.<br/>
        /// 4. Unload the intermediate scene (if provided).<br/>
        /// </summary>
        /// <param name="targetSceneInfo">
        /// A reference to the scene that's going to be transitioned to.
        /// </param>
        /// <param name="fromScenes">
        /// A reference to all scenes that will be unloaded in the transition.
        /// </param>
        /// <param name="intermediateSceneInfo">
        /// A reference to the scene that's going to be loaded as the transition intermediate (as a loading scene).
        /// If null, the transition will not have an intermediate loading scene.
        /// </param>
        void TransitionToSceneFromScenes(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo[] fromScenes, ILoadSceneInfo intermediateSceneInfo = null);

        /// <summary>
        /// Triggers a transition to a group of scenes from all loaded scenes.
        /// It will transition from all loaded scenes
        /// to a group of scenes (<paramref name="targetScenes"/>), with an optional intermediate loading scene (<paramref name="intermediateSceneInfo"/>).
        /// If the <paramref name="intermediateSceneInfo"/> is not set, the transition will have no intermediate loading scene and will instead simply load the target scene directly.
        /// The complete transition flow is:
        /// <br/><br/>
        /// 1. Load the intermediate scene (if provided).<br/>
        /// 2. Unload all loaded scenes.<br/>
        /// 3. Load all target scenes.<br/>
        /// 4. Unload the intermediate scene (if provided).<br/>
        /// </summary>
        /// <param name="targetScenes">
        /// A reference to all scenes that will be transitioned to.
        /// </param>
        /// <param name="setIndexActive">
        /// Index of the scene in the <paramref name="targetScenes"/> to be set as the active scene.
        /// </param>
        /// <param name="intermediateSceneInfo">
        /// A reference to the scene that's going to be loaded as the transition intermediate (as a loading scene).
        /// If null, the transition will not have an intermediate loading scene.
        /// </param>
        void TransitionToScenesFromAll(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null);

        /// <summary>
        /// Triggers a transition to the target scene from all loaded scenes.
        /// It will transition from the provided group of scenes (<paramref name="fromScenes"/>)
        /// to the target scene (<paramref name="targetSceneInfo"/>), with an optional intermediate loading scene (<paramref name="intermediateSceneInfo"/>).
        /// If the <paramref name="intermediateSceneInfo"/> is not set, the transition will have no intermediate loading scene and will instead simply load the target scene directly.
        /// The complete transition flow is:
        /// <br/><br/>
        /// 1. Load the intermediate scene (if provided).<br/>
        /// 2. Unload all provided scenes.<br/>
        /// 3. Load the target scene.<br/>
        /// 4. Unload the intermediate scene (if provided).<br/>
        /// </summary>
        /// <param name="targetSceneInfo">
        /// A reference to the scene that's going to be transitioned to.
        /// </param>
        /// <param name="fromScenes">
        /// A reference to all scenes that will be unloaded in the transition.
        /// </param>
        /// <param name="intermediateSceneInfo">
        /// A reference to the scene that's going to be loaded as the transition intermediate (as a loading scene).
        /// If null, the transition will not have an intermediate loading scene.
        /// </param>
        void TransitionToSceneFromAll(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null);

        /// <summary>
        /// Unloads all the scenes from the current scene stack.
        /// </summary>
        /// <param name="sceneInfos">
        /// Reference to all scenes that will be unloaded.
        /// </param>
        void UnloadScenes(ILoadSceneInfo[] sceneInfos);

        /// <summary>
        /// Unloads the given scene from the current scene stack.
        /// </summary>
        /// <param name="sceneInfo">
        /// Reference to the desired scene to be unloaded.
        /// </param>
        void UnloadScene(ILoadSceneInfo sceneInfo);

        /// <summary>
        /// Loads all scenes additively on top of the current scene stack, optionally marking one of them as the active scene.
        /// </summary>
        /// <param name="sceneInfos">
        /// Reference to all scenes that will be loaded.
        /// </param>
        /// <param name="setIndexActive">
        /// Which of the scenes should be marked as active? Default is -1.
        /// </param>
        void LoadScenes(ILoadSceneInfo[] sceneInfos, int setIndexActive = -1);

        /// <summary>
        /// Loads a scene additively on top of the current scene stack, optionally marking it as the active scene
        /// (<see cref="ISceneManager.SetActiveScene(Scene)"/>).
        /// </summary>
        /// <param name="sceneInfo">
        /// Reference to the scene that's going to be loaded.
        /// </param>
        /// <param name="setActive">Should the loaded scene be marked as active? Equivalent to calling <see cref="ISceneManager.SetActiveScene(Scene)"/>.</param>
        void LoadScene(ILoadSceneInfo sceneInfo, bool setActive = false);
    }
}