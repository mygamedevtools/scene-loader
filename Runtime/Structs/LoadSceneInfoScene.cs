/**
 * LoadSceneInfoScene.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-21
 */

using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public readonly struct LoadSceneInfoScene : ILoadSceneInfo
    {
        public object Reference => _scene.buildIndex;

        readonly Scene _scene;

        public LoadSceneInfoScene(Scene scene)
        {
            _scene = scene;
        }

        public static implicit operator LoadSceneInfoScene(Scene scene) => new LoadSceneInfoScene(scene);
    }
}