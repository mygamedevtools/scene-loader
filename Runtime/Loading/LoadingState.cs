/**
 * LoadingState.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2023-01-31
 */

namespace MyGameDevTools.SceneLoading
{
    public enum LoadingState
    {
        WaitingToStart,
        Loading,
        TargetSceneLoaded,
        TransitionComplete
    }
}