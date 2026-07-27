using NUnit.Framework;
using System.Collections;
using System.Threading;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneManager_CancellationTests : SceneTestBase
    {
        [UnityTest]
        public IEnumerator Cancellation_DuringLoad([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            CancellationTokenSource tokenSource = new();
            WaitTask<SceneResult> waitTask = manager.LoadAsync(sceneParameters, token: tokenSource.Token).ToWaitTask();
            tokenSource.Cancel();
            yield return waitTask;
            Assert.True(waitTask.Task.IsCanceled);
            tokenSource.Dispose();
        }

        [UnityTest]
        public IEnumerator Cancellation_DuringUnload([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            CancellationTokenSource tokenSource = new();
            WaitTask<SceneResult> waitTask = manager.LoadAsync(sceneParameters).ToWaitTask();
            yield return waitTask;

            waitTask = new(manager.UnloadAsync(sceneParameters, token: tokenSource.Token));
            tokenSource.Cancel();
            yield return waitTask;
            Assert.True(waitTask.Task.IsCanceled);
            tokenSource.Dispose();
        }

        /// <summary>
        /// Cancelling before the call keeps this deterministic, unlike cancelling mid-unload.
        /// </summary>
        [UnityTest]
        public IEnumerator Cancellation_DuringUnload_ClearsActiveScene([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            CancellationTokenSource tokenSource = new();
            WaitTask<SceneResult> waitTask = manager.LoadAsync(sceneParameters).ToWaitTask();
            yield return waitTask;

            tokenSource.Cancel();
            waitTask = new(manager.UnloadAsync(sceneParameters, token: tokenSource.Token));
            yield return waitTask;

            Assert.True(waitTask.Task.IsCanceled);
            Assert.Zero(manager.LoadedSceneCount);
            Assert.False(manager.GetActiveScene().IsValid());
            tokenSource.Dispose();
        }
    }
}