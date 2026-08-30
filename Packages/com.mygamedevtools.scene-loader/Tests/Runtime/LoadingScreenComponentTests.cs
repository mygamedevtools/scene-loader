using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// How a <see cref="LoadingScreenComponent"/> finds the behaviour it drives, and when it is
    /// allowed to act on it.
    /// </summary>
    public class LoadingScreenComponentTests
    {
        GameObject _root;

        [TearDown]
        public void Teardown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        /// <summary>
        /// The usual authored layout — one behaviour on the screen's root, components below it —
        /// needs no wiring in the Inspector at all.
        /// </summary>
        [UnityTest]
        public IEnumerator Binds_ToTheClosestBehaviorUpTheHierarchy()
        {
            _root = new GameObject("screen");
            LoadingBehavior behavior = _root.AddComponent<LoadingBehavior>();

            GameObject child = new("feedback");
            child.transform.SetParent(_root.transform);
            RecordingComponent component = child.AddComponent<RecordingComponent>();

            yield return null;

            Assert.True(component.Bound, "It should have resolved the behaviour from its parent.");
            Assert.AreSame(behavior.Progress, component.BoundProgress);
        }

        /// <summary>
        /// An explicit reference wins, and assigning it right after <c>AddComponent</c> still
        /// takes effect — the shape every test and every runtime-built screen uses.
        /// </summary>
        [UnityTest]
        public IEnumerator Binds_ToAnExplicitlyAssignedBehavior()
        {
            _root = new GameObject("screen");
            LoadingBehavior behavior = _root.AddComponent<LoadingBehavior>();

            GameObject elsewhere = new("elsewhere");
            elsewhere.transform.SetParent(_root.transform);
            RecordingComponent component = elsewhere.AddComponent<RecordingComponent>();
            component.LoadingBehavior = behavior;

            Assert.True(component.Bound, "Assigning the reference should bind immediately in play mode.");

            yield return null;

            Assert.AreEqual(1, component.BindCount, "Binding should happen once, not again in Start.");
        }

        /// <summary>
        /// A component that resolves nothing says so and stands down, rather than throwing from
        /// a lifecycle method every frame.
        /// </summary>
        [UnityTest]
        public IEnumerator WithNoBehaviorAnywhere_DisablesItselfAndReports()
        {
            _root = new GameObject("orphan");
            RecordingComponent component = _root.AddComponent<RecordingComponent>();

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("found no LoadingBehavior"));

            yield return null;

            Assert.False(component.Bound);
            Assert.False(component.enabled, "An unresolvable component should disable itself.");
        }

        /// <summary>
        /// Records what it was given, and asserts that it was given it at a moment when
        /// <c>Awake</c> has already run — which is what <see cref="LoadingScreenComponent"/>
        /// promises, and what an editor script assigning the reference used to break.
        /// </summary>
        class RecordingComponent : LoadingScreenComponent
        {
            public bool Bound;
            public int BindCount;
            public LoadingProgress BoundProgress;

            bool _awoke;

            protected override void Awake()
            {
                _awoke = true;
                base.Awake();
            }

            protected override void OnBound()
            {
                Assert.True(_awoke, "OnBound must never run before Awake has cached the subclass' state.");

                Bound = true;
                BindCount++;
                BoundProgress = Progress;
            }
        }
    }
}
