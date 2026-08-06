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
    /// Measurement helper, and the single home for every allocation ceiling.
    /// <br/>
    /// The ceilings are editor-playmode <b>regression bounds</b>, not "allocations in a shipped
    /// game" — an absolute claim needs a built-player run.
    /// </summary>
    /// <remarks>
    /// The counter is used because the alternatives do not work here: .NET Standard 2.1 has no
    /// <c>GC.GetTotalAllocatedBytes</c>, and Mono's <c>GC.GetAllocatedBytesForCurrentThread</c>
    /// returns a flat zero. Despite its name, <see cref="ProfilerRecorder.CurrentValue"/>
    /// accumulates for the recorder's lifetime rather than resetting per frame, so a before/after
    /// delta spans any number of frames. The idle floor is ~330 B/frame.
    /// </remarks>
    public static class AllocationGate
    {
        const string GcAllocCounter = "GC Allocated In Frame";

        /// <summary>Discarded before measuring, so JIT and static init stay out of the signal.</summary>
        public const int WarmupIterations = 2;
        /// <summary>Iterations averaged into the reported figure.</summary>
        public const int MeasurementIterations = 5;
        /// <summary>These run many sequential scene operations, so the default timeout is far too short.</summary>
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

        /// <summary>The README path.</summary>
        public const long TransitionWithLoadingScreen = 41_600;
        /// <summary>Exercises the temp-transition-scene branch.</summary>
        public const long TransitionDirect = 24_500;
        /// <summary>The simplest path.</summary>
        public const long LoadSingle = 10_500;
        /// <summary>Four scenes at once, which exercises the linking layer.</summary>
        public const long LoadMultiple = 24_000;
        /// <summary>A single unload.</summary>
        public const long UnloadSingle = 5_600;

        /// <summary>
        /// Reports a case without asserting on it. The addressable cases use this: the three
        /// manifests pin Addressables 1.19.19, 2.8.0 and 2.9.1, and one ceiling across all
        /// three would be either useless or flaky. Gating them needs a per-major ceiling.
        /// </summary>
        public const long NotGated = long.MaxValue;

        #endregion

        /// <summary>
        /// Measures <paramref name="operation"/> after discarded warmups, reports each run to the
        /// performance framework, and asserts the average stays under <paramref name="ceiling"/>.
        /// <paramref name="setup"/> and <paramref name="teardown"/> run outside the measurement.
        /// </summary>
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
