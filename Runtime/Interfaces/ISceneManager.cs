/**
 * ISceneManager.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-21
 */

using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public interface ISceneManager
    {
        event Action<Scene, Scene> ActiveSceneChanged;
        event Action<Scene> SceneUnloaded;
        event Action<Scene> SceneLoaded;

        int SceneCount { get; }

        void SetActiveScene(Scene scene);

        ValueTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null);

        ValueTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo);

        Scene GetActiveScene();

        Scene GetLoadedSceneAt(int index);

        Scene GetLastLoadedScene();

        Scene GetLoadedSceneByName(string name);
    }
}