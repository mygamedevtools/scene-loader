using System;
using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using Unity.PerformanceTesting;
using Unity.Profiling;
using UnityEngine;

namespace MyGameDevTools.SceneLoading.Tests.Performance
{
    /// <summary>
    /// Measurement helper and the single home for every allocation ceiling.
    /// <br/><br/>
    /// Allocations are read through a <see cref="ProfilerRecorder"/> on the engine's
    /// <c>GC Allocated In Frame</c> counter, not through <c>GC.GetTotalAllocatedBytes</c>.
    /// Two reasons, both found by trying: this project targets .NET Standard 2.1, which does
    /// not carry <c>GetTotalAllocatedBytes</c> at all, and Mono's
    /// <c>GC.GetAllocatedBytesForCurrentThread</c> — the netstandard-legal alternative — is
    /// unimplemented on this runtime and returns a flat zero.
    /// <br/><br/>
    /// Despite the counter's name, <see cref="ProfilerRecorder.CurrentValue"/> accumulates
    /// monotonically for the lifetime of the recorder rather than resetting per frame, so a
    /// before/after delta covers an operation spanning any number of frames. The delta does
    /// include whatever else the frame allocated, but the measured idle floor in batchmode
    /// playmode is around 330 bytes per frame — small enough that the operations below stay
    /// legible against it.
    /// <br/><br/>
    /// The ceilings are <b>regression bounds measured in editor playmode</b>, not
    /// "allocations per transition in a shipped game". The editor allocates incidental
    /// per-frame garbage and its accounting differs from a release IL2CPP build, so an
    /// absolute claim needs a built-player run. What these numbers buy is a failing test the
    /// moment a change makes an operation meaningfully more expensive.
    /// <br/><br/>
    /// Keeping them together means a step that improves allocations ratchets them down in one
    /// diff instead of hunting through test files.
    /// </summary>
    public static class AllocationGate
    {
        /// <summary>
        /// The engine counter reporting managed allocation bytes.
        /// </summary>
        const string GcAllocCounter = "GC Allocated In Frame";

        /// <summary>
        /// Iterations thrown away before measuring. The first call through any path pays JIT,
        /// static constructors and <see cref="RuntimeInitializeOnLoadMethodAttribute"/> hooks,
        /// which swamps the signal we are actually after.
        /// </summary>
        public const int WarmupIterations = 2;
        /// <summary>
        /// Iterations averaged into the reported figure.
        /// </summary>
        public const int MeasurementIterations = 5;

        /// <summary>
        /// Playmode tests here run many sequential scene operations, so they need far more
        /// than <see cref="SceneTestEnvironment.DefaultTimeout"/>.
        /// </summary>
        public const int TestTimeout = 300000;

        #region Ceilings

        // Baseline: v4.1.3, Unity 6000.5.5f1, editor batchmode playmode, Addressables 2.9.1.
        // Each ceiling is ~20% above the highest of five measured iterations, so ordinary
        // run-to-run variance does not trip it. The measured figures are recorded on #72:
        //
        //   Transition_WithLoadingScreen  avg 33,610 B  (32,566 – 34,654)
        //   Transition_Direct             avg 19,501 B  (19,188 – 20,232)
        //   Load_Single                   avg  8,400 B  ( 8,192 –  8,714)
        //   Load_Multiple                 avg 19,968 B  (19,968 – 19,968)
        //   Unload_Single                 avg  4,662 B  ( 4,662 –  4,662)
        //   Load_Single_Addressable       avg 10,323 B  (10,010 – 10,532)   report only
        //   Transition_..._Addressable    avg 43,662 B  (43,558 – 44,080)   report only

        /// <summary>Transition to a single scene through a loading scene: the README path.</summary>
        public const long TransitionWithLoadingScreen = 41_600;
        /// <summary>Transition with no loading scene, which exercises the temp-transition-scene branch.</summary>
        public const long TransitionDirect = 24_500;
        /// <summary>Load a single scene: the simplest path.</summary>
        public const long LoadSingle = 10_500;
        /// <summary>Load four scenes at once, which exercises the linking layer.</summary>
        public const long LoadMultiple = 24_000;
        /// <summary>Unload a single scene.</summary>
        public const long UnloadSingle = 5_600;

