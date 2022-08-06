/**
 * SceneLoaderWrapper.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/16/2022 (en-US)
 */

using MyUnityTools.SceneLoading.UniTaskSupport;
using UnityEngine;

namespace MyUnityTools.SceneLoading.Samples
{
    public class SceneLoaderWrapper : MonoBehaviour
    {
        ISceneLoader _sceneLoader;

        void Start() => _sceneLoader = new UniTaskSceneLoader(2);

        public void TransitionToSceneByIndex(int index) => _sceneLoader.TransitionToScene(index);

        public void TransitionToSceneByName(string name) => _sceneLoader.TransitionToScene(name);

        public void SwitchToSceneByIndex(int index) => _sceneLoader.SwitchToScene(index);

        public void SwitchToSceneByName(string name) => _sceneLoader.SwitchToScene(name);

        public void UnloadSceneByIndex(int index) => _sceneLoader.UnloadScene(index);

        public void UnloadSceneByName(string name) => _sceneLoader.UnloadScene(name);

        public void LoadSceneByIndex(int index) => _sceneLoader.LoadScene(index);

        public void LoadSceneByName(string name) => _sceneLoader.LoadScene(name);
    }
}