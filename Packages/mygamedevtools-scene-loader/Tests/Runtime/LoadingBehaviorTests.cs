/**
 * LoadingBehaviorTests.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2023-01-31
 */

using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class LoadingBehaviorTests
    {
        [OneTimeTearDown]
        public void Teardown()
        {
#if UNITY_2023_2_OR_NEWER
            var behaviors = Object.FindObjectsByType<LoadingBehavior>(FindObjectsSortMode.None);
#else
            var behaviors = Object.FindObjectsOfType<LoadingBehavior>();
#endif
            foreach (var b in behaviors)
                Object.DestroyImmediate(b.gameObject);
        }

        [UnityTest]
        public IEnumerator AutomaticTriggers()
        {
            var behavior = new GameObject().AddComponent<LoadingBehavior>();
            var progress = behavior.Progress;

            Assert.AreEqual(LoadingState.WaitingToStart, progress.State);

            yield return null;

            Assert.AreEqual(LoadingState.Loading, progress.State);

            progress.Report(1);
            progress.SetState(LoadingState.TargetSceneLoaded);

            Assert.AreEqual(LoadingState.TransitionComplete, progress.State);
        }

        [UnityTest]
        public IEnumerator ManualTriggers()
        {
            var behavior = new GameObject().AddComponent<LoadingBehavior>();

            behavior.waitForScriptedStart = true;
            behavior.waitForScriptedEnd = true;

            var progress = behavior.Progress;

            Assert.AreEqual(LoadingState.WaitingToStart, progress.State);

            yield return null;

            Assert.AreEqual(LoadingState.WaitingToStart, progress.State);

            progress.SetState(LoadingState.Loading);
            progress.Report(1);
            progress.SetState(LoadingState.TargetSceneLoaded);

            yield return null;
            Assert.AreEqual(LoadingState.TargetSceneLoaded, progress.State);

            progress.SetState(LoadingState.TransitionComplete);
            Assert.AreEqual(LoadingState.TransitionComplete, progress.State);
        }
    }
}