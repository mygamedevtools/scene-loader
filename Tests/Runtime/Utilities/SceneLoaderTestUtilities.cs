using UnityEngine;
using System.Collections;
using NUnit.Framework;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace MyGameDevTools.SceneLoading.Tests
{
    public static class SceneLoaderTestUtilities
    {
        public const string DisposeCategoryName = "Dispose Tests";

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

        public static IEnumerator UnloadRemainingScenes()
        {
            while (UnitySceneManager.loadedSceneCount > 1)
            {
                yield return UnitySceneManager.UnloadSceneAsync(UnitySceneManager.GetSceneAt(UnitySceneManager.loadedSceneCount - 1));
            }
        }
    }
}