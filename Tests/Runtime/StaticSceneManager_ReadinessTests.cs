using System;
using NUnit.Framework;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// Asking whether the static manager exists yet, without having to catch an exception to
    /// find out.
    /// </summary>
    /// <remarks>
    /// There are two ordinary windows with no manager: before the first scene has finished
    /// loading — every <c>Awake</c> and <c>OnEnable</c> in it, since
    /// <c>[RuntimeInitializeOnLoadMethod]</c> runs after that — and after play mode has torn the
    /// statics down, which is where an <c>OnDestroy</c> unsubscribing from manager events lands.
    /// </remarks>
    public class StaticSceneManager_ReadinessTests
    {
        ISceneManager _original;

        [SetUp]
        public void Setup() => MySceneManager.TryGetDefault(out _original);

        [TearDown]
        public void Teardown() => MySceneManager.Default = _original;

        [Test]
        public void TryGetDefault_HandsBackTheManager_WhenThereIsOne()
        {
            Assert.True(MySceneManager.TryGetDefault(out ISceneManager manager));
            Assert.NotNull(manager);
            Assert.AreSame(MySceneManager.Default, manager);
        }

        [Test]
        public void TryGetDefault_ReportsFalse_WithNoManager()
        {
            MySceneManager.Default = null;

            Assert.False(MySceneManager.TryGetDefault(out ISceneManager manager));
            Assert.IsNull(manager, "The out parameter should not be left holding a stale manager.");
        }

        /// <summary>
        /// The condition is "not initialised yet", not "you dereferenced null".
        /// <see cref="NullReferenceException"/> sent people hunting for a null variable in their
        /// own code; this one is catchable and says what actually happened.
        /// </summary>
        [Test]
        public void Default_WithNoManager_ThrowsInvalidOperation()
        {
            MySceneManager.Default = null;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => _ = MySceneManager.Default);
            Assert.That(exception.Message, Does.Contain(nameof(MySceneManager.TryGetDefault)),
                "The message should point at the way to ask without throwing.");
        }
    }
}
