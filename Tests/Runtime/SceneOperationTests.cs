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
    /// <summary>The operation handle: its state machine, its awaiter, and the combinators.</summary>
    public class SceneOperationTests : SceneTestBase
    {
        ISceneManager Manager => SceneTestEnvironment.SceneManagers[0];

        [OneTimeSetUp]
        public void OneTimeSetup() => SceneTestEnvironment.ValidateSceneEnvironment();

        // Subscribes through OperationStarted rather than to the returned handle: the first
        // state is entered synchronously, so by the time the call returns it is already loading.
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

        /// <summary>
        /// The whole sequence, in order and without repeats. A transition loads and unloads the
        /// loading screen's own scene as well as the target, and reporting that work made
        /// Loading, Activating and Unloading each occur twice meaning different things — the
        /// second Loading arriving after the first had already finished, so a listener reading
        /// the states as forward progress went backwards.
        /// </summary>
        [UnityTest]
        public IEnumerator Transition_MovesThroughItsStatesInOrder_WithoutRepeatingAny()
        {
            yield return Manager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true)).ToCoroutine();

            // Subscribed through OperationStarted for the same reason as the load above: nothing
            // guarantees the first state waits for the call to return.
            List<SceneOperationState> states = new();
            Manager.OperationStarted += Watch;

            SceneOperation operation = Manager.TransitionAsync(SceneBuilder.SceneNames[2], SceneBuilder.SceneNames[0]);

            Manager.OperationStarted -= Watch;

            yield return operation.ToCoroutine();

            CollectionAssert.AreEqual(new[]
            {
                SceneOperationState.ScreenIn,
                SceneOperationState.Unloading,
                SceneOperationState.Loading,
                SceneOperationState.Activating,
                SceneOperationState.ScreenOut,
                SceneOperationState.Completed,
            }, states);

            void Watch(SceneOperation started) => started.StateChanged += o => states.Add(o.State);
        }

        /// <summary>
        /// Progress belongs to the scenes that were asked for. Loading the screen's own scene
        /// used to drive it too, so a bar filled to 1, dropped back to 0 and filled again.
        /// </summary>
        [UnityTest]
        public IEnumerator Transition_Progress_OnlyMovesForward()
        {
            yield return Manager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true)).ToCoroutine();

            List<float> reported = new();
            Manager.OperationStarted += Watch;

            SceneOperation operation = Manager.TransitionAsync(SceneBuilder.SceneNames[2], SceneBuilder.SceneNames[0]);

            Manager.OperationStarted -= Watch;

            yield return operation.ToCoroutine();

            for (int i = 1; i < reported.Count; i++)
                Assert.GreaterOrEqual(reported[i], reported[i - 1], $"Progress went backwards at report {i}: {string.Join(", ", reported)}.");

            Assert.AreEqual(1f, operation.Progress);

            void Watch(SceneOperation started) => started.Progressed += reported.Add;
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

            // This is issue #52: knowing when the loading screen is completely gone has to be
            // observable from the operation itself, not from the screen's internals.
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

        /// <summary>
        /// A subscriber that throws is the subscriber's bug, but it used to become everyone's:
        /// the throw escaped through <c>Finish</c> before the awaiter continuations ran, so
        /// <c>await operation</c> never resumed — and because nothing awaits the manager's
        /// internal task, not a single line was logged about it.
        /// </summary>
        [UnityTest]
        public IEnumerator ThrowingSubscriber_IsReported_AndDoesNotStrandAwaiters()
        {
            LogAssert.Expect(LogType.Error, new Regex("Completed subscriber threw"));

            SceneOperation operation = Manager.LoadAsync(SceneBuilder.SceneNames[1]);

            // Subscribed before the awaiter so it runs first inside Finish().
            operation.Completed += _ => throw new InvalidOperationException("thrown by a subscriber");

            bool resumed = false;
            Task awaiting = Resume();

            yield return operation.ToCoroutine();
            yield return null;

            Assert.IsTrue(resumed, "The awaiter never resumed: a throwing subscriber stranded it.");
            Assert.AreEqual(SceneOperationState.Completed, operation.State, "The operation itself succeeded, so a subscriber's throw must not change its outcome.");
            Assert.IsNull(operation.Exception, "A subscriber's throw is not the operation's failure.");
            Assert.IsTrue(awaiting.IsCompleted);

            async Task Resume()
            {
                await operation;
                resumed = true;
            }
        }

        /// <summary>
        /// The containment reports through <see cref="SceneManagerLog"/>, from inside a catch
        /// block — so a substituted handler that throws used to escape it and strand the awaiter
        /// all over again, which is exactly the failure the containment exists to prevent.
        /// </summary>
        [UnityTest]
        public IEnumerator ThrowingSubscriber_IsStillContained_WhenTheLogHandlerAlsoThrows()
        {
            LogAssert.Expect(LogType.Error, new Regex("threw, so this went to the console instead"));

            ILogHandler original = SceneManagerLog.Handler;
            SceneManagerLog.Handler = new ThrowingLogHandler();

            bool resumed = false;
            SceneOperation operation;
            try
            {
                operation = Manager.LoadAsync(SceneBuilder.SceneNames[1]);
                operation.Completed += _ => throw new InvalidOperationException("thrown by a subscriber");
                _ = Resume();

                yield return operation.ToCoroutine();
                yield return null;
            }
            finally
            {
                SceneManagerLog.Handler = original;
            }

            Assert.IsTrue(resumed, "A broken log handler must not turn a contained subscriber throw back into a stranded awaiter.");

            async Task Resume()
            {
                await operation;
                resumed = true;
            }
        }

        class ThrowingLogHandler : ILogHandler
        {
            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args) =>
                throw new InvalidOperationException("thrown by the log handler");

            public void LogException(Exception exception, UnityEngine.Object context) =>
                throw new InvalidOperationException("thrown by the log handler");
        }

        /// <summary>The same containment on the per-frame progress callback, which the pump drives.</summary>
        [UnityTest]
        public IEnumerator ThrowingProgressSubscriber_DoesNotWedgeTheOperation()
        {
            LogAssert.Expect(LogType.Error, new Regex("Progressed subscriber threw"));

            SceneOperation operation = Manager.LoadAsync(SceneBuilder.SceneNames[1]);
            operation.Progressed += _ => throw new InvalidOperationException("thrown by a subscriber");

            yield return operation.ToCoroutine();

            Assert.AreEqual(SceneOperationState.Completed, operation.State);
            Assert.AreEqual(1, Manager.LoadedSceneCount);
        }
    }

    static class TaskTestExtensions
    {
        /// <summary>Drives a <see cref="Task"/> from a <c>[UnityTest]</c>, rethrowing so its assertions fail the test.</summary>
        public static IEnumerator ToCoroutineTest(this Task task)
        {
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
                throw task.Exception.InnerException;
        }
    }
}
