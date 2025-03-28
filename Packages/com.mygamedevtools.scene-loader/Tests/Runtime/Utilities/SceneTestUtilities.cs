using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading.Tests
{
    public static class SceneTestUtilities
    {
        public static IEnumerator UnloadDirectorScenes(ISceneManager sceneManager)
        {
            var lastScene = sceneManager.GetLastLoadedScene();
            while (sceneManager.LoadedSceneCount > 0 && lastScene.IsValid())
            {
                yield return new WaitTask<SceneResult>(sceneManager.UnloadAsync(lastScene));
                lastScene = sceneManager.GetLastLoadedScene();
            }

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

        public static IEnumerator UnloadAllScenes()
        {
            ISceneManager[] sceneManagers = SceneTestEnvironment.SceneManagers;
            for (int i = 0; i < sceneManagers.Length; i++)
                yield return UnloadDirectorScenes(sceneManagers[i]);

            yield return UnloadRemainingScenes();
        }
    }
}