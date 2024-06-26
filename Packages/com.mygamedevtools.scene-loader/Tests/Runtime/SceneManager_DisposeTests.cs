using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneManager_DisposeTests : SceneTestBase
    {
        // Note: These functions must create new scene managers to correctly test the dispose flow
        static readonly Func<ISceneManager>[] _sceneManagerCreateFuncs = new Func<ISceneManager>[]
        {
            () => new AdvancedSceneManager(),
        };

        [Test]
        public void Dispose_Simple([ValueSource(nameof(_sceneManagerCreateFuncs))] Func<ISceneManager> managerCreateFunc)
        {
            ISceneManager manager = managerCreateFunc();
            Assert.DoesNotThrow(manager.Dispose);
        }

        [UnityTest]
        public IEnumerator Dispose_DuringLoadScene([ValueSource(nameof(_sceneManagerCreateFuncs))] Func<ISceneManager> managerCreateFunc, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList))] ILoadSceneInfo sceneInfo)
        {
            ISceneManager manager = managerCreateFunc();
            WaitTask<Scene> waitTask = new(manager.LoadSceneAsync(sceneInfo).AsTask());
            manager.Dispose();
            yield return waitTask;
            Assert.That(waitTask.Task.IsCompleted);
        }

        [UnityTest]
        public IEnumerator Dispose_DuringLoadScenes([ValueSource(nameof(_sceneManagerCreateFuncs))] Func<ISceneManager> managerCreateFunc, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.MultipleLoadSceneInfoList))] ILoadSceneInfo[] sceneInfos)
        {
            ISceneManager manager = managerCreateFunc();
            WaitTask<Scene[]> waitTask = new(manager.LoadScenesAsync(sceneInfos).AsTask());
            manager.Dispose();
            yield return waitTask;
            Assert.True(waitTask.Task.IsCanceled);
        }

        [UnityTest]
        public IEnumerator Dipose_DuringUnloadScene([ValueSource(nameof(_sceneManagerCreateFuncs))] Func<ISceneManager> managerCreateFunc, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList))] ILoadSceneInfo sceneInfo)
        {
            ISceneManager manager = managerCreateFunc();
            WaitTask<Scene> waitTask = new(manager.LoadSceneAsync(sceneInfo).AsTask());
            yield return waitTask;

            waitTask = new(manager.UnloadSceneAsync(sceneInfo).AsTask());
            manager.Dispose();
            yield return waitTask;
            Assert.True(waitTask.Task.IsCanceled);
        }

        [UnityTest]
        public IEnumerator Dipose_DuringUnloadScenes([ValueSource(nameof(_sceneManagerCreateFuncs))] Func<ISceneManager> managerCreateFunc, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.MultipleLoadSceneInfoList))] ILoadSceneInfo[] sceneInfos)
        {
            ISceneManager manager = managerCreateFunc();
            WaitTask<Scene[]> waitTask = new(manager.LoadScenesAsync(sceneInfos).AsTask());
            yield return waitTask;

            waitTask = new(manager.UnloadScenesAsync(sceneInfos).AsTask());
            manager.Dispose();
            yield return waitTask;
            Assert.True(waitTask.Task.IsCanceled);
        }
    }
}