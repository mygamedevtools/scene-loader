/**
 * SceneLoaderTestUtilities.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-12
 */

using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneLoaderTestUtilities
    {
        public static IEnumerator UnloadRemainingScenes()
        {
            if (SceneManager.sceneCount == 1)
                yield break;

            SceneManager.SetActiveScene(SceneManager.GetSceneAt(0));

            var interval = new WaitForEndOfFrame();
            while (SceneManager.sceneCount > 1)
            {
                var scene = GetLastLoadedScene();
                if (scene.isLoaded)
                {
                    var operation = SceneManager.UnloadSceneAsync(scene);
                    while (!operation.isDone)
                        yield return interval;
                }
                else
                    yield return interval;
            }
        }

        public static Scene GetLastLoadedScene() => SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
    }
}