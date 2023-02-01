/**
 * LoadingProgress.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2023-01-31
 */

using System;

namespace MyGameDevTools.SceneLoading
{
    public class LoadingProgress : IProgress<float>
    {
        public event LoadingStateChangeDelegate StateChanged;
        public event SceneLoadProgressDelegate Progressed;

        public LoadingState State { get; private set; }

        readonly float _ratio;

        public LoadingProgress(bool reducedLoadRatio = false)
        {
            _ratio = reducedLoadRatio ? .9f : 1;
            State = LoadingState.WaitingToStart;
        }

        public void SetState(LoadingState targetState)
        {
            State = targetState;
            StateChanged?.Invoke(State);
        }

        public void Report(float value)
        {
            Progressed?.Invoke(value / _ratio);
        }
    }

    public delegate void SceneLoadProgressDelegate(float progress);

    public delegate void LoadingStateChangeDelegate(LoadingState loadingState);
}