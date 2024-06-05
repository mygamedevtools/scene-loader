using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public static class SceneDataUtilities
    {
        public static Scene[] GetLoadedScenesFromSceneDataArray(ISceneData[] sceneDataArray)
        {
            int sceneCount = sceneDataArray.Length;
            Scene[] loadedScenes = new Scene[sceneCount];
            for (int i = 0; i < sceneCount; i++)
            {
                loadedScenes[i] = sceneDataArray[i].LoadedScene;
            }
            return loadedScenes;
        }

        /// <summary>
        /// Attempts to link a loaded scene with an <see cref="ISceneData"> from a list.
        /// </summary>
        /// <param name="scene">The loaded scene to be linked.</param>
        /// <param name="sceneDataList"/>The list of <see cref="ISceneData"/> to validate.</param>
        /// <param name="matchedData">The matched <see cref="ISceneData"/>.</param>
        /// <returns>Whether a match has been found</match>
        public static bool TryLinkLoadedSceneWithSceneData(Scene scene, IList<ISceneData> sceneDataList, out ISceneData matchedData)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new ArgumentException($"Cannot try to link an invalid or unloaded scene: {scene.name} ({scene.handle}).", nameof(scene));
            }

            int sceneDataCount = sceneDataList.Count;
            for (int i = sceneDataCount - 1; i >= 0; i--)
            {
                if (sceneDataList[i].LoadSceneInfo.IsReferenceToScene(scene))
                {
                    matchedData = sceneDataList[i];
                    return true;
                }
            }
            matchedData = null;
            return false;
        }

        public static bool HasCompletedAllSceneLoadOperations(ISceneData[] sceneDataArray)
        {
            int length = sceneDataArray.Length;
            for (int i = 0; i < length; i++)
            {
                if (!sceneDataArray[i].LoadOperation.IsDone)
                    return false;
            }
            return true;
        }

        public static float GetAverageSceneLoadOperationProgress(ISceneData[] sceneDataArray)
        {
            int length = sceneDataArray.Length;
            float totalProgress = 0;
            for (int i = 0; i < length; i++)
            {
                totalProgress += sceneDataArray[i].LoadOperation.Progress;
            }
            return totalProgress / length;
        }
    }
}