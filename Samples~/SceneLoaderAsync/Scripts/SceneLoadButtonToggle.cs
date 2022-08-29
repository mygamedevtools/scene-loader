/**
 * SceneLoadButtonToggle.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/25/2022 (en-US)
 */

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
        string _sceneToEnableButton;

        Button _button;

        void Awake()
        {
            _button = GetComponent<Button>();
            SceneManager.sceneLoaded += OnLoadScene;
            SceneManager.sceneUnloaded += OnUnloadScene;

            var sceneIsLoaded = SceneManager.GetSceneByName(_sceneToEnableButton).IsValid();
            _button.interactable = _reverse ? !sceneIsLoaded : sceneIsLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnLoadScene;
            SceneManager.sceneUnloaded -= OnUnloadScene;
        }

        void OnUnloadScene(Scene unloadedScene)
        {
            if (unloadedScene.name == _sceneToEnableButton)
                _button.interactable = _reverse;
        }

        void OnLoadScene(Scene loadedScene, LoadSceneMode loadSceneMode)
        {
            if (loadedScene.name == _sceneToEnableButton)
                _button.interactable = !_reverse;
        }
    }
}