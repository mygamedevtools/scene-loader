using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.LowLevel;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// A player-loop hook that turns backend operations into awaitable tasks.
    /// </summary>
    /// <remarks>
    /// <see cref="ISceneBackend"/> deliberately has no completion event, so this polls once per
    /// frame instead of bridging through one — which drops a delegate, a <c>token.Register</c>
    /// closure and a queued <c>Action</c> closure per scene.
    /// </remarks>
    public static partial class UnityTaskUtilities
    {
        struct PendingOperation
        {
            public SceneBackendHandle Handle;
            public TaskCompletionSource<bool> Completion;
            public CancellationToken Token;
        }

        static List<PendingOperation> Pending;

        // Statics survive a disabled Domain Reload, so don't reuse the previous session's list.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_6000_5_OR_NEWER
        [OnExitingPlayMode]
#endif
        static void ResetStatics()
        {
            Pending = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void HookToPlayerLoop()
        {
            if (Pending != null)
                return;

            Pending = new List<PendingOperation>(16);

            PlayerLoopSystem playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            List<PlayerLoopSystem> updatedSystems = new(playerLoop.subSystemList);
            // The player loop is native state, so drop a previous session's system instead of stacking.
            updatedSystems.RemoveAll(system => system.type == typeof(UnityTaskUtilities));
            updatedSystems.Add(new PlayerLoopSystem
            {
                type = typeof(UnityTaskUtilities),
                updateDelegate = Tick
            });

            playerLoop.subSystemList = updatedSystems.ToArray();
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        /// <summary>Completes when the handle's operation finishes, or cancels with the token.</summary>
        public static Task FromBackendHandle(SceneBackendHandle handle, CancellationToken token = default)
        {
            TaskCompletionSource<bool> completion = new();

            if (token.IsCancellationRequested)
            {
                completion.SetCanceled();
                return completion.Task;
            }

            if (handle.Backend.IsDone(handle))
            {
                completion.SetResult(true);
                return completion.Task;
            }

            Pending ??= new List<PendingOperation>(16);
            Pending.Add(new PendingOperation
            {
                Handle = handle,
                Completion = completion,
                Token = token,
            });

            return completion.Task;
        }

        static void Tick()
        {
            // A previous session's player-loop system can tick before the list is rebuilt.
            if (Pending == null || Pending.Count == 0)
                return;

            for (int i = Pending.Count - 1; i >= 0; i--)
            {
                PendingOperation pending = Pending[i];

                if (pending.Token.IsCancellationRequested)
                {
                    Pending.RemoveAt(i);
                    pending.Completion.TrySetCanceled(pending.Token);
                    continue;
                }

                if (!pending.Handle.Backend.IsDone(pending.Handle))
                    continue;

                Pending.RemoveAt(i);
                pending.Completion.TrySetResult(true);
            }
        }
    }
}
