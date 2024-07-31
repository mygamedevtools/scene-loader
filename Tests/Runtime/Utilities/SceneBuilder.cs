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
        public static readonly string[] ScenePaths = new string[]
        {
            SceneTestEnvironment.ScenePathBase + "/" + SceneNames[0] + ".unity",
            SceneTestEnvironment.ScenePathBase + "/" + SceneNames[1] + ".unity",
            SceneTestEnvironment.ScenePathBase + "/" + SceneNames[2] + ".unity",
            SceneTestEnvironment.ScenePathBase + "/" + SceneNames[3] + ".unity",
        };

#if UNITY_EDITOR
        static readonly string _scenePathFormat = "/{0}.unity";
#endif

        public static bool TryBuildScenes(string pathBase, Action<int, Scene, string> sceneSaved)
        {
#if UNITY_EDITOR
            if (!Directory.Exists(pathBase))
                Directory.CreateDirectory(pathBase);
            else if (Directory.GetFiles(pathBase).Length >= SceneNames.Length)
                return false;

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
            return true;
#else
            return false;
#endif
        }
    }
}