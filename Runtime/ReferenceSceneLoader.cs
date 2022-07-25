/**
 * SceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 3/1/2021 (en-US)
 */

using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyUnityTools.SceneLoading
{
    public class ReferenceSceneLoader
    {
        public event System.Action<float> OnLoadingProgress;

        //#if UNITY_EDITOR
        //    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        //    static void Bootstrap()
        //    {
        //        var activeScene = SceneManager.GetActiveScene();
        //        if (activeScene.buildIndex == 0)
        //            return;
        //        SceneManager.LoadScene(0, LoadSceneMode.Additive);
        //    }

        //    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        //    static void ResetState()
        //    {
        //        loadedSceneHandle = new AsyncOperationHandle<SceneInstance>();
        //    }
        //#endif

        //public void ReloadCurrentScene() => _ = LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);

        //public void LoadPreviousScene() => _ = LoadSceneAsync(SceneManager.GetActiveScene().buildIndex - 1);

        //public void LoadNextScene() => _ = LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);

        //public void LoadSceneByIndex(int buildIndex) => _ = LoadSceneAsync(buildIndex);

        //public void LoadSceneByName(string name) => _ = LoadSceneAsync(name);

        //public void LoadScene(AssetReference scene) => _ = SwitchSceneAsync(scene);

        //public async UniTask SwitchSceneAsync(string name)
        //{
        //    await UnloadCurrentSceneAsync();
        //    await LoadSceneAsync(name);
        //}
        //public async UniTask SwitchSceneAsync(int buildIndex)
        //{
        //    await UnloadCurrentSceneAsync();
        //    await LoadSceneAsync(buildIndex);
        //}
        //public async UniTask SwitchSceneAsync(AssetReference scene)
        //{
        //    await UnloadCurrentSceneAsync();
        //    await LoadSceneAsync(scene);
        //}

        //public async UniTask LoadSceneAsync(string name)
        //{
        //    var operation = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
        //    while (!operation.isDone)
        //    {
        //        OnLoadingProgress?.Invoke(operation.progress / .9f);
        //        await UniTask.Yield();
        //    }
        //    SceneManager.SetActiveScene(SceneManager.GetSceneByName(name));
        //}
        //public async UniTask LoadSceneAsync(int buildIndex)
        //{
        //    var operation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive);
        //    while (!operation.isDone)
        //    {
        //        OnLoadingProgress?.Invoke(operation.progress / .9f);
        //        await UniTask.Yield();
        //    }
        //    SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(buildIndex));
        //}
        //public async UniTask LoadSceneAsync(AssetReference scene)
        //{
        //    var operation = scene.LoadSceneAsync(LoadSceneMode.Additive);
        //    while (!operation.IsDone)
        //    {
        //        OnLoadingProgress?.Invoke(operation.PercentComplete);
        //        await UniTask.Yield();
        //    }
        //    loadedSceneHandle = operation;
        //}

        //public async UniTask LoadSceneLoadingAsync(string name)
        //{
        //    await UnloadCurrentSceneAsync(SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive).ToUniTask());
        //    await LoadSceneAsync(name);
        //    _ = SceneManager.UnloadSceneAsync(loadingSceneName);
        //}
        //public async UniTask LoadSceneLoadingAsync(int buildIndex)
        //{
        //    await UnloadCurrentSceneAsync(SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive).ToUniTask());
        //    await LoadSceneAsync(buildIndex);
        //    _ = SceneManager.UnloadSceneAsync(loadingSceneName);
        //}
        //public async UniTask LoadSceneLoadingAsync(AssetReference scene, bool unloadCurrent = true)
        //{
        //    var loadingHandle = new AsyncOperationHandle<SceneInstance>();
        //    if (unloadCurrent)
        //        await UnloadCurrentSceneAsync(loadLoadingSceneAsync());
        //    else
        //        await loadLoadingSceneAsync();
        //    await LoadSceneAsync(scene);
        //    await Addressables.UnloadSceneAsync(loadingHandle);

        //    async UniTask loadLoadingSceneAsync()
        //    {
        //        var operation = loadingSceneReference.LoadSceneAsync(LoadSceneMode.Additive);
        //        await operation;
        //        loadingHandle = operation;
        //    }
        //}

        //public UniTask UnloadCurrentSceneAsync() => UnloadCurrentSceneAsync(UniTask.CompletedTask);
        //public async UniTask UnloadCurrentSceneAsync(UniTask onBeforeUnload)
        //{
        //    var activeScene = SceneManager.GetActiveScene();
        //    await onBeforeUnload;
        //    if (loadedSceneHandle.IsValid())
        //        await Addressables.UnloadSceneAsync(loadedSceneHandle);
        //    else
        //        await SceneManager.UnloadSceneAsync(activeScene);
        //}
    }
}