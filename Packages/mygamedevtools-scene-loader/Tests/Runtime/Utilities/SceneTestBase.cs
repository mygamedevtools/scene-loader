using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public abstract class SceneTestBase
    {
        [UnityTearDown]
        public IEnumerator UnloadScenesOnTearDown()
        {
            yield return SceneTestUtilities.UnloadAllScenes();
            Assert.AreEqual(1, SceneManager.sceneCount);
        }
    }
}