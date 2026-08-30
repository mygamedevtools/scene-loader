using System.Collections;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// Cancellation, after the token left the public API. These were token tests; they are now
    /// state-machine tests, asserting the honest contract — the operation stops reporting and
    /// completes in <see cref="SceneOperationState.Canceled"/> while the engine finishes
    /// underneath.
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
        /// The half that is easy to get wrong: cancelling does not undo a load. The scene still
        /// turns up — it is simply no longer this operation's business.
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

        /// <summary>
        /// Zero tracked scenes and a valid active scene cannot both be true, however the unload
        /// ended. Cancelling one is the way to end up asking.
        /// </summary>
        /// <remarks>
        /// The unload used to drop its scenes from <c>_loadedScenes</c> before awaiting the engine
        /// and reconcile the active scene only afterwards, leaving the pair disagreeing for the
        /// length of the unload. This asserts the property, not the ordering that exposed it: by
        /// the time the operation returns the window has closed either way, so it passes against
        /// both. It is here to keep the invariant stated, and to fail if a future change reopens
        /// the window past the end of the operation.
        /// </remarks>
        [UnityTest]
        public IEnumerator Cancel_DuringUnload_LeavesNoActiveSceneBehind([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            yield return manager.LoadAsync(sceneParameters).ToCoroutine();

            SceneOperation operation = manager.UnloadAsync(sceneParameters);
            operation.Cancel();

            yield return operation.ToCoroutine();

            if (manager.LoadedSceneCount != 0)
                yield break;

            Assert.False(manager.GetActiveScene().IsValid(),
                $"No scenes are tracked, yet '{manager.GetActiveScene().name}' is still reported as active.");
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

        /// <summary>The opt-in bridge that replaced the <c>CancellationToken</c> parameter on every method.</summary>
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
