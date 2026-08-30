using System;
using System.Runtime.CompilerServices;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Makes <c>await operation</c> work, over the operation's own continuation list rather than
    /// a <c>Task</c> or an <c>Awaitable</c>. Continuations resume on the main thread because the
    /// pump runs on the player loop, with no <c>SynchronizationContext</c> round-trip.
    /// <br/>
    /// Holding no state beyond the operation is what makes it re-awaitable — awaiting twice just
    /// registers two continuations. <c>Awaitable</c> cannot do that, which is why it is unused.
    /// </summary>
    public readonly struct SceneOperationAwaiter : INotifyCompletion
    {
        /// <summary>Whether the operation has already finished, in which case <c>await</c> does not suspend.</summary>
        public readonly bool IsCompleted => _operation.IsDone;

        readonly SceneOperation _operation;

        internal SceneOperationAwaiter(SceneOperation operation)
        {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
        }

        public readonly void OnCompleted(Action continuation) => _operation.AddContinuation(continuation);

        /// <summary>
        /// The scenes produced, rethrowing a fault and turning a cancellation into an
        /// <see cref="OperationCanceledException"/>, the way awaiting a <c>Task</c> would.
        /// </summary>
        public readonly SceneResult GetResult() => _operation.GetResultOrThrow();
    }
}
