/**
 * SceneBuilder.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-12
 */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;
#endif
using UnityEngine.SceneManagement;
using System;

namespace MyGameDevTools.SceneLoading.Tests
{
    public static class SceneBuilder
    {
        public static readonly string[] SceneNames = new string[] { "loading", "sceneA", "sceneB", "sceneC" };

#if UNITY_EDITOR
        static readonly string _scenePathFormat = "/{0}.unity";
#endif

        public static void BuildScenes(string pathBase, Action<int, Scene, string> sceneSaved)
        {
#if UNITY_EDITOR
            if (!Directory.Exists(pathBase))
                Directory.CreateDirectory(pathBase);

            var fullPathFormat = pathBase + _scenePathFormat;

            var length = SceneNames.Length;

            for (int i = 0; i < length; i++)
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

                if (i == 0)
                {
                    var loadingObject = new GameObject(nameof(LoadingBehavior), typeof(LoadingBehavior));
                    SceneManager.MoveGameObjectToScene(loadingObject, scene);
                }

                var path = string.Format(fullPathFormat, SceneNames[i]);
                EditorSceneManager.SaveScene(scene, path);
                EditorSceneManager.CloseScene(scene, true);
                sceneSaved?.Invoke(i, scene, path);
            }
#endif
        }
    }
}