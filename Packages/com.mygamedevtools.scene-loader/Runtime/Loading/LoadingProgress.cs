using System;
using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Reports loading progress to a loading screen, and gates the transition on that screen
    /// being ready.
    /// </summary>
    /// <remarks>
    /// Three v4 problems are fixed here: the two gates were public
    /// <c>TaskCompletionSource</c> fields any consumer could complete, calling
    /// <see cref="StartTransition"/> twice threw, and a gate nobody opened hung forever and
    /// silently. The gates now only observe, are idempotent, and name what is blocking them
    /// after <see cref="SceneOperationPump.GateWarningSeconds"/>.
    /// </remarks>
    public class LoadingProgress : IProgress<float>
    {
        /// <summary>
        /// Reports when the scene loading progress increases. Values range from 0 to 1.
        /// </summary>
        public event Action<float> Progressed;
        /// <summary>The loading screen's cue to start hiding itself.</summary>
        public event Action LoadingCompleted;

        /// <summary>Whether the screen has finished showing and the transition may proceed.</summary>
        public bool IsShown { get; private set; }

        /// <summary>Whether the screen has finished hiding and the transition may finish.</summary>
        public bool IsHidden { get; private set; }

        /// <summary>What is driving this, named when a gate blocks. Optional.</summary>
        internal string OwnerDescription { get; set; }

        /// <summary>Marks the screen as fully shown. Safe to call more than once.</summary>
        public void StartTransition()
        {
            IsShown = true;
        }

        /// <summary>Marks the screen as fully hidden. Safe to call more than once.</summary>
        public void EndTransition()
        {
            IsHidden = true;
        }

        /// <summary>Announces that loading has finished, so the screen can begin hiding.</summary>
        public void SetLoadingCompleted()
        {
            LoadingCompleted?.Invoke();
        }

        /// <summary>Waits until <see cref="StartTransition"/> has been called.</summary>
        public SceneOperationPump.ConditionAwaiter WaitForShowAsync(SceneOperation operation = null)
        {
            _isShown ??= () => IsShown;
            return SceneOperationPump.WaitUntil(_isShown, operation, Describe(nameof(StartTransition)));
        }

        /// <summary>Waits until <see cref="EndTransition"/> has been called.</summary>
        public SceneOperationPump.ConditionAwaiter WaitForHideAsync(SceneOperation operation = null)
        {
            _isHidden ??= () => IsHidden;
            return SceneOperationPump.WaitUntil(_isHidden, operation, Describe(nameof(EndTransition)));
        }

        /// <summary>
        /// <see cref="IProgress{T}"/> implementation. Reports the scene loading progress value, ranging from 0 to 1.
        /// </summary>
        /// <param name="value">Scene loading progress value, ranging from 0 to 1.</param>
        public void Report(float value)
        {
            Progressed?.Invoke(Mathf.Clamp01(value));
        }

        // Cached so awaiting a gate does not allocate a closure per frame or per transition.
        Func<bool> _isShown;
        Func<bool> _isHidden;

        string Describe(string openedBy) => $"{openedBy}() on {OwnerDescription ?? "a " + nameof(LoadingBehavior)}";
    }
}
