#if ENABLE_ADDRESSABLES
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#endif

namespace MyGameDevTools.SceneLoading
{
    public readonly struct LoadSceneOperationStandard : ILoadSceneOperation
    {
        public readonly float Progress => _loadSceneOperation.progress;

        public readonly bool IsDone => _loadSceneOperation.isDone;

        readonly AsyncOperation _loadSceneOperation;

        public LoadSceneOperationStandard(AsyncOperation operation)
        {
            _loadSceneOperation = operation ?? throw new ArgumentException("Cannot create a LoadSceneOperationStandard without a valid AsyncOperation instance.", nameof(operation));
        }

        public Scene GetResult()
        {
            Debug.LogWarning("Unable to get the loaded scene from standard scene operations.");
            return default;
        }
    }
}