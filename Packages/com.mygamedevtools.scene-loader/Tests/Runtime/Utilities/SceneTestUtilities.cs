using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading.Tests
{
    public static class SceneTestUtilities
    {
        public static IEnumerator UnloadDirectorScenes(ISceneDirector sceneDirector)
        {
            var lastScene = sceneDirector.GetLastLoadedScene();
            while (sceneDirector.LoadedSceneCount > 0 && lastScene.IsValid())
            {
                yield return new WaitTask<SceneResult>(sceneDirector.UnloadAsync(lastScene));
                lastScene = sceneDirector.GetLastLoadedScene();
            }

            Assert.Zero(sceneDirector.LoadedSceneCount);
            Assert.False(sceneDirector.GetActiveScene().IsValid());
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
            ISceneDirector[] sceneDirectors = SceneTestEnvironment.SceneDirectors;
            for (int i = 0; i < sceneDirectors.Length; i++)
                yield return UnloadDirectorScenes(sceneDirectors[i]);

            yield return UnloadRemainingScenes();
        }
    }
}