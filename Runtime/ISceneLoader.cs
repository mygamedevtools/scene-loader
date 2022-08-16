/**
 * ISceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/16/2022 (en-US)
 */

namespace MyUnityTools.SceneLoading
{
    public interface ISceneLoader
    {
        void TransitionToScene(string name);
        void TransitionToScene(int index);

        void SwitchToScene(string name);
        void SwitchToScene(int index);

        void UnloadScene(string name);
        void UnloadScene(int index);

        void LoadScene(string name, bool setActive = false);
        void LoadScene(int index, bool setActive = false);
    }
}