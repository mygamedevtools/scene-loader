#if ENABLE_ADDRESSABLES
/**
 * SceneReferenceData.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2023-02-03
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneReferenceData : ScriptableObject
    {
        public List<AssetReference> sceneReferences = new List<AssetReference>();
    }
}
#endif