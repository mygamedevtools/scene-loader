using Unity.Profiling;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Named profiler scopes for the load and unload paths, so allocations can be attributed to a
    /// region of this package rather than to a total that is mostly the engine's.
    /// </summary>
    /// <remarks>
    /// Marker samples are inclusive of their children, which is the point: <see cref="EngineLoad"/>
    /// nests inside <see cref="Load"/>, so subtracting one from the other gives what this package
    /// costs on top of the Unity Scene Manager. The profiler reports allocations per frame, so a
    /// marker that opens and closes inside one frame is what gets attributed — anything after an
    /// <c>await</c> lands in whichever marker is open when the continuation resumes.
    /// <br/><br/>
    /// <see cref="ProfilerMarker"/>'s begin and end calls compile out of players built without
    /// <c>ENABLE_PROFILER</c>, so this costs a release build nothing.
    /// </remarks>
    public static class SceneProfilerMarkers
    {
        /// <summary>Prefix every marker shares, so a report can pick them out of the frame.</summary>
        public const string Prefix = "MSM.";

        public static readonly ProfilerMarker Load = new(Prefix + "Load");
        public static readonly ProfilerMarker Unload = new(Prefix + "Unload");
        public static readonly ProfilerMarker Transition = new(Prefix + "Transition");

        /// <summary>Turning a scene reference into the data the manager tracks.</summary>
        public static readonly ProfilerMarker BuildSceneData = new(Prefix + "BuildSceneData");

        /// <summary>Working out which loaded scene belongs to which operation.</summary>
        public static readonly ProfilerMarker Link = new(Prefix + "Link");

        /// <summary>The per-frame progress poll, which is where the async machinery resumes.</summary>
        public static readonly ProfilerMarker PollProgress = new(Prefix + "PollProgress");

        /// <summary>The Unity or Addressables call itself — everything under here is not ours.</summary>
        public static readonly ProfilerMarker EngineLoad = new(Prefix + "Engine.Load");

        /// <inheritdoc cref="EngineLoad"/>
        public static readonly ProfilerMarker EngineUnload = new(Prefix + "Engine.Unload");
    }
}
