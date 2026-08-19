using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// A live handle on a scene operation: what phase it is in, how far along it is, what it
    /// produced, and how to wait for it. Returned instead of a <c>Task&lt;SceneResult&gt;</c>,
    /// which is what let <c>IProgress&lt;float&gt;</c> and <c>CancellationToken</c> leave every
    /// public signature — you attach them here instead of deciding up front.
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
    /// <b>Not pooled, deliberately.</b> This API encourages callers to keep the handle —
    /// <c>op.Result</c> after completion and awaiting twice are both supported — so nothing can
    /// know when it is free. Recycling anyway is the same hazard that kept <c>Awaitable</c> out,
    /// traded against one small allocation per scene load. The per-operation buffers are pooled.
    /// </remarks>
    public sealed class SceneOperation
    {
        // Without this the pump would re-raise an unchanged value every frame.
        const float ProgressEpsilon = 0.0001f;

        /// <summary>Which operation this is.</summary>
        public SceneOperationKind Kind { get; }

        /// <summary>The phase this operation is in.</summary>
        public SceneOperationState State { get; private set; }

        /// <summary>
        /// How far the load has got, from 0 to 1. A group mixing backends advances unevenly,
        /// since Addressables includes download time and the standard path does not.
        /// </summary>
        public float Progress { get; private set; }

        /// <summary>The scenes produced, empty until <see cref="SceneOperationState.Completed"/>.</summary>
        public SceneResult Result { get; private set; }

        /// <summary>
        /// Why the operation faulted, or <see langword="null"/>. Only the addressable path can
        /// report a real failure; a standard-path failure surfaces as a link failure instead.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>Whether the operation has finished, successfully or not.</summary>
        public bool IsDone => State >= SceneOperationState.Completed;

        /// <summary>Fires when <see cref="Progress"/> moves. Not raised for unchanged values.</summary>
        public event Action<float> Progressed;
        /// <summary>Fires once per scene loaded.</summary>
        public event Action<Scene> SceneLoaded;
        /// <summary>Fires once per scene unloaded.</summary>
        public event Action<Scene> SceneUnloaded;
        /// <summary>Fires on every <see cref="State"/> change.</summary>
        public event Action<SceneOperation> StateChanged;
        /// <summary>
        /// Fires once when the operation finishes — success, cancellation and fault alike.
        /// Subscribing after completion invokes it immediately.
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

        /// <summary>Whether <see cref="Cancel"/> has been called; phases check this to stop early.</summary>
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
        /// Stops this operation, completing it in <see cref="SceneOperationState.Canceled"/>.
        /// <br/>
        /// <b>The underlying Unity operations keep running</b> — they cannot be aborted, which is
        /// why v4's tokens never cancelled the work either. A scene already loading will finish;
        /// what stops is this operation's reporting, its remaining phases, and its waiters.
        /// </summary>
        public void Cancel()
        {
            if (IsDone || IsCancellationRequested)
                return;

            IsCancellationRequested = true;

            SceneManagerLog.Info($"{Kind} operation cancelled during {State}. The underlying Unity operations will still run to completion.");

            SetState(SceneOperationState.Canceled);
            Finish();
        }

        /// <summary>
        /// Cancels this operation when <paramref name="token"/> is cancelled — the opt-in bridge
        /// for structured concurrency, rather than a parameter on every method.
        /// <code>
        /// MySceneManager.TransitionAsync("target", "loading").CancelWith(destroyCancellationToken);
        /// </code>
        /// </summary>
        /// <returns>This operation, so it chains onto the call that created it.</returns>
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

        /// <summary>Makes <c>await operation</c> work. See <see cref="SceneOperationAwaiter"/>.</summary>
        public SceneOperationAwaiter GetAwaiter() => new(this);

        /// <summary>
        /// Bridges to <see cref="Task"/> for third-party interop. A convenience, not the primary
        /// path: it costs a <see cref="TaskCompletionSource{T}"/> per call and <c>await</c> does not.
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
        /// Waits from a coroutine: <c>yield return operation.ToCoroutine()</c>. Faults rethrow;
        /// a cancellation simply ends the wait.
        /// </summary>
        public IEnumerator ToCoroutine()
        {
            while (!IsDone)
                yield return null;

            if (State == SceneOperationState.Faulted)
                throw Exception;
        }

        /// <summary>
        /// Finishes when all of <paramref name="operations"/> have, with every scene in order.
        /// Cheaper than <c>Task.WhenAll</c> over <see cref="AsTask"/>, which costs a
        /// <see cref="TaskCompletionSource{T}"/> each.
        /// </summary>
        public static SceneOperation WhenAll(params SceneOperation[] operations) => Combine(operations, requireAll: true);

        /// <summary>Finishes as soon as any of <paramref name="operations"/> does; the others keep running.</summary>
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

            // Checked here, unlike the cold sites: every state transition passes through
            // this, and Verbose is off by default.
            if (SceneManagerLog.Level >= SceneLogLevel.Verbose)
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

            SceneManagerLog.Error($"{Kind} operation faulted during {State}: {Exception.Message}");

            SetState(SceneOperationState.Faulted);
            Finish();
        }

        /// <summary>
        /// Registers a continuation, or runs it immediately if already finished — which is what
        /// makes the awaiter re-awaitable.
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

        /// <summary>The result an awaiter hands back, rethrowing a fault the way <c>Task</c> would.</summary>
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
