using NUnit.Framework;
using System.Collections;
using System.Threading;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneDirector_CancellationTests : SceneTestBase
    {
        [UnityTest]
        public IEnumerator Cancellation_DuringLoad([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            CancellationTokenSource tokenSource = new();
            WaitTask<SceneResult> waitTask = director.LoadAsync(sceneParameters, token: tokenSource.Token).ToWaitTask();
            tokenSource.Cancel();
            yield return waitTask;
            Assert.True(waitTask.Task.IsCanceled);
            tokenSource.Dispose();
        }

        [UnityTest]
        public IEnumerator Cancellation_DuringUnload([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneDirectors))] ISceneDirector director, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            CancellationTokenSource tokenSource = new();
            WaitTask<SceneResult> waitTask = director.LoadAsync(sceneParameters).ToWaitTask();
            yield return waitTask;

            waitTask = new(director.UnloadAsync(sceneParameters, token: tokenSource.Token));
            tokenSource.Cancel();
            yield return waitTask;
            Assert.True(waitTask.Task.IsCanceled);
            tokenSource.Dispose();
        }
    }
}