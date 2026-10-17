using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Shared lighting assets and values; runtime switching never modifies this asset.</summary>
public sealed class ShowroomLightingPreset : ScriptableObject
{
    public Color ambientSky, ambientEquator, ambientGround;
    public Color keyColor, ceilingLightColor;
    public float keyIntensity, shadowStrength, ceilingLightIntensity, ceilingLightRange;
    public Quaternion keyRotation = Quaternion.identity;
    public bool keyUseColorTemperature = true;
    public float keyColorTemperature = 3900, shadowBias = .035f, shadowNormalBias = .16f;
    public float reflectionIntensity = .75f, probeIntensity = .85f;
    public VolumeProfile volumeProfile;
    public List<Material> ceilingMaterials = new List<Material>();
}
