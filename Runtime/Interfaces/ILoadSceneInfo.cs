/**
 * ILoadSceneInfo.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/24/2022 (en-US)
 */

using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Interface to standardize scene information.
    /// Can be created with either the target scene's build index (<see cref="LoadSceneInfoIndex"/>) or the scene's name (<see cref="LoadSceneInfoName"/>).
    /// </summary>
    public interface ILoadSceneInfo
    {
        object Reference { get; }

        bool IsReferenceToScene(Scene scene);
    }
}