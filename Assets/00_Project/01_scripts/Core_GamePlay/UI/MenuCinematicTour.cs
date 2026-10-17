using System;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>Scene-local, unscaled-time camera tour. All poses are authored in world space.</summary>
[DefaultExecutionOrder(-50)]
public sealed class MenuCinematicTour : MonoBehaviour
{
    [Serializable]
    public sealed class Shot
    {
        public string name;
        public CinemachineCamera camera;
        public Transform start;
        public Transform end;
        [Min(0.1f)] public float duration = 8f;
    }

    [SerializeField] private CinemachineBrain brain;
    [SerializeField, Min(0)] private float blendTime = 2f;
    [SerializeField] private Shot[] shots = Array.Empty<Shot>();
    private int current;
    private float elapsed;
    private bool running;
    private bool firstFrame;
    public int CurrentShot => current;
    public int CompletedLoops { get; private set; }

    private void OnEnable()
    {
        current = 0;
        elapsed = 0;
        CompletedLoops = 0;
        running = shots.Length > 0;
        firstFrame = true;
        for (int i = 0; i < shots.Length; i++)
        {
            if (shots[i].camera == null || shots[i].start == null || shots[i].end == null)
            {
                Debug.LogError("Cinematic tour needs a camera and both endpoints for every shot.", this);
                running = false;
                return;
            }
            SetPose(shots[i], 0);
            shots[i].camera.Priority = i == 0 ? 20 : 0;
        }
        if (brain != null)
        {
            brain.IgnoreTimeScale = true;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendTime);
        }
    }

    private void Update()
    {
        if (!running) return;
        // Scene activation can report loading time as the first unscaled delta.
        if (firstFrame) { firstFrame = false; return; }
        elapsed += Time.unscaledDeltaTime;
        // Hold outgoing endpoints during blends. Reset only the incoming camera.
        while (elapsed >= Mathf.Max(.1f, shots[current].duration))
        {
            elapsed -= Mathf.Max(.1f, shots[current].duration);
            SetPose(shots[current], 1);
            shots[current].camera.Priority = 0;
            current = (current + 1) % shots.Length;
            if (current == 0) CompletedLoops++;
            SetPose(shots[current], 0);
            shots[current].camera.Priority = 20;
            if (brain != null)
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut,
                    Mathf.Min(blendTime, Mathf.Max(.1f, shots[current].duration)));
        }
        SetPose(shots[current], elapsed / Mathf.Max(.1f, shots[current].duration));
    }

    private static void SetPose(Shot shot, float t)
    {
        t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
        shot.camera.transform.SetPositionAndRotation(Vector3.Lerp(shot.start.position, shot.end.position, t),
            Quaternion.Slerp(shot.start.rotation, shot.end.rotation, t));
    }

    public void StopTour() => running = false;
    private void OnDisable() => running = false;
}
