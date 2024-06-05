using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public readonly struct LoadSceneOperationStandard : ILoadSceneOperation
    {
        public readonly float Progress => _loadSceneOperation.progress;

        public readonly bool IsDone => _loadSceneOperation.isDone;

        public readonly bool HasDirectReferenceToScene => false;

        readonly AsyncOperation _loadSceneOperation;

        public LoadSceneOperationStandard(AsyncOperation operation)
        {
            _loadSceneOperation = operation ?? throw new ArgumentException($"Cannot create a {nameof(LoadSceneOperationStandard)} without a valid AsyncOperation instance.", nameof(operation));
        }

        public Scene GetResult()
        {
            Debug.LogWarning("Standard Load Scene Operations cannot link directly to the loaded scene due to SceneManager API limitations.");
            return default;
        }
    }
}