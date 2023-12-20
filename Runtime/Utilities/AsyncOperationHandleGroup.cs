#if ENABLE_ADDRESSABLES
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyGameDevTools.SceneLoading
{
    public readonly struct AsyncOperationHandleGroup
    {
        public readonly List<AsyncOperationHandle<SceneInstance>> Operations;

        public float Progress
        {
            get
            {
                int count = Operations.Count;
                if (count == 0)
                    return 0;

                float totalProgress = 0f;
                for (int i = 0; i < count; i++)
                    totalProgress += Operations[i].PercentComplete;

                return totalProgress / count;
            }
        }

        public bool IsDone
        {
            get
            {
                if (Operations.Count == 0)
                    return true;

                foreach (var o in Operations)
                    if (!o.IsDone)
                        return false;
                return true;
            }
        }

        public readonly int SetIndexActive;

        public AsyncOperationHandleGroup(List<AsyncOperationHandle<SceneInstance>> operationList, int setIndexActive = -1)
        {
            Operations = operationList;
            SetIndexActive = setIndexActive;
        }

        public IList<SceneInstance> GetResult()
        {
            int operationCount = Operations.Count;
            if (operationCount == 0 || !IsDone)
                return Array.Empty<SceneInstance>();

            var loadedScenes = new List<SceneInstance>(operationCount);
            foreach (var operation in Operations)
            {
                if (operation.Status == AsyncOperationStatus.Failed)
                    Debug.LogException(operation.OperationException);
                else
                    loadedScenes.Add(operation.Result);
            }

            return loadedScenes;
        }
    }
}
#endif