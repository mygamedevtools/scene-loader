using UnityEngine;
using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading.Tests
{
    public static class SceneLoaderTestUtilities
    {
        public const string DisposeCategoryName = "Dispose Tests";

        public static IEnumerator UnloadManagerScenes(ISceneManager sceneManager)
        {
            var lastScene = sceneManager.GetLastLoadedScene();
            while (sceneManager.LoadedSceneCount > 0 && lastScene.IsValid())
            {
                yield return new WaitTask(sceneManager.UnloadSceneAsync(new LoadSceneInfoScene(lastScene)).AsTask());
                lastScene = sceneManager.GetLastLoadedScene();
            }

            while (sceneManager.LoadedSceneCount > 0)
                yield return new WaitUntil(() => sceneManager.LoadedSceneCount == 0);

            Assert.Zero(sceneManager.LoadedSceneCount);
            Assert.False(sceneManager.GetActiveScene().IsValid());
        }

        public static IEnumerator UnloadRemainingScenes()
        {
            while (SceneManager.sceneCount > 1)
            {
                Scene scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
                if (scene.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(scene);
                else
                    yield return null;
            }
        }
    }
}