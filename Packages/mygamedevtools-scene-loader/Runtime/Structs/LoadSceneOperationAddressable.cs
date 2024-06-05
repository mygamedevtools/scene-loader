#if ENABLE_ADDRESSABLES
using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public readonly struct LoadSceneOperationAddressable : ILoadSceneOperation
    {
        public readonly float Progress => _loadSceneOperationHandle.PercentComplete;

        public readonly bool IsDone => _loadSceneOperationHandle.IsDone;

        public readonly bool HasDirectReferenceToScene => true;

        readonly AsyncOperationHandle<SceneInstance> _loadSceneOperationHandle;

        public LoadSceneOperationAddressable(AsyncOperationHandle<SceneInstance> operationHandle)
        {
            if (!operationHandle.IsValid())
                throw new ArgumentException($"Cannot create a {nameof(LoadSceneOperationAddressable)} from an invalid AsyncOperationHandle.", nameof(operationHandle));

            _loadSceneOperationHandle = operationHandle;
        }

        public Scene GetResult()
        {
            if (_loadSceneOperationHandle.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogException(_loadSceneOperationHandle.OperationException);
                return default;
            }

            return _loadSceneOperationHandle.Result.Scene;
        }
    }
}
#endif