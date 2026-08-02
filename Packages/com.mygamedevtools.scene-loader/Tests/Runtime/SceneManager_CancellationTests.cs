using System.Collections;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// Cancellation, after the token left the public API.
    /// <br/><br/>
    /// These were token tests. They are now state-machine tests, because there is exactly one
    /// cancellation mechanism: <see cref="SceneOperation.Cancel"/>. The token never cancelled
    /// the work anyway — Unity scene operations cannot be aborted, which v4's own XML docs said
    /// on all 64 methods — so what these assert is the honest contract: the operation stops
    /// reporting and completes in <see cref="SceneOperationState.Canceled"/>, while the engine
    /// finishes what it started underneath.
    /// </summary>
    public class SceneManager_CancellationTests : SceneTestBase
    {
        [UnityTest]
        public IEnumerator Cancel_DuringLoad_CompletesCanceled([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            SceneOperation operation = manager.LoadAsync(sceneParameters);
            operation.Cancel();

            yield return operation.ToCoroutine();

            Assert.AreEqual(SceneOperationState.Canceled, operation.State);
            Assert.True(operation.IsDone);
        }

        /// <summary>
        /// The half of the contract that is easy to get wrong: cancelling does not undo a load.
        /// Unity has already been asked to load the scene and there is no way to take that back,
        /// so the scene still turns up — it is simply no longer this operation's business.
        /// </summary>
        [UnityTest]
        public IEnumerator Cancel_DuringLoad_TheSceneStillFinishesLoading([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            int sceneCountBefore = SceneManager.sceneCount;

            SceneOperation operation = manager.LoadAsync(SceneBuilder.SceneNames[1]);
            operation.Cancel();

            yield return operation.ToCoroutine();
            Assert.AreEqual(SceneOperationState.Canceled, operation.State);

            yield return new WaitUntil(() => SceneManager.sceneCount > sceneCountBefore);
        }

        [UnityTest]
        public IEnumerator Cancel_DuringUnload_CompletesCanceled([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            yield return manager.LoadAsync(sceneParameters).ToCoroutine();

            SceneOperation operation = manager.UnloadAsync(sceneParameters);
            operation.Cancel();

            yield return operation.ToCoroutine();

            Assert.AreEqual(SceneOperationState.Canceled, operation.State);
        }

        [UnityTest]
        public IEnumerator Cancel_AfterCompletion_DoesNothing([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            SceneOperation operation = manager.LoadAsync(SceneBuilder.SceneNames[1]);
            yield return operation.ToCoroutine();

            Assert.AreEqual(SceneOperationState.Completed, operation.State);

            operation.Cancel();

            Assert.AreEqual(SceneOperationState.Completed, operation.State);
        }

        /// <summary>
        /// <see cref="SceneOperation.CancelWith"/> is the opt-in bridge that replaced the
        /// <c>CancellationToken</c> parameter on every method.
        /// </summary>
        [UnityTest]
        public IEnumerator CancelWith_CancelsWhenTheTokenDoes([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            using CancellationTokenSource tokenSource = new();

            SceneOperation operation = manager.LoadAsync(SceneBuilder.SceneNames[1]).CancelWith(tokenSource.Token);
            tokenSource.Cancel();

            yield return operation.ToCoroutine();

            Assert.AreEqual(SceneOperationState.Canceled, operation.State);
        }

        [UnityTest]
        public IEnumerator CancelWith_AnAlreadyCancelledToken_CancelsImmediately([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            using CancellationTokenSource tokenSource = new();
            tokenSource.Cancel();

            SceneOperation operation = manager.LoadAsync(SceneBuilder.SceneNames[1]).CancelWith(tokenSource.Token);

            Assert.AreEqual(SceneOperationState.Canceled, operation.State);

            yield return operation.ToCoroutine();
        }
    }
}
