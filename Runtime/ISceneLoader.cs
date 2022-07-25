/**
 * ISceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/16/2022 (en-US)
 */

namespace MyUnityTools.SceneLoading
{
    public interface ISceneLoader
    {
        void TransitionToSceneByIndex(int index);

        void TransitionToSceneByName(string name);

        void SwitchToSceneByIndex(int index);

        void SwitchToSceneByName(string name);

        void UnloadSceneByIndex(int index);

        void UnloadSceneByName(string name);

        void LoadSceneByIndex(int index, bool setActive = false);

        void LoadSceneByName(string name, bool setActive = false);
    }
}