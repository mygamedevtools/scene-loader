/**
 * SceneManager.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-21
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace MyGameDevTools.SceneLoading
{
    public class SceneManager : ISceneManager
    {
        public event Action<Scene, Scene> ActiveSceneChanged;
        public event Action<Scene> SceneUnloaded;
        public event Action<Scene> SceneLoaded;

        public int SceneCount => _loadedScenes.Count;

        readonly List<Scene> _loadedScenes = new List<Scene>();

        Scene _activeScene;

        public void SetActiveScene(Scene scene)
        {
            var validScene = scene.IsValid();
            if (validScene && !_loadedScenes.Contains(scene))
                throw new InvalidOperationException($"Cannot set active the scene \"{scene.name}\" that has not been loaded through this {GetType().Name}.");

            var previousScene = _activeScene;
            _activeScene = scene;
            if (validScene)
                UnitySceneManager.SetActiveScene(scene);

            ActiveSceneChanged?.Invoke(previousScene, scene);
        }

        public Scene GetActiveScene() => _activeScene;

        public Scene GetLoadedSceneAt(int index) => _loadedScenes[index];

        public Scene GetLoadedSceneByName(string name)
        {
            foreach (var scene in _loadedScenes)
                if (scene.name == name)
                    return scene;
            throw new ArgumentException($"Could not find any loaded scene with the name '{name}'.", nameof(name));
        }

        public Scene GetLastLoadedScene()
        {
            if (SceneCount == 0)
                return default;

            return _loadedScenes[^1];
        }

        public async ValueTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null)
        {
            var operation = GetLoadSceneOperation(sceneInfo);
            Scene loadedScene = default;

            UnitySceneManager.sceneLoaded += registerLoadedScene;

            while (!operation.isDone)
            {
                await Task.Yield();
                progress?.Report(operation.progress);
            }

            UnitySceneManager.sceneLoaded -= registerLoadedScene;

            _loadedScenes.Add(loadedScene);
            SceneLoaded?.Invoke(loadedScene);

            if (setActive)
                SetActiveScene(loadedScene);

            return loadedScene;

            void registerLoadedScene(Scene scene, LoadSceneMode loadSceneMode)
            {
                if (sceneInfo.IsReferenceToScene(scene))
                    loadedScene = scene;
            }
        }

        public async ValueTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo)
        {
            var scene = GetLastLoadedSceneByInfo(sceneInfo);
            if (!_loadedScenes.Contains(scene))
                throw new InvalidOperationException($"Cannot unload the scene \"{scene.name}\" that has not been loaded through this {GetType().Name}.");

            var operation = GetUnloadSceneOperation(sceneInfo);
            while (!operation.isDone)
                await Task.Yield();

            _loadedScenes.Remove(scene);
            SceneUnloaded?.Invoke(scene);
            if (_activeScene == scene)
                SetActiveScene(GetLastLoadedScene());

            return scene;
        }

        AsyncOperation GetLoadSceneOperation(ILoadSceneInfo sceneInfo)
        {
            if (sceneInfo.Reference is int index)
                return UnitySceneManager.LoadSceneAsync(index, LoadSceneMode.Additive);
            else if (sceneInfo.Reference is string name)
                return UnitySceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            else if (sceneInfo.Reference is Scene scene)
                return UnitySceneManager.LoadSceneAsync(scene.buildIndex, LoadSceneMode.Additive);
            else
                throw new Exception($"Unexpected {nameof(ILoadSceneInfo.Reference)} type.");
        }

        AsyncOperation GetUnloadSceneOperation(ILoadSceneInfo sceneInfo)
        {
            if (sceneInfo.Reference is int index)
                return UnitySceneManager.UnloadSceneAsync(index);
            else if (sceneInfo.Reference is string name)
                return UnitySceneManager.UnloadSceneAsync(name);
            else if (sceneInfo.Reference is Scene scene)
                return UnitySceneManager.UnloadSceneAsync(scene);
            else
                throw new Exception($"Unexpected {nameof(ILoadSceneInfo.Reference)} type.");
        }

        Scene GetLastLoadedSceneByInfo(ILoadSceneInfo sceneInfo)
        {
            var loadedSceneCount = UnitySceneManager.loadedSceneCount;
            for (int i = loadedSceneCount - 1; i >= 0; i--)
            {
                var scene = UnitySceneManager.GetSceneAt(i);
                if (sceneInfo.IsReferenceToScene(scene))
                    return scene;
            }
            throw new ArgumentException($"Could not find any loaded scene with the provided ILoadSceneInfo: {sceneInfo.Reference}");
        }
    }
}