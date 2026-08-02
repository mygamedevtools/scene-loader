using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.LowLevel;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Ticks every in-flight backend operation once per frame, from the player loop. One pump,
    /// one pass, and <see cref="SceneOperation.Progressed"/> only fires when there is something
    /// to report — where v4 polled once per frame <i>per group</i> through the
    /// <c>SynchronizationContext</c>.
    /// <br/>
    /// Running on the player loop is also what makes <see cref="SceneOperationAwaiter"/> honest:
    /// continuations resume on the main thread because that is where the pump ticks.
    /// </summary>
    public static partial class SceneOperationPump
    {
        struct Entry
        {
            public SceneBackendHandle[] Handles;
            public SceneOperation Operation;
            public Action Continuation;
            public float Waited;
            public bool Warned;
        }

        struct ConditionEntry
        {
            public Func<bool> Condition;
            public SceneOperation Operation;
            public Action Continuation;
            public string Description;
            public float Waited;
            public bool Warned;
        }

        /// <summary>How long a gate waits before naming what blocks it. It then keeps waiting.</summary>
        public const float GateWarningSeconds = 10f;

        static List<Entry> _entries;
        static List<ConditionEntry> _conditionEntries;

        /// <summary>
        /// Completes when every handle is done, reporting their average progress to
        /// <paramref name="operation"/> meanwhile.
        /// </summary>
        public static BackendGroupAwaiter WaitForAll(SceneBackendHandle[] handles, SceneOperation operation)
        {
            return new BackendGroupAwaiter(handles, operation);
        }

        /// <summary>
        /// An awaitable over a per-frame condition — the loading-screen gates, in practice.
        /// </summary>
        /// <param name="description">
        /// Named in the development-build warning if the wait runs long, so a gate that never
        /// opens stops being a silent hang.
        /// </param>
        public static ConditionAwaiter WaitUntil(Func<bool> condition, SceneOperation operation, string description)
        {
            return new ConditionAwaiter(condition, operation, description);
        }

        /// <summary>
        /// An awaitable that is already done — for a gate with nothing to wait on, so callers
        /// can return one unconditionally instead of branching on null.
        /// </summary>
        public static ConditionAwaiter Completed(SceneOperation operation)
        {
            return new ConditionAwaiter(AlwaysTrue, operation, "nothing");
        }

        static readonly Func<bool> AlwaysTrue = () => true;

        /// <summary>
        /// Yields once, resuming on the next pump tick. For the rare place that has to poll an
        /// engine operation no backend owns — the holder scene's unload.
        /// </summary>
        public static ConditionAwaiter NextFrame()
        {
            bool firstCheck = true;
            return new ConditionAwaiter(Elapsed, null, "the next frame");

            bool Elapsed()
            {
                if (!firstCheck)
                    return true;

                firstCheck = false;
                return false;
            }
        }

        internal static void Track(SceneBackendHandle[] handles, SceneOperation operation, Action continuation)
        {
            _entries ??= new List<Entry>(8);
            _entries.Add(new Entry
            {
                Handles = handles,
                Operation = operation,
                Continuation = continuation,
            });
        }

        internal static void TrackCondition(Func<bool> condition, SceneOperation operation, string description, Action continuation)
        {
            _conditionEntries ??= new List<ConditionEntry>(4);
            _conditionEntries.Add(new ConditionEntry
            {
                Condition = condition,
                Operation = operation,
                Continuation = continuation,
                Description = description,
            });
        }

        // Statics survive a disabled Domain Reload, so don't carry a previous session's entries —
        // their handles point at native scenes that no longer exist.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_6000_5_OR_NEWER
        [OnExitingPlayMode]
#endif
        internal static void ResetStatics()
        {
            _entries = null;
            _conditionEntries = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void HookToPlayerLoop()
        {
            if (_entries != null)
                return;

            _entries = new List<Entry>(8);

            PlayerLoopSystem playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            List<PlayerLoopSystem> updatedSystems = new(playerLoop.subSystemList);
            // The player loop is native state, so drop a previous session's system instead of stacking.
            updatedSystems.RemoveAll(system => system.type == typeof(SceneOperationPump));
            updatedSystems.Add(new PlayerLoopSystem
            {
                type = typeof(SceneOperationPump),
                updateDelegate = Tick
            });

            playerLoop.subSystemList = updatedSystems.ToArray();
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        static string Describe(SceneBackendHandle[] handles)
        {
            string[] descriptions = new string[handles.Length];
            for (int i = 0; i < handles.Length; i++)
                descriptions[i] = $"{handles[i]}{(handles[i].Backend.IsDone(handles[i]) ? "" : " (still running)")}";

            return string.Join(", ", descriptions);
        }

        static void Tick()
        {
            TickBackendGroups();
            TickConditions();
        }

        static void TickConditions()
        {
            if (_conditionEntries == null || _conditionEntries.Count == 0)
                return;

            for (int i = _conditionEntries.Count - 1; i >= 0; i--)
            {
                ConditionEntry entry = _conditionEntries[i];

                bool canceled = entry.Operation != null && entry.Operation.IsCancellationRequested;
                if (!canceled && !entry.Condition())
                {
                    entry.Waited += Time.unscaledDeltaTime;

                    // A gate nobody ever opens used to hang the transition forever, silently.
                    if (!entry.Warned && entry.Waited >= GateWarningSeconds && Debug.isDebugBuild)
                    {
                        entry.Warned = true;
                        if (SceneManagerLog.IsEnabled(SceneLogLevel.Warning))
                            SceneManagerLog.Warning($"A transition has been waiting {GateWarningSeconds:0} seconds for {entry.Description}. It will keep waiting, but something is expected to release it.");
                    }

                    _conditionEntries[i] = entry;
                    continue;
                }

                _conditionEntries.RemoveAt(i);
                entry.Continuation();
            }
        }

        static void TickBackendGroups()
        {
            // A previous session's player-loop system can tick before the list is rebuilt.
            if (_entries == null || _entries.Count == 0)
                return;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry entry = _entries[i];

                bool canceled = entry.Operation != null && entry.Operation.IsCancellationRequested;
                if (!canceled)
                {
                    entry.Operation?.ReportProgress(SceneLinker.GetAverageProgress(entry.Handles));

                    if (!SceneLinker.HasCompletedAll(entry.Handles))
                    {
                        // An engine operation that never reports itself done would otherwise be an
                        // unexplained freeze. Say what is stuck, once, and keep waiting.
                        entry.Waited += Time.unscaledDeltaTime;
                        if (!entry.Warned && entry.Waited >= GateWarningSeconds && Debug.isDebugBuild)
                        {
                            entry.Warned = true;
                            if (SceneManagerLog.IsEnabled(SceneLogLevel.Warning))
                                SceneManagerLog.Warning($"A {entry.Operation?.Kind.ToString() ?? "scene"} operation has been waiting {GateWarningSeconds:0} seconds on {Describe(entry.Handles)}.");
                        }

                        _entries[i] = entry;
                        continue;
                    }
                }

                // Remove before resuming: the continuation runs the next phase, which may add
                // entries of its own.
                _entries.RemoveAt(i);
                entry.Continuation();
            }
        }

        /// <summary>Awaits a group of backend operations through the pump.</summary>
        public readonly struct BackendGroupAwaiter : INotifyCompletion
        {
            /// <summary>
            /// Whether the group has finished. A cancelled operation counts as finished — its
            /// remaining phases are skipped, even though the engine keeps loading underneath.
            /// </summary>
            public readonly bool IsCompleted => (_operation != null && _operation.IsCancellationRequested) || SceneLinker.HasCompletedAll(_handles);

            readonly SceneBackendHandle[] _handles;
            readonly SceneOperation _operation;

            internal BackendGroupAwaiter(SceneBackendHandle[] handles, SceneOperation operation)
            {
                _handles = handles;
                _operation = operation;
            }

            public readonly BackendGroupAwaiter GetAwaiter() => this;

            public readonly void OnCompleted(Action continuation) => Track(_handles, _operation, continuation);

            public readonly void GetResult() { }
        }

        /// <summary>Awaits a per-frame condition through the pump.</summary>
        public readonly struct ConditionAwaiter : INotifyCompletion
        {
            /// <summary>Whether the condition already holds, or the operation was cancelled.</summary>
            public readonly bool IsCompleted => (_operation != null && _operation.IsCancellationRequested) || _condition();

            readonly Func<bool> _condition;
            readonly SceneOperation _operation;
            readonly string _description;

            internal ConditionAwaiter(Func<bool> condition, SceneOperation operation, string description)
            {
                _condition = condition ?? throw new ArgumentNullException(nameof(condition));
                _operation = operation;
                _description = description;
            }

            public readonly ConditionAwaiter GetAwaiter() => this;

            public readonly void OnCompleted(Action continuation) => TrackCondition(_condition, _operation, _description, continuation);

            public readonly void GetResult() { }
        }
    }
}
