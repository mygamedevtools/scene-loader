using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public readonly struct AsyncSceneOperationStandard : IAsyncSceneOperation
    {
        public readonly float Progress => _asyncOperation.progress;

        public readonly bool IsDone => _asyncOperation.isDone;

        public readonly bool HasDirectReferenceToScene => false;

        readonly AsyncOperation _asyncOperation;

        public AsyncSceneOperationStandard(AsyncOperation operation)
        {
            _asyncOperation = operation ?? throw new ArgumentException($"Cannot create a {nameof(AsyncSceneOperationStandard)} without a valid AsyncOperation instance.", nameof(operation));
        }

        public Scene GetResult()
        {
            Debug.LogWarning($"{nameof(AsyncSceneOperationStandard)} cannot link directly to the loaded scene due to SceneManager API limitations.");
            return default;
        }
    }
}