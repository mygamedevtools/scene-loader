using System.Collections.Generic;
using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    public readonly struct AsyncOperationGroup
    {
        public readonly List<AsyncOperation> Operations;

        public float Progress
        {
            get
            {
                int count = Operations.Count;
                if (count == 0)
                    return 0;

                float totalProgress = 0f;
                for (int i = 0; i < count; i++)
                    totalProgress += Operations[i].progress;

                return totalProgress / count;
            }
        }

        public bool IsDone
        {
            get
            {
                foreach (var o in Operations)
                    if (!o.isDone)
                        return false;
                return true;
            }
        }

        public AsyncOperationGroup(int initialCapacity)
        {
            Operations = new List<AsyncOperation>(initialCapacity);
        }
    }
}