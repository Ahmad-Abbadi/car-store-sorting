using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary> Drives FpsMouseLook sensitivity from a UI slider; saves to PlayerPrefs. </summary>
public class SensitivitySlider : MonoBehaviour
{
    private const string PrefKey = "LookSensitivity";

    public Slider Slider;

    [Tooltip("Optional. Auto-found in the scene if left empty.")]
    public FpsMouseLook MouseLook;

    public float MinValue = 0.1f;
    public float MaxValue = 5f;
    public float DefaultValue = 2f;

    private float _value;

    private void Awake()
    {
        _value = Mathf.Clamp(PlayerPrefs.GetFloat(PrefKey, DefaultValue), MinValue, MaxValue);

        if (Slider != null)
        {
            Slider.minValue = MinValue;
            Slider.maxValue = MaxValue;
            Slider.SetValueWithoutNotify(_value);
            Slider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Start() => Apply();

    private void OnDestroy()
    {
        if (Slider != null) Slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    /// <summary> Re-applies to the new player after a scene (re)load. </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        MouseLook = null;
        Apply();
    }

    /// <summary> Stores, saves and applies the slider value. </summary>
    private void OnSliderChanged(float value)
    {
        _value = value;
        PlayerPrefs.SetFloat(PrefKey, value);
        PlayerPrefs.Save();
        Apply();
    }

    /// <summary> Pushes the current value to the mouse look controller. </summary>
    private void Apply()
    {
        if (MouseLook == null) MouseLook = FindFirstObjectByType<FpsMouseLook>();
        if (MouseLook != null) MouseLook.Sensitivity = _value;
    }
}
