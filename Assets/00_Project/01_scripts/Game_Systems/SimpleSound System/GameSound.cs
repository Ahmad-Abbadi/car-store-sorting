using System;
using UnityEngine;

/// <summary> A clip plus its playback settings; the only thing SimpleAudioManager plays. </summary>
[Serializable]
public struct GameSound
{
    public AudioClip Clip;

    [Range(0.1f, 3f)]
    public float Pitch;

    [Range(0f, 1f)]
    public float Volume;

    [Tooltip("Start and end points in seconds.")]
    public Vector2 Cutoff; // startPoint, endPoint

    /// <summary> Full-length sound at the given volume and pitch. </summary>
    public GameSound(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        Clip = clip;
        Pitch = pitch;
        Volume = volume;
        Cutoff = new Vector2(0f, clip != null ? clip.length : 0f);
    }

    /// <summary> True when a clip is assigned. </summary>
    public bool IsValid => Clip != null;

    /// <summary> Pitch, falling back to 1 when unset or invalid. </summary>
    public float SafePitch => Pitch > 0.01f ? Pitch : 1f;

    /// <summary> Start point clamped inside the clip. </summary>
    public float StartTime => Clip != null ? Mathf.Clamp(Cutoff.x, 0f, Clip.length) : 0f;

    /// <summary> End point; the clip's end when unset or out of range. </summary>
    public float EndTime
    {
        get
        {
            if (Clip == null) return 0f;
            float end = Cutoff.y;
            return end <= StartTime || end > Clip.length ? Clip.length : end;
        }
    }

    /// <summary> True when the end point stops the clip early. </summary>
    public bool EndsEarly => Clip != null && EndTime < Clip.length - 0.001f;
}
