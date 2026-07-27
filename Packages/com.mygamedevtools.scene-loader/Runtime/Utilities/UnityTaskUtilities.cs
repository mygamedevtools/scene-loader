using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.LowLevel;

namespace MyGameDevTools.SceneLoading
{
    public static partial class UnityTaskUtilities
    {
        static Queue<Action> Actions;

        // Statics survive a disabled Domain Reload, so don't reuse the previous session's queue.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_6000_5_OR_NEWER
        [OnExitingPlayMode]
#endif
        static void ResetStatics()
        {
            Actions = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void HookToPlayerLoop()
        {
            if (Actions != null)
                return;

            Actions = new Queue<Action>(16);

            PlayerLoopSystem playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            List<PlayerLoopSystem> updatedSystems = new(playerLoop.subSystemList);
            // The player loop is native state, so drop a previous session's system instead of stacking.
            updatedSystems.RemoveAll(system => system.type == typeof(UnityTaskUtilities));
            updatedSystems.Add(new PlayerLoopSystem
            {
                type = typeof(UnityTaskUtilities),
                updateDelegate = ProcessMainThreadQueue
            });

            playerLoop.subSystemList = updatedSystems.ToArray();
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        public static Task FromAsyncOperation(IAsyncSceneOperation asyncSceneOperation, CancellationToken token = default)
        {
            TaskCompletionSource<bool> tcs = new();

            token.Register(() =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.TrySetCanceled(token);
                }
            });

            Enqueue(() =>
            {
                if (tcs.Task.IsCanceled || tcs.Task.IsFaulted)
                    return;

                if (asyncSceneOperation.IsDone)
                {
                    tcs.SetResult(true);
                    return;
                }

                asyncSceneOperation.Completed += () =>
                {
                    tcs.TrySetResult(true);
                };
            });

            return tcs.Task;
        }

        static void Enqueue(Action action)
        {
            lock (Actions)
            {
                Actions.Enqueue(action);
            }
        }

        static void ProcessMainThreadQueue()
        {
            // A previous session's system can tick before the queue is rebuilt.
            if (Actions == null)
                return;

            lock (Actions)
            {
                while (Actions.TryDequeue(out Action action))
                {
                    action.Invoke();
                }
            }
        }
    }
}