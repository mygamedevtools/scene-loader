using MyGameDevTools.SceneLoading;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LoadingFeedbackImageFill : MonoBehaviour
{
    /// <summary>
    /// Set this component reference in the inspector to be able to read the loading progress.
    /// </summary>
    [SerializeField]
    LoadingBehavior _loadingBehavior;

    // We'll use the Image component to display the loading feedback as the fill amount.
    Image _image;

    /// <summary>
    /// Initialize the feedback state.
    /// </summary>
    void Awake()
    {
        _image = GetComponent<Image>();
        _image.fillAmount = 0;
    }

    /// <summary>
    /// Subscribe to the <see cref="LoadingProgress.Progressed"/> event to receive the loading progress of the target scenes.
    /// </summary>
    void Start()
    {
        _loadingBehavior.Progress.Progressed += UpdateSlider;
    }

    /// <summary>
    /// Updates the <see cref="Image.fillAmount"/> to display the loading progress feedback.
    /// </summary>
    void UpdateSlider(float progress) => _image.fillAmount = progress;
}
