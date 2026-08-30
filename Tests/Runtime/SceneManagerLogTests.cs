using System.Collections.Generic;
using NUnit.Framework;
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
