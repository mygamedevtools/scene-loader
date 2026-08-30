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

        BindProgress(LoadingBehaviorRegistry.TryGet(_instance, out LoadingBehavior behavior) ? behavior.Progress : null);

        return SceneOperationPump.Completed(operation);
    }

    public override void Dispose()
    {
        if (_instance != null)
            Object.Destroy(_instance);

        _instance = null;

        base.Dispose();
    }
}
