using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Reports loading progress to a loading screen, and gates the transition on that screen
    /// being ready.
    /// </summary>
    /// <remarks>
    /// The two gates are <b>open unless something is holding them closed</b>. Anything that needs
    /// the transition to wait — a fade, an animation, a script — calls
    /// <see cref="HoldShow"/> or <see cref="HoldHide"/> and releases when it is done, and the gate
    /// opens when the last holder lets go. That is what lets several participants gate the same
    /// transition without any of them having to know about the others.
    /// <br/><br/>
    /// Holds are identified by their owner, so taking one twice and releasing one twice are both
    /// harmless. A holder that is destroyed without releasing is dropped rather than left blocking
    /// forever, and a gate that stays closed names its holders after
    /// <see cref="SceneOperationPump.GateWarningSeconds"/>.
    /// </remarks>
    public class LoadingProgress : IProgress<float>
    {
        /// <summary>
        /// Reports when the scene loading progress increases. Values range from 0 to 1.
        /// </summary>
        public event Action<float> Progressed;
        /// <summary>The loading screen's cue to start hiding itself.</summary>
        public event Action LoadingCompleted;

        /// <summary>Whether nothing is holding the screen from being shown, so the transition may proceed.</summary>
        public bool IsShown => !HasHolders(_showHolders);

        /// <summary>Whether nothing is holding the screen from being hidden, so the transition may finish.</summary>
        public bool IsHidden => !HasHolders(_hideHolders);

        /// <summary>What is driving this, named when a gate blocks. Optional.</summary>
        internal string OwnerDescription { get; set; }

        List<object> _showHolders;
        List<object> _hideHolders;
        List<object> _completionHolders;

        bool _loadingCompleted;
        bool _completionRaised;

        /// <summary>
        /// Keeps the transition waiting before it unloads the scene you came from, until
        /// <paramref name="owner"/> calls <see cref="ReleaseShow"/>.
        /// <br/>
        /// Take the hold in <c>Awake</c> or <c>OnEnable</c>. One taken later may arrive after the
        /// transition has already read the gate.
        /// </summary>
        /// <param name="owner">Identifies the hold, and names it if the gate blocks for too long.</param>
        public void HoldShow(object owner) => Hold(ref _showHolders, owner);

        /// <summary>Releases this owner's <see cref="HoldShow"/>. Safe to call more than once.</summary>
        public void ReleaseShow(object owner) => Release(_showHolders, owner);

        /// <summary>
        /// Keeps the transition waiting after loading finishes, until <paramref name="owner"/>
        /// calls <see cref="ReleaseHide"/> — the screen's chance to play itself out.
        /// </summary>
        /// <param name="owner">Identifies the hold, and names it if the gate blocks for too long.</param>
        public void HoldHide(object owner) => Hold(ref _hideHolders, owner);

        /// <summary>Releases this owner's <see cref="HoldHide"/>. Safe to call more than once.</summary>
        public void ReleaseHide(object owner) => Release(_hideHolders, owner);

        /// <summary>
        /// Delays <see cref="LoadingCompleted"/> — the screen's cue to start hiding — until
        /// <paramref name="owner"/> calls <see cref="ReleaseCompletion"/>.
        /// </summary>
        /// <remarks>
        /// Distinct from <see cref="HoldHide"/>, and the difference matters. Holding the hide
        /// gate delays the <i>transition</i>; the screen has already been told to go, so a fade
        /// runs to completion and leaves nothing on screen for the rest of the wait. Holding
        /// completion delays the cue itself, so the screen stays up and whatever plays it out
        /// starts when it should.
        /// <br/>
        /// This is what a minimum display time wants: a load that finishes in two frames would
        /// otherwise produce a screen that flashes on and off.
        /// </remarks>
        public void HoldCompletion(object owner) => Hold(ref _completionHolders, owner);

        /// <summary>
        /// Releases this owner's <see cref="HoldCompletion"/>, raising
        /// <see cref="LoadingCompleted"/> if loading has finished and nothing else holds it.
        /// </summary>
        public void ReleaseCompletion(object owner)
        {
            Release(_completionHolders, owner);
            RaiseCompletionIfReady();
        }

        /// <summary>
        /// <see cref="IProgress{T}"/> implementation. Reports the scene loading progress value, ranging from 0 to 1.
        /// </summary>
        /// <param name="value">Scene loading progress value, ranging from 0 to 1.</param>
        public void Report(float value)
        {
            Progressed?.Invoke(Mathf.Clamp01(value));
        }

        /// <summary>Waits until nothing holds the show gate.</summary>
        public SceneOperationPump.ConditionAwaiter WaitForShowAsync(SceneOperation operation = null)
        {
            _isShown ??= () => IsShown;
            _describeShow ??= () => Describe(_showHolders, nameof(ReleaseShow));
            return SceneOperationPump.WaitUntil(_isShown, operation, _describeShow);
        }

        /// <summary>Waits until nothing holds the hide gate.</summary>
        public SceneOperationPump.ConditionAwaiter WaitForHideAsync(SceneOperation operation = null)
        {
            _isHidden ??= () => IsHidden;
            _describeHide ??= () => Describe(_hideHolders, nameof(ReleaseHide));
            return SceneOperationPump.WaitUntil(_isHidden, operation, _describeHide);
        }

        /// <summary>
        /// Announces that loading has finished, so the screen can begin hiding. Raised by the
        /// transition, through <see cref="LoadingScreen.HideAsync"/>.
        /// </summary>
        internal void SetLoadingCompleted()
        {
            _loadingCompleted = true;
            RaiseCompletionIfReady();
        }

        /// <summary>
        /// Raises <see cref="LoadingCompleted"/> once, when loading has finished and nothing is
        /// holding the cue back.
        /// </summary>
        void RaiseCompletionIfReady()
        {
            if (_completionRaised || !_loadingCompleted || HasHolders(_completionHolders))
                return;

            _completionRaised = true;
            LoadingCompleted?.Invoke();
        }

        // Cached so awaiting a gate does not allocate a closure per frame or per transition.
        Func<bool> _isShown;
        Func<bool> _isHidden;
        Func<string> _describeShow;
        Func<string> _describeHide;

        void Hold(ref List<object> holders, object owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner), $"A {nameof(LoadingProgress)} hold has to name its owner, so it can be released and reported.");

            holders ??= new List<object>(2);
            if (!Contains(holders, owner))
                holders.Add(owner);
        }

        static void Release(List<object> holders, object owner)
        {
            if (holders == null || owner == null)
                return;

            for (int i = holders.Count - 1; i >= 0; i--)
                if (Matches(holders[i], owner))
                    holders.RemoveAt(i);
        }

        /// <summary>
        /// Whether anything still holds this gate, dropping holders that were destroyed without
        /// releasing — a screen torn down mid-fade should let the transition through, not hang it.
        /// </summary>
        static bool HasHolders(List<object> holders)
        {
            if (holders == null || holders.Count == 0)
                return false;

            for (int i = holders.Count - 1; i >= 0; i--)
            {
                if (!IsDestroyed(holders[i]))
                    continue;

                SceneManagerLog.Warning($"A {holders[i].GetType().Name} was destroyed while holding a loading screen gate. The gate is being released on its behalf.");
                holders.RemoveAt(i);
            }

            return holders.Count > 0;
        }

        static bool Contains(List<object> holders, object owner)
        {
            for (int i = 0; i < holders.Count; i++)
                if (Matches(holders[i], owner))
                    return true;
            return false;
        }

        // Reference equality, deliberately: a hold belongs to the instance that took it, and a
        // value type overriding Equals must not be able to release someone else's hold.
        static bool Matches(object holder, object owner) => ReferenceEquals(holder, owner);

        // `== null` on a UnityEngine.Object is the destroyed check, not a reference comparison.
        static bool IsDestroyed(object holder) => holder is UnityEngine.Object unityObject && unityObject == null;

        string Describe(List<object> holders, string releasedBy)
        {
            if (holders == null || holders.Count == 0)
                return $"a gate on {OwnerDescription ?? "a loading screen"}";

            string names = holders.Count == 1 ? Name(holders[0]) : string.Join(" and ", NamesOf(holders));
            return $"{names} to call {releasedBy}()";
        }

        static string[] NamesOf(List<object> holders)
        {
            string[] names = new string[holders.Count];
            for (int i = 0; i < holders.Count; i++)
                names[i] = Name(holders[i]);
            return names;
        }

        static string Name(object holder)
        {
            if (holder == null)
                return "an unknown holder";

            // Reaching for `name` on a destroyed object throws, and this is called precisely to
            // report one.
            if (IsDestroyed(holder))
                return $"a destroyed {holder.GetType().Name}";

            return holder switch
            {
                Component component => $"{component.GetType().Name} on '{component.name}' (scene '{component.gameObject.scene.name}')",
                UnityEngine.Object unityObject => $"{unityObject.GetType().Name} '{unityObject.name}'",
                _ => holder.GetType().Name,
            };
        }
    }
}
