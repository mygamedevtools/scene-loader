#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor.Profiling;
using UnityEditorInternal;
using Unity.Profiling;
using UnityEngine;

namespace MyGameDevTools.SceneLoading.Tests.Performance
{
    /// <summary>
    /// Measures what a scene operation allocates, attributes it to the
    /// <see cref="SceneProfilerMarkers"/> scopes it happened in, and writes the run to JSON for CI
    /// to diff. Nothing here fails a test.
    /// </summary>
    /// <remarks>
    /// <b>Two numbers, because neither is sufficient alone.</b> <c>workBytes</c> is the frame total
    /// minus measured ambient — it misses nothing, but it includes the engine's own work, so it
    /// moves for reasons outside this repository. The <c>MSM.*</c> scope table is precise about
    /// where allocations happened, but only covers synchronous regions: a marker cannot span an
    /// <c>await</c>, so everything an async continuation allocates lands outside one. Expect the
    /// scopes to account for a small fraction of <c>workBytes</c>; <c>unattributedBytes</c> reports
    /// that gap rather than hiding it. Track the first, localise with the second.
    /// <br/><br/>
    /// Marker samples are inclusive of their children, so <c>MSM.Engine.*</c> subtracts out of the
    /// enclosing package scope to leave what this package costs on top of the engine.
    /// <br/><br/>
    /// <b>Why a binary log rather than the in-memory buffer.</b> <see cref="ProfilerDriver.enabled"/>
    /// captures nothing under <c>-batchmode -nographics</c> — the frame buffer stays empty and
    /// <see cref="ProfilerDriver.firstFrameIndex"/> reports -1. Writing a <c>.raw</c> log and
    /// loading it back does work headless, which is the only reason this runs in CI at all.
    /// </remarks>
    public static class AllocationReport
    {
        /// <summary>Discarded before measuring, so JIT and static init stay out of the signal.</summary>
        public const int WarmupIterations = 2;

        /// <summary>These run many sequential scene operations, so the default timeout is far too short.</summary>
        public const int TestTimeout = 300000;

