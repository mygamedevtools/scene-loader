/**
 * SceneTestEnvironment.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-12
 */

#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#endif
using UnityEngine.TestTools;
using NUnit.Framework;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneTestEnvironment : IPrebuildSetup, IPostBuildCleanup
    {
#if UNITY_EDITOR
        const string _scenePathBase = "Assets/_test";
#endif

        public void Setup()
        {
#if UNITY_EDITOR
            var buildScenes = new List<EditorBuildSettingsScene>(SceneBuilder.SceneNames.Length);

            SceneBuilder.BuildScenes(_scenePathBase, (i, s, p) => buildScenes.Add(new EditorBuildSettingsScene(p, true)));

            Debug.Log("Adding test scenes to build settings:\n" + string.Join("\n", buildScenes.Select(scene => scene.path)));
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Union(buildScenes).ToArray();
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(scene => !scene.path.StartsWith(_scenePathBase)).ToArray();

            AssetDatabase.DeleteAsset(_scenePathBase);
            AssetDatabase.Refresh();
#endif
        }

        [Test]
        public void EnvironmentSetup_Test()
        {
#if UNITY_EDITOR
            var builtScenes = EditorBuildSettings.scenes;
            Assert.True(hasAllEnvironmentScenes());

            bool hasAllEnvironmentScenes()
            {
                foreach (var name in SceneBuilder.SceneNames)
                {
                    if (!hasBuiltSceneWithName(name))
                        return false;
                }
                return true;
            }

            bool hasBuiltSceneWithName(string name)
            {
                foreach (var builtScene in builtScenes)
                    if (builtScene.path.Contains(name))
                        return true;
                return false;
            }
#endif
        }
    }
}