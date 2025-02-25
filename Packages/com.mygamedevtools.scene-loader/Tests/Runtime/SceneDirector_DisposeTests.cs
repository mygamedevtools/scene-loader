using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneDirector_DisposeTests : SceneTestBase
    {
        // Note: These functions must create new Scene Directors to correctly test the dispose flow
        static readonly Func<ISceneDirector>[] _sceneDirectorCreateFuncs = new Func<ISceneDirector>[]
        {
            () => new SceneDirector(),
        };

        [Test]
        public void Dispose_Simple([ValueSource(nameof(_sceneDirectorCreateFuncs))] Func<ISceneDirector> directorCreateFunc)
        {
            ISceneDirector director = directorCreateFunc();
            Assert.DoesNotThrow(director.Dispose);
        }

        [UnityTest]
        public IEnumerator Dispose_DuringLoad([ValueSource(nameof(_sceneDirectorCreateFuncs))] Func<ISceneDirector> directorCreateFunc, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            ISceneDirector director = directorCreateFunc();
            WaitTask<SceneResult> waitTask = director.LoadAsync(sceneParameters).ToWaitTask();
            director.Dispose();
            yield return waitTask;
            Assert.True(waitTask.Task.IsCanceled);
        }

        [UnityTest]
        public IEnumerator Dipose_DuringUnload([ValueSource(nameof(_sceneDirectorCreateFuncs))] Func<ISceneDirector> directorCreateFunc, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            ISceneDirector director = directorCreateFunc();
            WaitTask<SceneResult> waitTask = director.LoadAsync(sceneParameters).ToWaitTask();
            yield return waitTask;

            waitTask = new(director.UnloadAsync(sceneParameters));
            director.Dispose();
            yield return waitTask;
            Assert.True(waitTask.Task.IsCanceled);
        }

        [UnityTest]
        public IEnumerator Dispose_DuringTransition([ValueSource(nameof(_sceneDirectorCreateFuncs))] Func<ISceneDirector> directorCreateFunc, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.TransitionSceneParametersList))] SceneParameters sceneParameters, [ValueSource(typeof(SceneDirectorTests), nameof(SceneDirectorTests.LoadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            async Awaitable Test()
            {
                ISceneDirector director = directorCreateFunc();
                await SceneDirectorTests.LoadFirstScene(director).Task;

                var task = director.TransitionAsync(sceneParameters, loadingScene);
                director.Dispose();

                bool canceled = false;
                try
                {
                    await task;
                }
                catch (OperationCanceledException)
                {
                    canceled = true;
                }
                Assert.True(canceled);
            }
            return Test();
        }
    }
}