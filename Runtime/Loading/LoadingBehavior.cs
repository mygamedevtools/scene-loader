/**
 * LoadingBehavior.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/23/2022 (en-US)
 */

using System;
using UnityEngine;

namespace MyUnityTools.SceneLoading
{
    public delegate void SceneLoadProgressDelegate(float progress);
    
    public class LoadingBehavior : MonoBehaviour, IProgress<float>
    {
        public event SceneLoadProgressDelegate OnProgress;
        public event Action OnLoadingComplete;

        public bool Active { get; private set; }

        [SerializeField]
        bool _autoStart;
        [SerializeField]
        bool _autoFinish;
        [SerializeField]
        bool _reduceLoadRatio;

        float _ratio;

        void Awake() => _ratio = _reduceLoadRatio ? .9f : 1f;

        void Start()
        {
            if (_autoStart)
                SetLoadingActive(true);
        }

        public void SetLoadingActive(bool value) => Active = value;

        public void CompleteLoading()
        {
            OnLoadingComplete?.Invoke();
            if (_autoFinish)
                SetLoadingActive(false);
        }

        public void Report(float progress) => OnProgress?.Invoke(progress / _ratio);
    }
}