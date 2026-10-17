using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Applies the chosen theme before rendering, using scene-authored references.</summary>
[DefaultExecutionOrder(-200)]
public sealed class ShowroomLightingController : MonoBehaviour
{
    [Serializable]
    public struct Surface
    {
        public Renderer renderer;
        public Material bright, dark;
    }

    public ShowroomLightingPreset brightPreset, darkPreset;
    public Light keyLight;
    public Light[] ceilingLights = Array.Empty<Light>();
    public Surface[] surfaces = Array.Empty<Surface>();
    public Volume volume;
    public ReflectionProbe reflectionProbe;
    public Texture brightReflection, darkReflection;

    private void OnEnable()
    {
        LightingThemeSettings.Changed += ApplyTheme;
        SceneManager.activeSceneChanged += ActiveSceneChanged;
        ApplyTheme(LightingThemeSettings.Current);
    }

    private void OnDisable()
    {
        LightingThemeSettings.Changed -= ApplyTheme;
        SceneManager.activeSceneChanged -= ActiveSceneChanged;
    }

    private void ActiveSceneChanged(Scene previous, Scene current)
    {
        if (current == gameObject.scene) ApplyTheme(LightingThemeSettings.Current);
    }

    public void ApplyTheme(LightingTheme theme)
    {
        bool bright = theme != LightingTheme.Dark;
        var preset = bright ? brightPreset : darkPreset;
        if (preset == null) return;
        if (gameObject.scene == SceneManager.GetActiveScene())
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = preset.ambientSky;
            RenderSettings.ambientEquatorColor = preset.ambientEquator;
            RenderSettings.ambientGroundColor = preset.ambientGround;
            RenderSettings.ambientIntensity = 1;
            RenderSettings.reflectionIntensity = preset.reflectionIntensity;
            RenderSettings.fog = false;
            RenderSettings.sun = keyLight;
        }
        if (keyLight != null)
        {
            keyLight.color = preset.keyColor;
            keyLight.intensity = preset.keyIntensity;
            keyLight.shadowStrength = preset.shadowStrength;
            keyLight.transform.rotation = preset.keyRotation;
            keyLight.useColorTemperature = preset.keyUseColorTemperature;
            keyLight.colorTemperature = preset.keyColorTemperature;
            keyLight.shadowBias = preset.shadowBias;
            keyLight.shadowNormalBias = preset.shadowNormalBias;
        }
        foreach (var light in ceilingLights)
        {
            if (light == null) continue;
            light.color = preset.ceilingLightColor;
            light.intensity = preset.ceilingLightIntensity;
            light.range = preset.ceilingLightRange;
        }
        foreach (var surface in surfaces)
            if (surface.renderer != null) surface.renderer.sharedMaterial = bright ? surface.bright : surface.dark;
        if (volume != null) volume.sharedProfile = preset.volumeProfile;
        if (reflectionProbe != null)
        {
            reflectionProbe.mode = ReflectionProbeMode.Custom;
            reflectionProbe.customBakedTexture = bright ? brightReflection : darkReflection;
            reflectionProbe.intensity = preset.probeIntensity;
        }
    }
}
