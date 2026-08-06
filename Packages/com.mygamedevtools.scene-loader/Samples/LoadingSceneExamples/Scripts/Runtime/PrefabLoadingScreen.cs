using MyGameDevTools.SceneLoading;
using UnityEngine;

/// <summary>
/// A loading screen that is a prefab rather than a scene. A reference implementation — copy it
/// and change it; the hard part, <see cref="LoadingScreenHost"/>, is in the package.
/// <code>
/// await MySceneManager.TransitionAsync("target", new PrefabLoadingScreen(loadingScreenPrefab));
/// </code>
/// </summary>
public class PrefabLoadingScreen : LoadingScreen
{
    readonly GameObject _prefab;

    GameObject _instance;
    LoadingProgress _progress;

    /// <param name="prefab">
    /// A <see cref="LoadingBehavior"/> anywhere on it is picked up automatically and gates the
    /// transition; without one the screen holds nothing up.
    /// </param>
    public PrefabLoadingScreen(GameObject prefab)
    {
        _prefab = prefab != null ? prefab : throw new System.ArgumentNullException(nameof(prefab));
    }

    public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation)
    {
        _instance = Object.Instantiate(_prefab);
        // Into the holder scene, so it survives the outgoing scene being unloaded.
        host.Adopt(_instance);

        _progress = _instance.GetComponentInChildren<LoadingBehavior>(true)?.Progress;

        return SceneOperationPump.Completed(operation);
    }

    public override SceneOperationPump.ConditionAwaiter ShowAsync(SceneOperation operation)
    {
        return _progress == null ? SceneOperationPump.Completed(operation) : _progress.WaitForShowAsync(operation);
    }

    public override void ReportProgress(float progress)
    {
        _progress?.Report(progress);
    }

    public override SceneOperationPump.ConditionAwaiter HideAsync(SceneOperation operation)
    {
        if (_progress == null)
            return SceneOperationPump.Completed(operation);

        _progress.SetLoadingCompleted();
        return _progress.WaitForHideAsync(operation);
    }

    public override void Dispose()
    {
        if (_instance != null)
            Object.Destroy(_instance);

        _instance = null;
        _progress = null;
    }
}
