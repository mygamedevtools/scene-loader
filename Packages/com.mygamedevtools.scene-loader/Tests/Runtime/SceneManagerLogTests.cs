#if !MSM_DISABLE_LOGGING
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneManagerLogTests
    {
        static readonly SceneLogLevel[] _allLevels = new[]
        {
            SceneLogLevel.Off,
            SceneLogLevel.Error,
            SceneLogLevel.Warning,
            SceneLogLevel.Info,
            SceneLogLevel.Verbose,
        };

        static readonly SceneLogLevel[] _emittableLevels = new[]
        {
            SceneLogLevel.Error,
            SceneLogLevel.Warning,
            SceneLogLevel.Info,
            SceneLogLevel.Verbose,
        };

        RecordingLogHandler _handler;
        SceneLogLevel _originalLevel;
        ILogHandler _originalHandler;

        [SetUp]
        public void SetUp()
        {
            _originalLevel = SceneManagerLog.Level;
            _originalHandler = SceneManagerLog.Handler;

            // Everything below routes into the recorder rather than the console, so an
            // intentionally-logged error does not fail the test as an unexpected log.
            _handler = new RecordingLogHandler();
            SceneManagerLog.Handler = _handler;
        }

        [TearDown]
        public void TearDown()
        {
            SceneManagerLog.Handler = _originalHandler;
            SceneManagerLog.Level = _originalLevel;
        }

        [Test]
        public void Level_EmitsItselfAndEverythingMoreSevere([ValueSource(nameof(_allLevels))] SceneLogLevel level)
        {
            SceneManagerLog.Level = level;
            EmitOneOfEach();

            Assert.AreEqual(level >= SceneLogLevel.Error, _handler.Contains(LogType.Error, nameof(SceneLogLevel.Error)));
            Assert.AreEqual(level >= SceneLogLevel.Warning, _handler.Contains(LogType.Warning, nameof(SceneLogLevel.Warning)));
            Assert.AreEqual(level >= SceneLogLevel.Info, _handler.Contains(LogType.Log, nameof(SceneLogLevel.Info)));
            Assert.AreEqual(level >= SceneLogLevel.Verbose, _handler.Contains(LogType.Log, nameof(SceneLogLevel.Verbose)));
        }

        [Test]
        public void Off_EmitsNothing()
        {
            SceneManagerLog.Level = SceneLogLevel.Off;
            EmitOneOfEach();

            Assert.IsEmpty(_handler.Messages);
        }

        [Test]
        public void IsEnabled_AgreesWithWhatIsEmitted([ValueSource(nameof(_allLevels))] SceneLogLevel level)
        {
            SceneManagerLog.Level = level;

            foreach (SceneLogLevel severity in _emittableLevels)
            {
                _handler.Clear();
                Emit(severity);

                Assert.AreEqual(SceneManagerLog.IsEnabled(severity), _handler.Messages.Count == 1,
                    $"IsEnabled({severity}) disagreed with what was emitted at Level = {level}.");
            }
        }

        // Off is a threshold, never a thing to ask about.
        [Test]
        public void IsEnabled_Off_IsNeverEnabled([ValueSource(nameof(_allLevels))] SceneLogLevel level)
        {
            SceneManagerLog.Level = level;
            Assert.False(SceneManagerLog.IsEnabled(SceneLogLevel.Off));
        }

        // The error below never reaches the console. If the substituted handler were ignored it
        // would, and the framework would fail this test on an unexpected error log.
        [Test]
        public void SubstitutedHandler_ReceivesMessages_AndTheDefaultDoesNot()
        {
            SceneManagerLog.Level = SceneLogLevel.Error;
            SceneManagerLog.Error("routed away from the console");

            Assert.AreEqual(1, _handler.Messages.Count);
            Assert.That(_handler.Messages[0].Message, Does.Contain("routed away from the console"));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Handler_AssignedNull_RestoresTheDefaultRatherThanSilencing()
        {
            SceneManagerLog.Handler = null;

            Assert.AreSame(Debug.unityLogger.logHandler, SceneManagerLog.Handler);
        }

        [Test]
        public void ResetStatics_RestoresDefaults()
        {
            SceneManagerLog.Level = SceneLogLevel.Verbose;

            SceneManagerLog.ResetStatics();

            Assert.AreEqual(Debug.isDebugBuild ? SceneLogLevel.Warning : SceneLogLevel.Error, SceneManagerLog.Level);
            Assert.AreSame(Debug.unityLogger.logHandler, SceneManagerLog.Handler);

            // Put the recorder back so the teardown's console isolation still holds.
            SceneManagerLog.Handler = _handler;
        }

        // 100,000 unguarded interpolations would allocate megabytes, so a couple of kilobytes is
        // a wide margin that still fails loudly if the call-site guard stops working.
        [UnityTest]
        public IEnumerator DisabledLevel_GuardedCallSite_DoesNotAllocate()
        {
            SceneManagerLog.Level = SceneLogLevel.Off;

            ProfilerRecorder recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            if (!recorder.Valid)
            {
                recorder.Dispose();
                Assert.Ignore("The 'GC Allocated In Frame' profiler counter is unavailable on this runtime.");
            }

            // Warm up so the frame under measurement pays no first-call costs.
            GuardedVerboseCalls(1000);
            yield return null;

            long before = recorder.CurrentValue;
            GuardedVerboseCalls(100_000);
            yield return null;
            long allocated = recorder.CurrentValue - before;

            recorder.Dispose();

            Assert.Less(allocated, 2048, $"100,000 guarded verbose calls allocated {allocated:N0} bytes with logging off. " +
                "The call-site guard is not preventing the message from being built.");
        }

        void GuardedVerboseCalls(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (SceneManagerLog.IsEnabled(SceneLogLevel.Verbose))
                    SceneManagerLog.Verbose($"iteration {i} of {count}");
            }
        }

        static void EmitOneOfEach()
        {
            foreach (SceneLogLevel severity in _emittableLevels)
                Emit(severity);
        }

        static void Emit(SceneLogLevel severity)
        {
            switch (severity)
            {
                case SceneLogLevel.Error:
                    SceneManagerLog.Error(nameof(SceneLogLevel.Error));
                    break;
                case SceneLogLevel.Warning:
                    SceneManagerLog.Warning(nameof(SceneLogLevel.Warning));
                    break;
                case SceneLogLevel.Info:
                    SceneManagerLog.Info(nameof(SceneLogLevel.Info));
                    break;
                case SceneLogLevel.Verbose:
                    SceneManagerLog.Verbose(nameof(SceneLogLevel.Verbose));
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(severity), severity, "Not an emittable level.");
            }
        }

        class RecordingLogHandler : ILogHandler
        {
            public readonly List<(LogType Type, string Message)> Messages = new();

            public void Clear() => Messages.Clear();

            public bool Contains(LogType type, string fragment)
            {
                foreach ((LogType messageType, string message) in Messages)
                    if (messageType == type && message.Contains(fragment))
                        return true;
                return false;
            }

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                Messages.Add((logType, string.Format(format, args)));
            }

            public void LogException(System.Exception exception, Object context)
            {
                Messages.Add((LogType.Exception, exception.Message));
            }
        }
    }
}
#endif
