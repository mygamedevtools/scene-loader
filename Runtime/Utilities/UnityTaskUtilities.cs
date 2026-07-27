using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.LowLevel;

namespace MyGameDevTools.SceneLoading
{
    public static class UnityTaskUtilities
    {
        static Queue<Action> Actions;

        /// <summary>
        /// Clears the static state before anything else runs, so that a queue left over from the
        /// previous play mode session is not reused when <b>Domain Reload</b> is disabled.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
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
            // The player loop is native state that survives a disabled Domain Reload, so drop the
            // system a previous session registered instead of queueing up another one.
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
            // A system registered by a previous session can tick between the reset above and
            // HookToPlayerLoop rebuilding the queue.
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