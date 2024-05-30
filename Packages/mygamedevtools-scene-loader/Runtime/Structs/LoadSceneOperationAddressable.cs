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
        public float Progress => _loadSceneOperationHandle.PercentComplete;

        public bool IsDone => _loadSceneOperationHandle.IsDone;

        readonly AsyncOperationHandle<SceneInstance> _loadSceneOperationHandle;

        public LoadSceneOperationAddressable(AsyncOperationHandle<SceneInstance> operationHandle)
        {
            if (!operationHandle.IsValid())
                throw new ArgumentException("Cannot create a LoadSceneOperationAddressable from an invalid AsyncOperationHandle.", nameof(operationHandle));

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