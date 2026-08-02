using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// The operation handle itself: its state machine, its awaiter, and the combinators.
    /// </summary>
    public class SceneOperationTests : SceneTestBase
    {
        ISceneManager Manager => SceneTestEnvironment.SceneManagers[0];

        [OneTimeSetUp]
        public void OneTimeSetup() => SceneTestEnvironment.ValidateSceneEnvironment();

        /// <summary>
        /// Subscribes through <see cref="ISceneManager.OperationStarted"/> rather than to the
        /// returned handle, because the first state is entered synchronously — by the time the
        /// call returns, the operation is already loading.
        /// </summary>
        [UnityTest]
        public IEnumerator Load_MovesThroughItsStatesInOrder()
        {
            List<SceneOperationState> states = new();
            Manager.OperationStarted += Watch;

            SceneOperation operation = Manager.LoadAsync(SceneBuilder.SceneNames[1]);

            Manager.OperationStarted -= Watch;

            yield return operation.ToCoroutine();

            CollectionAssert.AreEqual(new[]
            {
                SceneOperationState.Loading,
                SceneOperationState.Activating,
                SceneOperationState.Completed,
            }, states);

            void Watch(SceneOperation started) => started.StateChanged += o => states.Add(o.State);
        }

        [UnityTest]
        public IEnumerator Transition_ReachesScreenOutBeforeItCompletes()
        {
            yield return Manager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true)).ToCoroutine();

            SceneOperation operation = Manager.TransitionAsync(SceneBuilder.SceneNames[1], SceneBuilder.SceneNames[0]);

            bool sawScreenIn = false;
            bool sawScreenOut = false;
            bool screenOutCameBeforeCompletion = false;
            operation.StateChanged += o =>
            {
                sawScreenIn |= o.State == SceneOperationState.ScreenIn;
                sawScreenOut |= o.State == SceneOperationState.ScreenOut;
                if (o.State == SceneOperationState.Completed)
                    screenOutCameBeforeCompletion = sawScreenOut;
            };

            yield return operation.ToCoroutine();

            // This is issue #52: knowing when the loading screen is completely gone used to mean
            // reaching into a publicly exposed TaskCompletionSource.
            Assert.True(sawScreenIn, "A transition with a loading screen should report ScreenIn.");
            Assert.True(sawScreenOut, "A transition with a loading screen should report ScreenOut.");
            Assert.True(screenOutCameBeforeCompletion, "ScreenOut should be reported before the operation completes.");
        }

        [UnityTest]
        public IEnumerator Await_Twice_ReturnsTheSameResult()
        {
            yield return AwaitTwice().ToCoroutineTest();

            async Task AwaitTwice()
            {
                SceneOperation operation = Manager.LoadAsync(SceneBuilder.SceneNames[1]);

                SceneResult first = await operation;
                // Re-awaitable: this is why the awaiter is hand-rolled instead of using Awaitable,
                // whose objects return to a pool after a single await.
                SceneResult second = await operation;

                Assert.AreEqual(first.GetScene(), second.GetScene());
            }
        }

        [UnityTest]
        public IEnumerator Progressed_DoesNotFireForUnchangedValues()
        {
            SceneOperation operation = Manager.LoadAsync(SceneBuilder.SceneNames[1]);

            List<float> reported = new();
            operation.Progressed += reported.Add;

            yield return operation.ToCoroutine();

            CollectionAssert.IsOrdered(reported);
            CollectionAssert.AllItemsAreUnique(reported);
            Assert.AreEqual(1f, reported[^1]);
        }

        [UnityTest]
        public IEnumerator Completed_FiresOnceAndImmediatelyForLateSubscribers()
        {
            SceneOperation operation = Manager.LoadAsync(SceneBuilder.SceneNames[1]);

            int fired = 0;
            operation.Completed += _ => fired++;

            yield return operation.ToCoroutine();
            Assert.AreEqual(1, fired);

            int lateFired = 0;
            operation.Completed += _ => lateFired++;
            Assert.AreEqual(1, lateFired, "Subscribing after completion should invoke the handler immediately.");
        }

        [UnityTest]
        public IEnumerator AsTask_CompletesWithTheSameResult()
        {
            SceneOperation operation = Manager.LoadAsync(SceneBuilder.SceneNames[1]);
            Task<SceneResult> task = operation.AsTask();

            yield return operation.ToCoroutine();
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.True(task.IsCompletedSuccessfully);
            Assert.AreEqual(operation.Result.GetScene(), task.Result.GetScene());
        }

        [UnityTest]
        public IEnumerator AsTask_CancelsWhenTheOperationDoes()
        {
            SceneOperation operation = Manager.LoadAsync(SceneBuilder.SceneNames[1]);
            Task<SceneResult> task = operation.AsTask();
            operation.Cancel();

            yield return new WaitUntil(() => task.IsCompleted);

            Assert.True(task.IsCanceled);
        }

        [UnityTest]
        public IEnumerator AsTask_FaultsWhenTheOperationDoes()
        {
            LogAssert.Expect(LogType.Error, new Regex("faulted during Resolving"));

            SceneOperation operation = Manager.LoadAsync("not-a-real-scene");
            Task<SceneResult> task = operation.AsTask();

            yield return new WaitUntil(() => task.IsCompleted);

            Assert.True(task.IsFaulted);
            Assert.AreSame(operation.Exception, task.Exception.InnerException);
        }

        [UnityTest]
        public IEnumerator WhenAll_CompletesAfterEveryOperation()
        {
            SceneOperation first = Manager.LoadAsync(SceneBuilder.SceneNames[1]);
            SceneOperation second = Manager.LoadAsync(SceneBuilder.SceneNames[2]);

            SceneOperation all = SceneOperation.WhenAll(first, second);

            yield return all.ToCoroutine();

            Assert.True(first.IsDone);
            Assert.True(second.IsDone);
            Assert.AreEqual(2, all.Result.GetScenes().Length);
        }

        [UnityTest]
        public IEnumerator WhenAny_CompletesWithTheFirstToFinish()
        {
            SceneOperation first = Manager.LoadAsync(SceneBuilder.SceneNames[1]);
            SceneOperation second = Manager.LoadAsync(SceneBuilder.SceneNames[2]);

            SceneOperation any = SceneOperation.WhenAny(first, second);

            yield return any.ToCoroutine();

            Assert.True(any.IsDone);
            Assert.AreEqual(1, any.Result.GetScenes().Length);

            // The loser keeps running; the teardown needs both settled.
            yield return SceneOperation.WhenAll(first, second).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator OperationStarted_FiresOncePerOperation()
        {
            List<SceneOperation> started = new();
            Manager.OperationStarted += started.Add;

            SceneOperation operation = Manager.LoadAsync(SceneBuilder.SceneNames[1]);

            Manager.OperationStarted -= started.Add;

            Assert.AreEqual(1, started.Count);
            Assert.AreSame(operation, started[0]);

            yield return operation.ToCoroutine();
        }

        [Test]
        public void WhenAll_WithNoOperations_Throws()
        {
            Assert.Throws<ArgumentException>(() => SceneOperation.WhenAll());
            Assert.Throws<ArgumentException>(() => SceneOperation.WhenAny());
        }
    }

    static class TaskTestExtensions
    {
        /// <summary>
        /// Drives a <see cref="Task"/> from a <c>[UnityTest]</c>, rethrowing whatever it threw so
        /// the assertions inside it actually fail the test.
        /// </summary>
        public static IEnumerator ToCoroutineTest(this Task task)
        {
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
                throw task.Exception.InnerException;
        }
    }
}
