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
        [SerializeField]
        int _loadingIndex = 2;

        ILoadSceneInfo _loadingSceneInfo;
        ISceneLoader _sceneLoader;

        void Start()
        {
            _sceneLoader = new SceneLoaderUniTask();
            _loadingSceneInfo = new LoadSceneInfoIndex(_loadingIndex);
        }

        public void TransitionToSceneByIndex_Loading(int index) => _sceneLoader.TransitionToScene(new LoadSceneInfoIndex(index), _loadingSceneInfo);

        public void TransitionToSceneByName_Loading(string name) => _sceneLoader.TransitionToScene(new LoadSceneInfoName(name), _loadingSceneInfo);

        public void TransitionToSceneByIndex_Simple(int index) => _sceneLoader.TransitionToScene(new LoadSceneInfoIndex(index));

        public void TransitionToSceneByName_Simple(string name) => _sceneLoader.TransitionToScene(new LoadSceneInfoName(name));

        public void UnloadSceneByIndex(int index) => _sceneLoader.UnloadScene(new LoadSceneInfoIndex(index));

        public void UnloadSceneByName(string name) => _sceneLoader.UnloadScene(new LoadSceneInfoName(name));

        public void LoadSceneByIndex(int index) => _sceneLoader.LoadScene(new LoadSceneInfoIndex(index));

        public void LoadSceneByName(string name) => _sceneLoader.LoadScene(new LoadSceneInfoName(name));
    }
}