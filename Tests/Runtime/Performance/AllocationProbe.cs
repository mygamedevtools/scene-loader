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
    /// <b>The number is <c>work</c>: the frame total minus ambient.</b> Every case spans several
    /// frames and an idle frame allocates a few hundred bytes on its own, so a raw total partly
    /// measures how long an operation took rather than what it did. Ambient is measured in the
    /// same run rather than assumed.
    /// <br/><br/>
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

        /// <summary>Bytes an idle frame costs, measured in this run rather than assumed.</summary>
        public static long AmbientPerFrameBytes { get; private set; }

        /// <summary>
        /// Measures what an idle frame costs, so every later case can have that subtracted. Run
        /// this before any case that reports a <c>work</c> figure.
        /// </summary>
        public static IEnumerator MeasureAmbient(int frames)
        {
            ProfilerRecorder recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, GcAllocCounter);
            if (!recorder.Valid)
            {
                recorder.Dispose();
                UnityEngine.Debug.LogWarning($"[{nameof(AllocationProbe)}] the GC allocation counter is unavailable, so ambient is treated as zero.");
                yield break;
            }

            // Settle first, so the counter is not still publishing the previous test's frame.
            yield return null;

            long before = recorder.CurrentValue;
            for (int i = 0; i < frames; i++)
                yield return null;

            long total = recorder.CurrentValue - before;
            recorder.Dispose();

            AmbientPerFrameBytes = total / frames;
            UnityEngine.Debug.Log($"[{nameof(AllocationProbe)}] ambient: {AmbientPerFrameBytes:N0} B per idle frame, over {frames} frames.");
        }

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
            int totalFrames = 0;
            Stopwatch stopwatch = new();

            for (int i = 0; i < MeasurementIterations; i++)
            {
                if (setup != null)
                    yield return setup();

                // Settle a frame before sampling so the setup's own garbage lands outside the window.
                yield return null;

                long before = recorder.CurrentValue;
                stopwatch.Restart();

                int frames = 0;
                IEnumerator inner = operation();
                while (inner.MoveNext())
                {
                    frames++;
                    yield return inner.Current;
                }

                stopwatch.Stop();

                // One more frame so the counter has published the frame the operation finished in.
                frames++;
                yield return null;
                long allocated = recorder.CurrentValue - before;

                totalFrames += frames;
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
            int averageFrames = totalFrames / MeasurementIterations;
            long ambient = averageFrames * AmbientPerFrameBytes;
            long work = Math.Max(0, average - ambient);

            UnityEngine.Debug.Log(
                $"[{nameof(AllocationProbe)}] {label}: work {work:N0} B " +
                $"(total {average:N0} B over {averageFrames} frames, ambient {ambient:N0} B, " +
                $"range {min:N0}–{max:N0}) over {MeasurementIterations} iterations");
        }
    }
}
