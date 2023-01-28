/**
 * ISceneLoaderAsync.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/15/2022 (en-US)
 */

using System;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Interface to standardize async scene operations.
    /// </summary>
    public interface ISceneLoaderAsync<TAsync> : ISceneLoader
    {
        TAsync TransitionToSceneAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo intermediateSceneReference = default);

        TAsync LoadSceneAsync(ILoadSceneInfo sceneReference, bool setActive = false, IProgress<float> progress = null);

        TAsync UnloadSceneAsync(ILoadSceneInfo sceneReference);
    }
}