/**
 * SceneLoadButtonToggle.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/28/2022 (en-US)
 */

using MyUnityTools.SceneLoading.AddressablesSupport;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyUnityTools.SceneLoading.Samples
{
    [RequireComponent(typeof(Button))]
    public class SceneLoadButtonToggle : MonoBehaviour
    {
        [SerializeField]
        bool _reverse;
        [SerializeField]
        string _additiveSceneName;

        IAddressableSceneLoader _sceneLoader;
        Button _button;

        void Awake()
        {
            var bootstrapper = FindObjectOfType<Bootstrapper>();
            _sceneLoader = bootstrapper.SceneLoader;

            _button = GetComponent<Button>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            UpdateButtonState();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        void OnSceneUnloaded(Scene scene) => UpdateButtonState();

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => UpdateButtonState();

        void UpdateButtonState()
        {
            StartCoroutine(runNextFrame());
            IEnumerator runNextFrame()
            {
                yield return null;
                var sceneIsLoaded = _sceneLoader.GetLoadedSceneHandle(_additiveSceneName).IsValid();
                _button.interactable = _reverse ? !sceneIsLoaded : sceneIsLoaded;
            }
        }
    }
}