        /// <summary>
        /// Where the run lands: the project root, not <c>persistentDataPath</c>, so CI can find it
        /// at a path that does not depend on where the container mounts the home directory.
        /// </summary>
        public static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "allocation-report.json"));

        static readonly List<CaseResult> _results = new();

        /// <summary>
        /// Bytes an idle frame costs, measured in this run rather than assumed. Every case spans
        /// several frames, and that ambient garbage belongs to neither the engine nor this package.
        /// </summary>
        public static long AmbientPerFrameBytes { get; private set; }

        public sealed class CaseResult
        {
            public string Name;
            public int Frames;
            /// <summary>The outermost package scopes, which still contain the engine's own cost.</summary>
            public long PackageInclusiveBytes;
            public long EngineBytes;
            public long TotalBytes;

            /// <summary>The marked package scopes minus the engine calls nested inside them.</summary>
            public long PackageBytes => PackageInclusiveBytes - EngineBytes;

            /// <summary>Ambient garbage for the frames this case spanned.</summary>
            public long AmbientBytes => Frames * AmbientPerFrameBytes;

            /// <summary>
            /// The tracked headline: everything the frames allocated beyond ambient. Covers the
            /// engine's own work as well as this package's, so it moves for reasons outside this
            /// repository — but it is the only figure that misses nothing.
            /// </summary>
            public long WorkBytes => TotalBytes - AmbientBytes;

            /// <summary>
            /// Work the markers did not account for: async continuations, engine internals across
            /// frames, and the test harness. Large by design — see the class remarks.
            /// </summary>
            public long UnattributedBytes => WorkBytes - PackageInclusiveBytes;
            public readonly SortedDictionary<string, long> Scopes = new(StringComparer.Ordinal);
        }

        /// <summary>
        /// Runs <paramref name="operation"/> under the profiler and records what it allocated.
        /// <paramref name="setup"/> and <paramref name="teardown"/> stay outside the capture.
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

            if (setup != null)
                yield return setup();

            string logPath = Path.Combine(Application.persistentDataPath, $"capture-{label}");
            string rawPath = logPath + ".raw";
            SafeDelete(rawPath);

            ProfilerDriver.ClearAllFrames();
            // Playmode runs in the editor process, so without this the capture covers a player
            // that does not exist.
            ProfilerDriver.profileEditor = true;
            ProfilerDriver.enabled = true;

            UnityEngine.Profiling.Profiler.logFile = logPath;
            UnityEngine.Profiling.Profiler.enableBinaryLog = true;
            UnityEngine.Profiling.Profiler.enabled = true;

            // Settle onto a frame boundary so the first captured frame is a whole one.
            yield return null;

            int frames = 0;
            IEnumerator inner = operation();
            while (inner.MoveNext())
            {
                frames++;
                yield return inner.Current;
            }

            // One more frame so the frame the operation finished in gets flushed.
            frames++;
            yield return null;

            UnityEngine.Profiling.Profiler.enabled = false;
            UnityEngine.Profiling.Profiler.logFile = "";
            UnityEngine.Profiling.Profiler.enableBinaryLog = false;
            ProfilerDriver.enabled = false;

            CaseResult result = new() { Name = label, Frames = frames };

            if (!File.Exists(rawPath) || !ProfilerDriver.LoadProfile(rawPath, false))
            {
                Debug.LogWarning($"[{nameof(AllocationReport)}] {label}: no profiler capture was produced, so it is reported as zero.");
            }
            else
            {
                Accumulate(result);
                SafeDelete(rawPath);
            }

            _results.Add(result);

            Debug.Log(
                $"[{nameof(AllocationReport)}] {label}: work {result.WorkBytes:N0} B over {result.Frames} frames " +
                $"(marked package {result.PackageBytes:N0} B, marked engine {result.EngineBytes:N0} B, " +
                $"unattributed {result.UnattributedBytes:N0} B, ambient {result.AmbientBytes:N0} B)");

            if (teardown != null)
                yield return teardown();
        }

        /// <summary>
        /// Measures what an idle frame costs, so every later case can have that subtracted. Call
        /// once, before the first case.
        /// </summary>
        public static IEnumerator MeasureAmbient(int frames)
        {
            ProfilerRecorder recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            if (!recorder.Valid)
            {
                recorder.Dispose();
                Debug.LogWarning($"[{nameof(AllocationReport)}] the GC allocation counter is unavailable, so ambient is treated as zero.");
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
            Debug.Log($"[{nameof(AllocationReport)}] ambient: {AmbientPerFrameBytes:N0} B per idle frame, over {frames} frames.");
        }

        /// <summary>Writes every case measured so far. Call once, after the last case.</summary>
        public static void Write(string unityVersion, string addressablesVersion)
        {
            StringBuilder json = new();
            json.AppendLine("{");
            json.AppendLine($"  \"unityVersion\": {Quote(unityVersion)},");
            json.AppendLine($"  \"addressablesVersion\": {Quote(addressablesVersion)},");
            json.AppendLine($"  \"ambientPerFrameBytes\": {AmbientPerFrameBytes},");
            json.AppendLine("  \"cases\": {");

            for (int i = 0; i < _results.Count; i++)
            {
                CaseResult result = _results[i];
                json.AppendLine($"    {Quote(result.Name)}: {{");
                json.AppendLine($"      \"workBytes\": {result.WorkBytes},");
                json.AppendLine($"      \"frames\": {result.Frames},");
                json.AppendLine($"      \"ambientBytes\": {result.AmbientBytes},");
                json.AppendLine($"      \"unattributedBytes\": {result.UnattributedBytes},");
                json.AppendLine($"      \"packageBytes\": {result.PackageBytes},");
                json.AppendLine($"      \"packageInclusiveBytes\": {result.PackageInclusiveBytes},");
                json.AppendLine($"      \"engineBytes\": {result.EngineBytes},");
                json.AppendLine($"      \"totalBytes\": {result.TotalBytes},");
                json.AppendLine("      \"scopes\": {");

                int scopeIndex = 0;
                foreach (KeyValuePair<string, long> scope in result.Scopes)
                {
                    string comma = ++scopeIndex == result.Scopes.Count ? "" : ",";
                    json.AppendLine($"        {Quote(scope.Key)}: {scope.Value}{comma}");
                }

                json.AppendLine("      }");
                json.AppendLine($"    }}{(i == _results.Count - 1 ? "" : ",")}");
            }

            json.AppendLine("  }");
            json.AppendLine("}");

            File.WriteAllText(OutputPath, json.ToString());
            Debug.Log($"[{nameof(AllocationReport)}] wrote {_results.Count} cases to {OutputPath}");
        }

        static void Accumulate(CaseResult result)
        {
            int first = ProfilerDriver.firstFrameIndex;
            int last = ProfilerDriver.lastFrameIndex;

            for (int frame = first; frame <= last && frame >= 0; frame++)
            {
                using HierarchyFrameDataView view = ProfilerDriver.GetHierarchyFrameDataView(
                    frame, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                    (int)HierarchyFrameDataView.columnGcMemory, false);

                if (view == null || !view.valid)
                    continue;

                int root = view.GetRootItemID();
                result.TotalBytes += (long)view.GetItemColumnDataAsSingle(root, HierarchyFrameDataView.columnGcMemory);
                Walk(view, root, false, result);
            }
        }

        static void Walk(HierarchyFrameDataView view, int id, bool insideMarker, CaseResult result)
        {
            string name = view.GetItemName(id);
            bool isMarker = name != null && name.StartsWith(SceneProfilerMarkers.Prefix, StringComparison.Ordinal);

            if (isMarker)
            {
                long bytes = (long)view.GetItemColumnDataAsSingle(id, HierarchyFrameDataView.columnGcMemory);

                result.Scopes.TryGetValue(name, out long running);
                result.Scopes[name] = running + bytes;

                bool isEngine = name.StartsWith(SceneProfilerMarkers.Prefix + "Engine.", StringComparison.Ordinal);
                if (isEngine)
                {
                    // Counted wherever it appears, since it always nests inside a package scope.
                    result.EngineBytes += bytes;
                }
                else if (!insideMarker)
                {
                    // Samples are inclusive, so only the outermost package scope is added —
                    // anything nested is already part of this number, engine calls included.
                    result.PackageInclusiveBytes += bytes;
                }
            }

            if (!view.HasItemChildren(id))
                return;

            List<int> children = new();
            view.GetItemChildren(id, children);
            foreach (int child in children)
                Walk(view, child, insideMarker || isMarker, result);
        }

        static void SafeDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
                // A capture left behind is harmless; the next run overwrites it.
            }
        }

        static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
#endif
