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
    /// Measures what a scene operation allocates and reports it. Nothing here fails a run.
    /// </summary>
    /// <remarks>
    /// Deliberately not a gate. These are editor-playmode figures, and the suite runs across a
    /// Unity version matrix against three pinned Addressables majors — a single threshold over
    /// that spread is either loose enough to catch nothing or tight enough to fail on noise.
    /// A real gate needs one pinned configuration and a built player, which is its own job.
    /// <br/><br/>
    /// The counter is used because the alternatives do not work here: .NET Standard 2.1 has no
    /// <c>GC.GetTotalAllocatedBytes</c>, and Mono's <c>GC.GetAllocatedBytesForCurrentThread</c>
    /// returns a flat zero. Despite its name, <see cref="ProfilerRecorder.CurrentValue"/>
    /// accumulates for the recorder's lifetime rather than resetting per frame, so a before/after
    /// delta spans any number of frames. The idle floor is ~330 B/frame.
    /// </remarks>
    public static class AllocationProbe
    {
        const string GcAllocCounter = "GC Allocated In Frame";

        /// <summary>Discarded before measuring, so JIT and static init stay out of the signal.</summary>
        public const int WarmupIterations = 2;
        /// <summary>Iterations averaged into the reported figure.</summary>
        public const int MeasurementIterations = 5;
        /// <summary>These run many sequential scene operations, so the default timeout is far too short.</summary>
        public const int TestTimeout = 300000;

        // Reference figures, Unity 6000.5.5f1, editor batchmode playmode, Addressables 2.9.1.
        // Recorded on #72 so a run can be eyeballed against them; nothing reads them.
        //
        //   Transition_WithLoadingScreen  avg 33,610 B  (32,566 – 34,654)
        //   Transition_Direct             avg 19,501 B  (19,188 – 20,232)
        //   Load_Single                   avg  8,400 B  ( 8,192 –  8,714)
        //   Load_Multiple                 avg 19,968 B  (19,968 – 19,968)
        //   Unload_Single                 avg  4,662 B  ( 4,662 –  4,662)
        //   Load_Single_Addressable       avg 10,323 B  (10,010 – 10,532)
        //   Transition_..._Addressable    avg 43,662 B  (43,558 – 44,080)

        /// <summary>
        /// Measures <paramref name="operation"/> after discarded warmups and reports each run to
        /// the performance framework. <paramref name="setup"/> and <paramref name="teardown"/>
        /// run outside the measurement.
        /// </summary>
        public static IEnumerator Measure(string label, Func<IEnumerator> setup, Func<IEnumerator> operation, Func<IEnumerator> teardown)
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

            UnityEngine.Debug.Log(
                $"[{nameof(AllocationProbe)}] {label}: avg {total / MeasurementIterations:N0} B " +
                $"(min {min:N0} B, max {max:N0} B) over {MeasurementIterations} iterations");
        }
    }
}
