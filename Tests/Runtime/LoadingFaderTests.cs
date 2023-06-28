/**
 * LoadingFaderTests.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2023-02-01
 */

using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class LoadingFaderTests
    {
        [OneTimeTearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(Object.FindObjectOfType<LoadingBehavior>().gameObject);
            Object.DestroyImmediate(Object.FindObjectOfType<LoadingFader>().gameObject);
        }

        [UnityTest]
        public IEnumerator FadeInOut()
        {
            var loadingBehavior = new GameObject().AddComponent<LoadingBehavior>();
            var progress = loadingBehavior.Progress;

            loadingBehavior.waitForScriptedStart = true;
            loadingBehavior.waitForScriptedEnd = true;

            var loadingFader = new GameObject("Fader", typeof(CanvasGroup)).AddComponent<LoadingFader>();
            var canvasGroup = loadingFader.GetComponent<CanvasGroup>();

            loadingFader.fadeTime = .2f;
            loadingFader.loadingBehavior = loadingBehavior;

            Assert.AreEqual(LoadingState.WaitingToStart, progress.State);
            Assert.AreEqual(0, canvasGroup.alpha);
            yield return new WaitForSeconds(loadingFader.fadeTime * 2);

            Assert.AreEqual(LoadingState.Loading, progress.State);
            Assert.AreEqual(1, canvasGroup.alpha);

            progress.SetState(LoadingState.TargetSceneLoaded);

            yield return new WaitForSeconds(loadingFader.fadeTime * 2);

            Assert.AreEqual(LoadingState.TransitionComplete, progress.State);
            Assert.AreEqual(0, canvasGroup.alpha);
        }
    }
}