using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// Disposing a manager cancels everything it still has in flight.
    /// <br/>
    /// Same behaviour as v4, with none of the machinery: there is no lifetime
    /// <c>CancellationTokenSource</c>, no linked source per call, and no
    /// <c>token.Register</c> closure. The manager simply holds its live operations and cancels
    /// each one.
    /// </summary>
    public class SceneManager_DisposeTests : SceneTestBase
    {
        // Note: These functions must create new Scene Managers to correctly test the dispose flow
        static readonly Func<ISceneManager>[] _sceneManagerCreateFuncs = new Func<ISceneManager>[]
        {
            () => new CoreSceneManager(),
        };

        [Test]
        public void Dispose_Simple([ValueSource(nameof(_sceneManagerCreateFuncs))] Func<ISceneManager> managerCreateFunc)
        {
            ISceneManager manager = managerCreateFunc();
            Assert.DoesNotThrow(manager.Dispose);
        }

        [UnityTest]
        public IEnumerator Dispose_DuringLoad([ValueSource(nameof(_sceneManagerCreateFuncs))] Func<ISceneManager> managerCreateFunc, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            ISceneManager manager = managerCreateFunc();
            SceneOperation operation = manager.LoadAsync(sceneParameters);
            manager.Dispose();

            yield return operation.ToCoroutine();

            Assert.AreEqual(SceneOperationState.Canceled, operation.State);
        }

        [UnityTest]
        public IEnumerator Dispose_DuringUnload([ValueSource(nameof(_sceneManagerCreateFuncs))] Func<ISceneManager> managerCreateFunc, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            ISceneManager manager = managerCreateFunc();
            yield return manager.LoadAsync(sceneParameters).ToCoroutine();

            SceneOperation operation = manager.UnloadAsync(sceneParameters);
            manager.Dispose();

            yield return operation.ToCoroutine();

            Assert.AreEqual(SceneOperationState.Canceled, operation.State);
        }

        [UnityTest]
        public IEnumerator Dispose_DuringTransition([ValueSource(nameof(_sceneManagerCreateFuncs))] Func<ISceneManager> managerCreateFunc, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.TransitionSceneParametersList))] SceneParameters sceneParameters, [ValueSource(typeof(SceneManagerTests), nameof(SceneManagerTests.LoadingScenes))] SceneRef loadingScene)
        {
            ISceneManager manager = managerCreateFunc();
            yield return SceneManagerTests.LoadFirstScene(manager);

            SceneOperation operation = manager.TransitionAsync(sceneParameters, loadingScene);
            manager.Dispose();

            yield return operation.ToCoroutine();

            Assert.AreEqual(SceneOperationState.Canceled, operation.State);
        }
    }
}
