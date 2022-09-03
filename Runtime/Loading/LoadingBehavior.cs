/**
 * LoadingBehavior.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/23/2022 (en-US)
 */

using UnityEngine;

namespace MyUnityTools.SceneLoading
{
    public class LoadingBehavior : LoadingBase
    {
        [SerializeField, Tooltip("Should it wait for an animation or script to allow starting the transition? If not, then enable this toggle.")]
        bool _autoStart;
        [SerializeField, Tooltip("Should it wait for an animation or script to allow finishing the transition? If not, then enable this toggle.")]
        bool _autoFinish;

        void Start()
        {
            if (_autoStart)
                SetLoadingActive(true);
        }

        public void SetLoadingActive(bool value) => Active = value;

        public override void CompleteLoading()
        {
            base.CompleteLoading();
            if (_autoFinish)
                SetLoadingActive(false);
        }
    }
}