/**
 * SceneLoaderTestUtilities.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-12
 */

using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneLoaderTestUtilities
    {
        public static IEnumerator UnloadManagerScenes(ISceneManager sceneManager)
        {
            int sceneCount = sceneManager.SceneCount;
            var scenes = new HashSet<ILoadSceneInfo>(sceneCount);

            for (int i = 0; i < sceneCount; i++)
            {
                var scene = sceneManager.GetLoadedSceneAt(i);
                if (scene.IsValid())
                    scenes.Add(new LoadSceneInfoScene(scene));
            }

            if (scenes.Count > 0)
                yield return new WaitTask(sceneManager.UnloadScenesAsync(scenes.ToArray()).AsTask());

            Assert.Zero(sceneManager.SceneCount);
            Assert.False(sceneManager.GetActiveScene().IsValid());
        }
    }
}