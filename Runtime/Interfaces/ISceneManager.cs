/**
 * ISceneManager.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-21
 */

using System;
using System.Threading.Tasks;

namespace MyGameDevTools.SceneLoading
{
    public interface ISceneManager<TScene, TInfo>
    {
        event Action<TScene, TScene> ActiveSceneChanged;
        event Action<TScene> SceneUnloaded;
        event Action<TScene> SceneLoaded;

        int SceneCount { get; }

        void SetActiveScene(TScene scene);

        ValueTask<TScene> LoadSceneAsync(TInfo sceneInfo, bool setActive = false, IProgress<float> progress = null);

        ValueTask UnloadSceneAsync(TInfo sceneInfo);

        TScene GetActiveScene();

        TScene GetLoadedSceneAt(int index);

        TScene GetLastLoadedScene();

        TScene GetLoadedSceneByName(string name);
    }
}