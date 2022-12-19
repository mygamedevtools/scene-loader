/**
 * SceneLoaderTestUtilities.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-12
 */

using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;
using MyGameDevTools.SceneLoading.AddressablesSupport;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneLoaderTestUtilities
    {
        public static IEnumerator UnloadRemainingScenes()
        {
            // The first scene is the Unity Test Runner's Scene, and we don't want to unload that
            if (SceneManager.sceneCount == 1)
                yield break;

            SceneManager.SetActiveScene(SceneManager.GetSceneAt(0));

            var interval = new WaitForEndOfFrame();
            while (SceneManager.sceneCount > 1)
            {
                var scene = GetLastLoadedScene();
                if (scene.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(scene);
                else
                    yield return interval;
            }
        }

        public static IEnumerator UnloadRemainingAddressableScenes(IAddressableSceneManager sceneManager)
        {
            var interval = new WaitForEndOfFrame();
            while (sceneManager.SceneCount > 0)
            {
                var sceneInstance = sceneManager.GetLoadedSceneAt(0);
                if (sceneInstance.Scene.IsValid() && sceneInstance.Scene.isLoaded)
                    yield return sceneManager.UnloadSceneAsync(new AddressableLoadSceneInfoInstance(sceneInstance));
                else
                    yield return interval;
            }
        }

        public static Scene GetLastLoadedAddressableScene(IAddressableSceneManager sceneManager)
        {
            if (sceneManager.SceneCount == 0)
                return default;
            return sceneManager.GetLoadedSceneAt(sceneManager.SceneCount - 1).Scene;
        }

        public static Scene GetLastLoadedScene() => SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
    }
}