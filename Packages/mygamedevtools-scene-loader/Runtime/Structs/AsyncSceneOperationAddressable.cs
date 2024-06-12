#if ENABLE_ADDRESSABLES
using System;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Struct to implement <see cref="IAsyncSceneOperation"/> with the addressable <see cref="AsyncOperationHandle<SceneInstance>"/>.
    /// </summary>
    public readonly struct AsyncSceneOperationAddressable : IAsyncSceneOperation
    {
        public readonly float Progress => _asyncOperationHandle.PercentComplete;

        public readonly bool IsDone => _asyncOperationHandle.IsDone;

        public readonly bool HasDirectReferenceToScene => true;

        public AsyncOperationHandle<SceneInstance> AsyncOperationHandle => _asyncOperationHandle;

        readonly AsyncOperationHandle<SceneInstance> _asyncOperationHandle;

        public AsyncSceneOperationAddressable(AsyncOperationHandle<SceneInstance> operationHandle)
        {
            if (!operationHandle.IsValid())
                throw new ArgumentException($"Cannot create a {nameof(AsyncSceneOperationAddressable)} from an invalid AsyncOperationHandle.", nameof(operationHandle));

            _asyncOperationHandle = operationHandle;
        }

        public Scene GetResult()
        {
            if (_asyncOperationHandle.Status == AsyncOperationStatus.Failed)
            {
                throw _asyncOperationHandle.OperationException;
            }

            return _asyncOperationHandle.Result.Scene;
        }
    }
}
#endif