#if ENABLE_ADDRESSABLES
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