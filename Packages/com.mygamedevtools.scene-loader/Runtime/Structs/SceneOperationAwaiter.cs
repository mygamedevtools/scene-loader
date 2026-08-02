using System;
using System.Runtime.CompilerServices;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Makes <c>await operation</c> work, over a <see cref="SceneOperation"/>'s own continuation
    /// list rather than a <c>Task</c> or an <c>Awaitable</c>.
    /// <br/><br/>
    /// Because <see cref="SceneOperationPump"/> runs on the player loop, a continuation resumes
    /// on the main thread by construction — no <c>SynchronizationContext</c> round-trip, which
    /// is faster than either of the alternatives.
    /// <br/><br/>
    /// It is a struct with no state of its own beyond the operation, which is what makes it
    /// re-awaitable: awaiting the same operation twice, or from two places, just registers two
    /// continuations. <c>Awaitable</c> cannot do that — its objects return to a pool after a
    /// single await — and that is precisely why it is not used here.
    /// </summary>
    public readonly struct SceneOperationAwaiter : INotifyCompletion
    {
        /// <summary>
        /// Whether the operation has already finished, in which case <c>await</c> does not
        /// suspend at all.
        /// </summary>
        public readonly bool IsCompleted => _operation.IsDone;

        readonly SceneOperation _operation;

        internal SceneOperationAwaiter(SceneOperation operation)
        {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
        }

        public readonly void OnCompleted(Action continuation) => _operation.AddContinuation(continuation);

        /// <summary>
        /// The scenes the operation produced, rethrowing a fault and turning a cancellation into
        /// an <see cref="OperationCanceledException"/>, the way awaiting a <c>Task</c> would.
        /// </summary>
        public readonly SceneResult GetResult() => _operation.GetResultOrThrow();
    }
}