        /// <summary>
        /// Opts a case out of the assertion, leaving it as a reported trend only.
        /// <br/>
        /// The addressable cases use this. CI runs a detected Unity matrix and the three
        /// manifests pin different Addressables majors — 1.19.19, 2.8.0 and 2.9.1 — whose
        /// internal allocation behaviour differs. One ceiling across all three would be either
        /// so loose it catches nothing or so tight it fails on one major. Gating them properly
        /// needs a per-major ceiling; until then they are recorded, not enforced.
        /// </summary>
        public const long NotGated = long.MaxValue;

        #endregion

        /// <summary>
        /// Runs <paramref name="operation"/> <see cref="MeasurementIterations"/> times after
        /// <see cref="WarmupIterations"/> discarded warmups, reports the allocation and
        /// duration of each measured run to the performance framework, and asserts the average
        /// stays under <paramref name="ceiling"/>.
        /// </summary>
        /// <param name="label">Sample group prefix, also used in the console report.</param>
        /// <param name="setup">Optional per-iteration preparation, excluded from the measurement.</param>
        /// <param name="operation">The measured work.</param>
        /// <param name="teardown">Optional per-iteration cleanup, excluded from the measurement.</param>
        /// <param name="ceiling">Byte ceiling for the average, or <see cref="NotGated"/> to report only.</param>
        public static IEnumerator Measure(string label, Func<IEnumerator> setup, Func<IEnumerator> operation, Func<IEnumerator> teardown, long ceiling)
        {
            for (int i = 0; i < WarmupIterations; i++)
            {
                if (setup != null)
                    yield return setup();
                yield return operation();
                if (teardown != null)
                    yield return teardown();
            }

            ProfilerRecorder recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, GcAllocCounter);
            if (!recorder.Valid)
            {
                recorder.Dispose();
                Assert.Ignore($"The '{GcAllocCounter}' profiler counter is unavailable on this runtime, so allocations cannot be measured.");
            }

            SampleGroup allocationSamples = new($"{label}.GC.Alloc", SampleUnit.Byte);
            SampleGroup durationSamples = new($"{label}.Duration", SampleUnit.Millisecond);

            long total = 0;
            long min = long.MaxValue;
            long max = 0;
            Stopwatch stopwatch = new();

            for (int i = 0; i < MeasurementIterations; i++)
            {
                if (setup != null)
                    yield return setup();

                // Settle a frame before sampling so the setup's own garbage lands outside the window.
                yield return null;

                long before = recorder.CurrentValue;
                stopwatch.Restart();

                yield return operation();

                stopwatch.Stop();

                // One more frame so the counter has published the frame the operation finished in.
                yield return null;
                long allocated = recorder.CurrentValue - before;

                total += allocated;
                min = Math.Min(min, allocated);
                max = Math.Max(max, allocated);

                Unity.PerformanceTesting.Measure.Custom(allocationSamples, allocated);
                Unity.PerformanceTesting.Measure.Custom(durationSamples, stopwatch.Elapsed.TotalMilliseconds);

                if (teardown != null)
                    yield return teardown();
            }

            recorder.Dispose();

            long average = total / MeasurementIterations;
            bool gated = ceiling != NotGated;

            UnityEngine.Debug.Log(
                $"[AllocationGate] {label}: avg {average:N0} B (min {min:N0} B, max {max:N0} B) " +
                $"over {MeasurementIterations} iterations — {(gated ? $"ceiling {ceiling:N0} B" : "report only")}");

            if (gated)
            {
                Assert.Less(average, ceiling, $"{label} allocated {average:N0} bytes on average, above the {ceiling:N0} byte ceiling. " +
                    $"If this is an intended cost, raise the constant in {nameof(AllocationGate)}; otherwise it is a regression.");
            }
        }
    }
}
