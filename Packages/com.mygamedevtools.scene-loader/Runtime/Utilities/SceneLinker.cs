using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Works out which loaded scene belongs to which handle.
    /// <br/><br/>
    /// This exists because the Unity Scene Manager will not tell you what an operation loaded.
    /// It is also historically the buggiest part of the package — 3.0.1 was a scene-linking fix
    /// — so every step narrates itself at <see cref="SceneLogLevel.Verbose"/>.
    /// <br/><br/>
    /// The v4 version allocated a <c>List&lt;Scene&gt;</c> and a <c>List&lt;ISceneData&gt;</c>
    /// per load and did O(n·m) linear <c>Remove</c> scans over both. Typical n is under 8, so
    /// this uses pooled arrays with swap-remove instead. The semantics are unchanged: this is a
    /// data-structure rewrite, not an algorithm one.
    /// </summary>
    public static partial class SceneLinker
    {
        // Scratch buffers, reused across operations. Linking runs synchronously start to finish
        // with no awaits in between, so two overlapping operations can never be inside it at
        // once — which is what makes sharing these safe on the main thread.
        static Scene[] _candidateScenes = new Scene[8];
        static int[] _unlinkedHandles = new int[8];
        static int[] _availableHandles = new int[8];

        /// <summary>
        /// Fills in each handle's <see cref="SceneBackendHandle.Scene"/>.
        /// <br/>
        /// Backends that can name their own result do so first; whatever is left is matched
        /// against the scenes that appeared and are not already tracked.
        /// </summary>
        /// <param name="handles">Handles to link, updated in place.</param>
        /// <param name="alreadyTracked">Scenes the manager already owns, excluded from matching.</param>
        /// <exception cref="Exception">A handle could not be linked to any loaded scene.</exception>
        public static void Link(SceneBackendHandle[] handles, IReadOnlyList<SceneBackendHandle> alreadyTracked)
        {
            int handleCount = handles.Length;
            int candidateCount = CollectCandidateScenes(alreadyTracked);
            int unlinkedCount = 0;

            // Pass one: backends that hand back their own scene.
            EnsureCapacity(ref _unlinkedHandles, handleCount);
            for (int i = 0; i < handleCount; i++)
            {
                SceneBackendHandle handle = handles[i];
                if (handle.Backend.TryResolveScene(handle, out Scene scene))
                {
                    handles[i] = handle.WithScene(scene);
                    RemoveCandidate(scene, ref candidateCount);

                    if (SceneManagerLog.IsEnabled(SceneLogLevel.Verbose))
                        SceneManagerLog.Verbose($"Linked '{scene.name}' ({scene.handle}) to {handle.SceneRef} directly.");
                }
                else
                {
                    _unlinkedHandles[unlinkedCount++] = i;
                }
            }

            // Pass two: match what is left against the scenes that appeared.
            for (int i = unlinkedCount - 1; i >= 0 && candidateCount > 0; i--)
            {
                int handleIndex = _unlinkedHandles[i];
                SceneBackendHandle handle = handles[handleIndex];

                for (int c = candidateCount - 1; c >= 0; c--)
                {
                    Scene candidate = _candidateScenes[c];
                    if (!handle.SceneRef.CanBeReferenceToScene(candidate))
                        continue;

                    handles[handleIndex] = handle.WithScene(candidate);
                    SwapRemove(_candidateScenes, c, --candidateCount);
                    SwapRemove(_unlinkedHandles, i, --unlinkedCount);

                    if (SceneManagerLog.IsEnabled(SceneLogLevel.Verbose))
                        SceneManagerLog.Verbose($"Linked '{candidate.name}' ({candidate.handle}) to {handle.SceneRef} by matching.");
                    break;
                }
            }

            if (unlinkedCount == 0)
                return;

            string unlinked = DescribeUnlinked(handles, unlinkedCount);
            if (SceneManagerLog.IsEnabled(SceneLogLevel.Error))
                SceneManagerLog.Error($"Unable to link {unlinkedCount}/{handleCount} scenes to a loaded scene: {unlinked}.");

            throw new Exception($"Unable to link all scenes to loaded scenes. Linked {handleCount - unlinkedCount}/{handleCount}. Unlinked: {unlinked}.");
        }

        /// <summary>
        /// Finds the tracked handles matching a group of references, in the same order.
        /// </summary>
        /// <exception cref="Exception">A reference matched no tracked scene.</exception>
        public static SceneBackendHandle[] GetTrackedHandles(SceneRef[] sceneRefs, IReadOnlyList<SceneBackendHandle> tracked)
        {
            int count = sceneRefs.Length;
            SceneBackendHandle[] matched = new SceneBackendHandle[count];

            // Which tracked handles are still available, so two references pointing at the same
            // source scene resolve to two different loaded scenes rather than the same one.
            int trackedCount = tracked.Count;
            EnsureCapacity(ref _availableHandles, trackedCount);
            for (int i = 0; i < trackedCount; i++)
                _availableHandles[i] = i;

            int availableCount = trackedCount;

            for (int i = count - 1; i >= 0; i--)
            {
                SceneRef sceneRef = sceneRefs[i];
                bool found = false;

                for (int a = availableCount - 1; a >= 0; a--)
                {
                    SceneBackendHandle candidate = tracked[_availableHandles[a]];
                    if (!Matches(candidate, sceneRef))
                        continue;

                    matched[i] = candidate;
                    SwapRemove(_availableHandles, a, --availableCount);
                    found = true;
                    break;
                }

                if (found)
                    continue;

                if (SceneManagerLog.IsEnabled(SceneLogLevel.Error))
                    SceneManagerLog.Error($"Unable to find a managed scene matching {sceneRef}. Is the scene loaded?");

                throw new Exception($"Unable to match a managed scene with {sceneRef}.");
            }

            return matched;
        }

        /// <summary>
        /// The scenes behind a group of handles.
        /// </summary>
        public static Scene[] GetScenes(SceneBackendHandle[] handles)
        {
            Scene[] scenes = new Scene[handles.Length];
            for (int i = 0; i < handles.Length; i++)
                scenes[i] = handles[i].Scene;

            return scenes;
        }

        /// <summary>
        /// The average progress of a group of handles, each normalized 0..1 by its own backend.
        /// </summary>
        public static float GetAverageProgress(SceneBackendHandle[] handles)
        {
            float total = 0;
            for (int i = 0; i < handles.Length; i++)
                total += handles[i].Backend.GetProgress(handles[i]);

            return total / handles.Length;
        }

        /// <summary>
        /// Whether every handle's operation has finished.
        /// </summary>
        public static bool HasCompletedAll(SceneBackendHandle[] handles)
        {
            for (int i = 0; i < handles.Length; i++)
                if (!handles[i].Backend.IsDone(handles[i]))
                    return false;

            return true;
        }

        /// <summary>
        /// Whether <paramref name="sceneRef"/> identifies the scene behind <paramref name="handle"/>.
        /// <br/>
        /// The addressable kinds compare references, because an address says nothing about the
        /// resulting scene's name. Everything else matches against the loaded scene.
        /// </summary>
        static bool Matches(SceneBackendHandle handle, SceneRef sceneRef)
        {
            return sceneRef.Kind switch
            {
                SceneRefKind.Address or SceneRefKind.AssetReference => sceneRef.Equals(handle.SceneRef) || sceneRef.CanBeReferenceToScene(handle.Scene),
                _ => sceneRef.CanBeReferenceToScene(handle.Scene),
            };
        }

        static int CollectCandidateScenes(IReadOnlyList<SceneBackendHandle> alreadyTracked)
        {
            int sceneCount = SceneManager.sceneCount;
            EnsureCapacity(ref _candidateScenes, sceneCount);

            int candidateCount = 0;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    _candidateScenes[candidateCount++] = scene;
            }

            for (int i = 0; i < alreadyTracked.Count; i++)
                RemoveCandidate(alreadyTracked[i].Scene, ref candidateCount);

            return candidateCount;
        }

        static void RemoveCandidate(Scene scene, ref int candidateCount)
        {
            for (int i = candidateCount - 1; i >= 0; i--)
            {
                if (_candidateScenes[i] != scene)
                    continue;

                SwapRemove(_candidateScenes, i, --candidateCount);
                return;
            }
        }

        static void SwapRemove<T>(T[] array, int index, int newCount)
        {
            array[index] = array[newCount];
        }

        static void EnsureCapacity<T>(ref T[] buffer, int required)
        {
            if (buffer.Length >= required)
                return;

            int capacity = buffer.Length;
            while (capacity < required)
                capacity *= 2;

            buffer = new T[capacity];
        }

        static string DescribeUnlinked(SceneBackendHandle[] handles, int unlinkedCount)
        {
            string[] descriptions = new string[unlinkedCount];
            for (int i = 0; i < unlinkedCount; i++)
                descriptions[i] = handles[_unlinkedHandles[i]].SceneRef.ToString();

            return string.Join(", ", descriptions);
        }

        // The buffers are static, so a disabled Domain Reload would carry a previous session's
        // Scene structs — pointing at native scenes that no longer exist — into the next one.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_6000_5_OR_NEWER
        [OnExitingPlayMode]
#endif
        internal static void ResetStatics()
        {
            _candidateScenes = new Scene[8];
            _unlinkedHandles = new int[8];
            _availableHandles = new int[8];
        }
    }
}
