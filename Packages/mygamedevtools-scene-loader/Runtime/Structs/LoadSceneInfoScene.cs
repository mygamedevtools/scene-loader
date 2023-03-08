/**
 * LoadSceneInfoScene.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-21
 */

using System;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public readonly struct LoadSceneInfoScene : ILoadSceneInfo
    {
        public object Reference => _scene;

        readonly Scene _scene;

        public LoadSceneInfoScene(Scene scene)
        {
            if (!scene.IsValid())
                throw new ArgumentException("Cannot create a LoadSceneInfoScene from an invalid scene.", nameof(scene));
            _scene = scene;
        }

        public bool IsReferenceToScene(Scene scene) => scene == _scene;

        public override string ToString()
        {
            return $"Scene \"{_scene.name}\" [{_scene.handle}]";
        }
    }
}