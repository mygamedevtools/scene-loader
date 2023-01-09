/**
 * SceneLoaderTestUtilities.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-12
 */

using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;
using MyGameDevTools.SceneLoading.AddressablesSupport;
using NUnit.Framework;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneLoaderTestUtilities
    {
        public static IEnumerator UnloadManagerScenes(ISceneManager sceneManager)
        {
            while (sceneManager.SceneCount > 0)
                yield return new WaitTask(sceneManager.UnloadSceneAsync(new LoadSceneInfoScene(sceneManager.GetLastLoadedScene())).AsTask());

            Assert.Zero(sceneManager.SceneCount);
            Assert.False(sceneManager.GetActiveScene().IsValid());
            Assert.AreEqual(1, UnityEngine.SceneManagement.SceneManager.sceneCount);
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
    }
}