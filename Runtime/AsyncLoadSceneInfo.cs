/**
 * AsyncSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/16/2022 (en-US)
 */

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyUnityTools.SceneLoading
{
    public readonly struct LoadSceneInfo
    {
        delegate AsyncOperation AsyncSceneOperationDelegate();
        delegate Scene GetSceneDelegate();

        readonly AsyncSceneOperationDelegate _unloadSceneAsyncDelegate;
        readonly AsyncSceneOperationDelegate _loadSceneAsyncDelegate;
        readonly GetSceneDelegate _getSceneDelegate;
        readonly Action _loadSceneDelegate;

        public LoadSceneInfo(int index, LoadSceneMode loadMode = LoadSceneMode.Additive)
        {
            _unloadSceneAsyncDelegate = () => SceneManager.UnloadSceneAsync(index);
            _loadSceneAsyncDelegate = () => SceneManager.LoadSceneAsync(index, loadMode);
            _getSceneDelegate = () => SceneManager.GetSceneByBuildIndex(index);
            _loadSceneDelegate = () => SceneManager.LoadScene(index, loadMode);
        }
        public LoadSceneInfo(string name, LoadSceneMode loadMode = LoadSceneMode.Additive)
        {
            _unloadSceneAsyncDelegate = () => SceneManager.UnloadSceneAsync(name);
            _loadSceneAsyncDelegate = () => SceneManager.LoadSceneAsync(name, loadMode);
            _getSceneDelegate = () => SceneManager.GetSceneByName(name);
            _loadSceneDelegate = () => SceneManager.LoadScene(name, loadMode);
        }

        public AsyncOperation UnloadSceneAsync() => _unloadSceneAsyncDelegate();

        public AsyncOperation LoadSceneAsync() => _loadSceneAsyncDelegate();

        public Scene GetScene() => _getSceneDelegate();

        public void LoadScene() => _loadSceneDelegate();
    }
}