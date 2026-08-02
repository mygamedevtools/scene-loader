using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// A live handle on a scene operation: what phase it is in, how far along it is, what it
    /// produced, and how to wait for it.
    /// <br/><br/>
    /// Every operation returns one of these instead of a <c>Task&lt;SceneResult&gt;</c>. That is
    /// what let <c>IProgress&lt;float&gt;</c> and <c>CancellationToken</c> leave every public
    /// signature: in v4 you had to decide up front whether you wanted progress or cancellation,
    /// because there was nothing to attach them to afterwards.
    /// <code>
    /// SceneOperation op = MySceneManager.TransitionAsync("target", "loading");
    ///
    /// op.Progressed   += p =&gt; bar.value = p;
    /// op.StateChanged += o =&gt; { if (o.State == SceneOperationState.ScreenOut) BeginIntro(); };
    ///
    /// SceneResult result = await op;   // or op.Cancel(), or yield return op.ToCoroutine()
    /// </code>
    /// </summary>
    /// <remarks>
    /// <b>Not pooled, deliberately.</b> #76 asked for pooling, and it is not safe for a handle
    /// this API actively encourages callers to keep: <c>op.Result</c> after completion and
    /// awaiting the same operation twice are both supported, so there is no moment at which the
    /// package can know nobody holds it any more. Recycling anyway is exactly the
    /// single-await-then-return-to-pool hazard that kept <c>Awaitable</c> out of the design
    /// (#71 §7.5), and it would trade a use-after-free footgun for one small class allocation
    /// against the tens of kilobytes a scene load costs. The per-operation buffers <i>are</i>
    /// pooled, in <see cref="SceneLinker"/> and <see cref="SceneOperationPump"/>.
    /// </remarks>
    public sealed class SceneOperation
    {
        /// <summary>
        /// Progress has to move by at least this much before <see cref="Progressed"/> fires
        /// again. Without it the pump would re-raise an unchanged value every frame.
        /// </summary>
        const float ProgressEpsilon = 0.0001f;

        /// <summary>
        /// Which operation this is.
        /// </summary>
        public SceneOperationKind Kind { get; }

        /// <summary>
        /// The phase this operation is in.
        /// </summary>
        public SceneOperationState State { get; private set; }

        /// <summary>
        /// How far the load has got, from 0 to 1.
        /// <br/>
        /// The two backends measure different work — Addressables includes download time, the
        /// standard path does not — so a group mixing them advances unevenly. That is a
        /// presentation caveat, not a bug: rescaling one to match the other would be inventing a
        /// number neither backend reports.
        /// </summary>
        public float Progress { get; private set; }

        /// <summary>
        /// The scenes this operation produced. Empty until <see cref="State"/> reaches
        /// <see cref="SceneOperationState.Completed"/>.
        /// </summary>
        public SceneResult Result { get; private set; }

        /// <summary>
        /// Why the operation faulted, or <see langword="null"/>.
        /// <br/>
        /// Only the addressable path can report a real failure. The Unity Scene Manager has no
        /// failure surface at all — a bad scene name logs to the console and the operation still
        /// reports itself done — so a standard-path failure surfaces as a link failure instead.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Whether the operation has finished, successfully or not.
        /// </summary>
        public bool IsDone => State >= SceneOperationState.Completed;

        /// <summary>
        /// Fires when <see cref="Progress"/> moves. Not raised for unchanged values.
        /// </summary>
        public event Action<float> Progressed;
        /// <summary>
        /// Fires once per scene this operation loads.
        /// </summary>
        public event Action<Scene> SceneLoaded;
        /// <summary>
        /// Fires once per scene this operation unloads.
        /// </summary>
        public event Action<Scene> SceneUnloaded;
        /// <summary>
        /// Fires on every <see cref="State"/> change.
        /// </summary>
        public event Action<SceneOperation> StateChanged;
        /// <summary>
        /// Fires exactly once when the operation finishes — on success, cancellation and fault
        /// alike. Subscribing after completion invokes it immediately.
        /// </summary>
        public event Action<SceneOperation> Completed
        {
            add
            {
                if (IsDone)
                {
                    value?.Invoke(this);
                    return;
                }
                _completed += value;
            }
            remove => _completed -= value;
        }

        /// <summary>
        /// Whether <see cref="Cancel"/> has been called. Internal phases check this to stop
        /// early.
        /// </summary>
        internal bool IsCancellationRequested { get; private set; }

        Action<SceneOperation> _completed;
        Action _continuations;
        CancellationTokenRegistration _cancellationRegistration;

        internal SceneOperation(SceneOperationKind kind)
        {
            Kind = kind;
            State = SceneOperationState.Pending;
        }

        /// <summary>
        /// Stops this operation as soon as it can, completing it in
        /// <see cref="SceneOperationState.Canceled"/>.
        /// <br/><br/>
        /// <b>The underlying Unity scene operations keep running.</b> They cannot be aborted —
        /// that is not a limitation of this package, and it is why v4's
        /// <c>CancellationToken</c> parameters never cancelled the work either, only the await.
        /// A scene that was already loading will finish loading; what stops is this operation's
        /// reporting, its remaining phases, and everything waiting on it.
        /// </summary>
        public void Cancel()
        {
            if (IsDone || IsCancellationRequested)
                return;

            IsCancellationRequested = true;

            if (SceneManagerLog.IsEnabled(SceneLogLevel.Info))
                SceneManagerLog.Info($"{Kind} operation cancelled during {State}. The underlying Unity operations will still run to completion.");

            SetState(SceneOperationState.Canceled);
            Finish();
        }

        /// <summary>
        /// Cancels this operation when <paramref name="token"/> is cancelled.
        /// <br/>
        /// The opt-in bridge for structured concurrency — a <c>MonoBehaviour</c>'s
        /// <c>destroyCancellationToken</c>, typically — rather than a parameter on every method:
        /// <code>
        /// MySceneManager.TransitionAsync("target", "loading").CancelWith(destroyCancellationToken);
        /// </code>
        /// </summary>
        /// <returns>This operation, so it can be chained onto the call that created it.</returns>
        public SceneOperation CancelWith(CancellationToken token)
        {
            if (IsDone || !token.CanBeCanceled)
                return this;

            if (token.IsCancellationRequested)
            {
                Cancel();
                return this;
            }

            _cancellationRegistration = token.Register(static state => ((SceneOperation)state).Cancel(), this);
            return this;
        }

        /// <summary>
        /// Makes <c>await operation</c> work.
        /// <br/>
        /// The awaiter is hand-rolled over this operation's continuation list — no
        /// <c>Task</c>, no <c>Awaitable</c>. Because <see cref="SceneOperationPump"/> runs on
        /// the player loop, continuations resume on the main thread by construction, with no
        /// <c>SynchronizationContext</c> round-trip. It is also re-awaitable: awaiting the same
        /// operation twice, or from two places, both work.
        /// </summary>
        public SceneOperationAwaiter GetAwaiter() => new(this);

        /// <summary>
        /// Bridges to <see cref="Task"/> for third-party interop — <c>Task</c>-typed APIs,
        /// <c>ContinueWith</c>, UniTask.
        /// <br/>
        /// A convenience, not the primary path: it costs a <see cref="TaskCompletionSource{T}"/>
        /// per call, and <c>await operation</c> costs nothing. Prefer the latter.
        /// </summary>
        public Task<SceneResult> AsTask()
        {
            TaskCompletionSource<SceneResult> completion = new();

            Completed += operation =>
            {
                switch (operation.State)
                {
                    case SceneOperationState.Canceled:
                        completion.TrySetCanceled();
                        break;
                    case SceneOperationState.Faulted:
                        completion.TrySetException(operation.Exception);
                        break;
                    default:
                        completion.TrySetResult(operation.Result);
                        break;
                }
            };

            return completion.Task;
        }

        /// <summary>
        /// Waits for this operation from a coroutine: <c>yield return operation.ToCoroutine()</c>.
        /// <br/>
        /// Replaces v4's <c>WaitTask&lt;T&gt;</c>. Faults are rethrown when the coroutine
        /// resumes; a cancellation simply ends the wait.
        /// </summary>
        public IEnumerator ToCoroutine()
        {
            while (!IsDone)
                yield return null;

            if (State == SceneOperationState.Faulted)
                throw Exception;
        }

        /// <summary>
        /// One operation that finishes when all of <paramref name="operations"/> have.
        /// <br/>
        /// Its <see cref="Result"/> is every scene from every operation, in order. Prefer this
        /// over <c>Task.WhenAll</c> on <see cref="AsTask"/>: it works over the same continuation
        /// lists and costs no <see cref="TaskCompletionSource{T}"/> per operation.
        /// </summary>
        public static SceneOperation WhenAll(params SceneOperation[] operations) => Combine(operations, requireAll: true);

        /// <summary>
        /// One operation that finishes as soon as any of <paramref name="operations"/> does.
        /// <br/>
        /// Its <see cref="Result"/> is the winner's. The others keep running.
        /// </summary>
        public static SceneOperation WhenAny(params SceneOperation[] operations) => Combine(operations, requireAll: false);

        public override string ToString() => $"{Kind} operation ({State}, {Progress:P0})";

        static SceneOperation Combine(SceneOperation[] operations, bool requireAll)
        {
            if (operations == null || operations.Length == 0)
                throw new ArgumentException("Cannot combine a null or empty set of operations.", nameof(operations));

            SceneOperation composite = new(SceneOperationKind.Composite);
            int remaining = operations.Length;
            bool settled = false;

            foreach (SceneOperation operation in operations)
            {
                operation.Completed += onCompleted;
            }

            return composite;

            void onCompleted(SceneOperation operation)
            {
                if (settled)
                    return;

                if (operation.State == SceneOperationState.Faulted)
                {
                    settled = true;
                    composite.Fault(operation.Exception);
                    return;
                }

                if (!requireAll)
                {
                    settled = true;
                    composite.Complete(operation.Result);
                    return;
                }

                if (--remaining > 0)
                    return;

                settled = true;
                composite.Complete(CombineResults(operations));
            }
        }

        static SceneResult CombineResults(SceneOperation[] operations)
        {
            int total = 0;
            foreach (SceneOperation operation in operations)
                total += operation.Result.GetScenes()?.Length ?? 0;

            Scene[] scenes = new Scene[total];
            int index = 0;
            foreach (SceneOperation operation in operations)
            {
                Scene[] operationScenes = operation.Result.GetScenes();
                if (operationScenes == null)
                    continue;

                Array.Copy(operationScenes, 0, scenes, index, operationScenes.Length);
                index += operationScenes.Length;
            }

            return new SceneResult(scenes);
        }

        internal void SetState(SceneOperationState state)
        {
            if (State == state)
                return;

            State = state;

            if (SceneManagerLog.IsEnabled(SceneLogLevel.Verbose))
                SceneManagerLog.Verbose($"{Kind} operation entered {state}.");

            StateChanged?.Invoke(this);
        }

        internal void ReportProgress(float progress)
        {
            if (IsDone || Math.Abs(progress - Progress) < ProgressEpsilon)
                return;

            Progress = progress;
            Progressed?.Invoke(progress);
        }

        internal void ReportSceneLoaded(Scene scene) => SceneLoaded?.Invoke(scene);

        internal void ReportSceneUnloaded(Scene scene) => SceneUnloaded?.Invoke(scene);

        internal void Complete(SceneResult result)
        {
            if (IsDone)
                return;

            Result = result;
            ReportProgress(1f);
            SetState(SceneOperationState.Completed);
            Finish();
        }

        internal void Fault(Exception exception)
        {
            if (IsDone)
                return;

            Exception = exception ?? new Exception("The operation faulted without an exception.");

            if (SceneManagerLog.IsEnabled(SceneLogLevel.Error))
                SceneManagerLog.Error($"{Kind} operation faulted during {State}: {Exception.Message}");

            SetState(SceneOperationState.Faulted);
            Finish();
        }

        /// <summary>
        /// Registers a continuation, or runs it immediately if the operation already finished.
        /// This is what makes the awaiter re-awaitable.
        /// </summary>
        internal void AddContinuation(Action continuation)
        {
            if (IsDone)
            {
                continuation();
                return;
            }

            _continuations += continuation;
        }

        /// <summary>
        /// The result an awaiter hands back, rethrowing a fault the way <c>Task</c> would.
        /// </summary>
        internal SceneResult GetResultOrThrow()
        {
            return State switch
            {
                SceneOperationState.Faulted => throw Exception,
                SceneOperationState.Canceled => throw new OperationCanceledException($"The {Kind} operation was canceled."),
                _ => Result,
            };
        }

        void Finish()
        {
            _cancellationRegistration.Dispose();
            _cancellationRegistration = default;

            Action<SceneOperation> completed = _completed;
            _completed = null;
            completed?.Invoke(this);

            Action continuations = _continuations;
            _continuations = null;
            continuations?.Invoke();
        }
    }
}
