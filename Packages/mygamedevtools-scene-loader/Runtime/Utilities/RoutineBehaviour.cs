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
    }
}