using System.Collections;
using System.Threading;
using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    public class RoutineBehaviour : MonoBehaviour 
    {
        public static RoutineBehaviour Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = new GameObject(nameof(RoutineBehaviour)).AddComponent<RoutineBehaviour>();
                    DontDestroyOnLoad(_instance.gameObject);
                    _instance.hideFlags = HideFlags.HideAndDontSave;
                }
                return _instance;
            }
        }

        static RoutineBehaviour _instance;

        public Coroutine StartCoroutineWithDisposableToken(IEnumerator routine, CancellationTokenSource cancellationTokenSource)
        {
            return StartCoroutine(wrapperRoutine());
            IEnumerator wrapperRoutine()
            {
                yield return routine;
                cancellationTokenSource.Dispose();
            }
        }
    }
}