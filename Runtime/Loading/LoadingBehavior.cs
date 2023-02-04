/**
 * LoadingBehavior.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/23/2022 (en-US)
 */

using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    [AddComponentMenu("Scene Loading/Loading Behavior")]
    public class LoadingBehavior : MonoBehaviour
    {
        public LoadingProgress Progress { get; private set; }

        [Tooltip("Should it wait for an animation or script to allow starting the transition?")]
        public bool waitForScriptedStart;
        [Tooltip("Should it wait for an animation or script to allow finishing the transition?")]
        public bool waitForScriptedEnd;

        [SerializeField, Tooltip("Common scene operations stop at 90%, but addressable scene operations go all the way up to 100%. Enabling this value reduces the ratio to 90% instead of 100%")]
        bool _reduceLoadRatio;

        void Awake()
        {
            Progress = new LoadingProgress(_reduceLoadRatio);
            Progress.StateChanged += OnLoadingStateChange;
        }

        void Start()
        {
            if (!waitForScriptedStart)
                Progress.SetState(LoadingState.Loading);
        }

        void OnLoadingStateChange(LoadingState loadingState)
        {
            if (loadingState == LoadingState.TargetSceneLoaded && !waitForScriptedEnd)
                Progress.SetState(LoadingState.TransitionComplete);
        }
    }
}