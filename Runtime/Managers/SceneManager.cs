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
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);

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
            while (!operation.isDone)
            {
                await Task.Yield();
                progress?.Report(operation.progress);
            }

            var scene = GetSceneByInfo(sceneInfo);
            _loadedScenes.Add(scene);
            SceneLoaded?.Invoke(scene);

            if (setActive)
                SetActiveScene(scene);

            return scene;
        }

        public async ValueTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo)
        {
            var scene = GetSceneByInfo(sceneInfo);
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
                return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(index, LoadSceneMode.Additive);
            else if (sceneInfo.Reference is string name)
                return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            else
                throw new Exception($"Unexpected {nameof(ILoadSceneInfo.Reference)} type.");
        }

        AsyncOperation GetUnloadSceneOperation(ILoadSceneInfo sceneInfo)
        {
            if (sceneInfo.Reference is int index)
                return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(index);
            else if (sceneInfo.Reference is string name)
                return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(name);
            else
                throw new Exception($"Unexpected {nameof(ILoadSceneInfo.Reference)} type.");
        }

        Scene GetSceneByInfo(ILoadSceneInfo sceneInfo)
        {
            if (sceneInfo.Reference is int index)
                return UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(index);
            else if (sceneInfo.Reference is string name)
                return UnityEngine.SceneManagement.SceneManager.GetSceneByName(name);
            else
                throw new Exception($"Unexpected {nameof(ILoadSceneInfo.Reference)} type.");
        }
    }
}