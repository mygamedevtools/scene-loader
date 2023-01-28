/**
 * SceneLoaderTestUtilities.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-12
 */

using UnityEngine;
using System.Collections;
using NUnit.Framework;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneLoaderTestUtilities
    {
        public static IEnumerator UnloadManagerScenes(ISceneManager sceneManager)
        {
            var lastScene = sceneManager.GetLastLoadedScene();
            while (sceneManager.SceneCount > 0 && lastScene.IsValid())
            {
                yield return new WaitTask(sceneManager.UnloadSceneAsync(new LoadSceneInfoScene(lastScene)).AsTask());
                lastScene = sceneManager.GetLastLoadedScene();
            }

            while (sceneManager.SceneCount > 0)
                yield return new WaitUntil(() => sceneManager.SceneCount == 0);

            Assert.Zero(sceneManager.SceneCount);
            Assert.False(sceneManager.GetActiveScene().IsValid());
        }
    }
}