using System.Collections;
using TMPro;
using UnityEngine;

namespace TinyRiders
{
    /// <summary>Persistent, decorative collection reward; never participates in sorting.</summary>
    public sealed class GoldenCarReveal : MonoBehaviour
    {
        [Header("Display")]
        public Transform pedestal;
        public GameObject trophy;
        public Transform cover;
        [Tooltip("Keep a protective glass cover in place instead of lifting a reveal cloth.")]
        public bool keepCoverVisible;
        public Light displayLight;
        public TMP_Text placard;
        public GoldenRewardCeremony ceremony;
        public Transform cameraViewpoint;
        [Min(1f)] public float duration = 4f;
        [Range(15f, 90f)] public float revealFieldOfView = 43f;

        public bool IsRevealed { get; private set; }
        public bool IsRevealing { get; private set; }

        Vector3 coverPosition, coverScale;
        Coroutine sequence;
        Camera sourceCamera, revealCamera;
        bool sourceEnabled, cursorVisible;
        CursorLockMode cursorLock;
        bool ownsFreeze;
        readonly System.Collections.Generic.List<Canvas> hiddenCanvases = new System.Collections.Generic.List<Canvas>();

        void Awake()
        {
            if (cover != null) { coverPosition = cover.localPosition; coverScale = cover.localScale; }
            ApplyDisplay(false);
        }

        public void Complete(bool animate)
        {
            if (IsRevealed || IsRevealing) return;
            if (!animate || !isActiveAndEnabled || trophy == null || cover == null || cameraViewpoint == null)
            { ApplyDisplay(true); return; }
            sequence = StartCoroutine(Reveal());
        }

        public void ResetDisplay()
        {
            CancelSequence();
            ApplyDisplay(false);
        }

        void ApplyDisplay(bool revealed)
        {
            IsRevealed = revealed;
            if (trophy != null) trophy.SetActive(revealed);
            if (cover != null)
            {
                cover.localPosition = coverPosition;
                cover.localScale = coverScale;
                cover.gameObject.SetActive(keepCoverVisible || !revealed);
            }
            if (displayLight != null) displayLight.intensity = revealed ? 3f : .25f;
            ceremony?.SetRest(revealed);
        }

        IEnumerator Reveal()
        {
            IsRevealing = true;
            // A final placement may finish while a menu is open. Wait for active play.
            while (GameFlowManager.Instance != null && GameFlowManager.Instance.currentState != GameFlowManager.GameState.Playing)
                yield return null;
            sourceCamera = Camera.main;
            if (sourceCamera == null) { ApplyDisplay(true); FinishSequence(); yield break; }

            cursorVisible = Cursor.visible;
            cursorLock = Cursor.lockState;
            var flow = GameFlowManager.Instance;
            ownsFreeze = flow != null;
            flow?.FreezeGame();
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.enabled && canvas.renderMode != RenderMode.WorldSpace)
                { hiddenCanvases.Add(canvas); canvas.enabled = false; }

            sourceEnabled = sourceCamera.enabled;
            var cameraObject = new GameObject("Golden Car Reveal Camera");
            revealCamera = cameraObject.AddComponent<Camera>();
            revealCamera.CopyFrom(sourceCamera);
            // Copy URP rendering options, including post-processing, without changing the player camera.
            if (sourceCamera.TryGetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(out var sourceData))
            {
                var data = cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                data.renderPostProcessing = sourceData.renderPostProcessing;
                data.volumeLayerMask = sourceData.volumeLayerMask;
                data.antialiasing = sourceData.antialiasing;
                data.requiresDepthTexture = true;
            }
            var startPosition = sourceCamera.transform.position;
            var startRotation = sourceCamera.transform.rotation;
            revealCamera.transform.SetPositionAndRotation(startPosition, startRotation);
            sourceCamera.enabled = false;
            ceremony?.Begin();
            float elapsed = 0;
            while (elapsed < duration)
            {
                // External pause/scene transitions end the cinematic without stealing their state.
                if (flow != null && !flow.IsFrozen) break;
                float t = elapsed / duration;
                float seconds = t * 90f;
                float travel = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .23f));
                if (t > .78f) travel = 1 - Mathf.SmoothStep(0, 1, (t - .78f) / .22f);
                revealCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, cameraViewpoint.position, travel),
                    Quaternion.Slerp(startRotation, cameraViewpoint.rotation, travel));
                revealCamera.fieldOfView = Mathf.Lerp(sourceCamera.fieldOfView, revealFieldOfView, travel);
                if (ceremony != null)
                {
                    ceremony.Sample(seconds);
                    trophy.SetActive(seconds >= 32);
                    if (ceremony.shots.Length > 0)
                    {
                        ceremony.CameraPose(seconds, out var position, out var rotation, out var fov);
                        float enter = Mathf.SmoothStep(0, 1, Mathf.Clamp01(seconds / 12));
                        float leave = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(84, 90, seconds));
                        float blend = enter * (1 - leave);
                        revealCamera.transform.SetPositionAndRotation(Vector3.Lerp(startPosition, position, blend),
                            Quaternion.Slerp(startRotation, rotation, blend));
                        if (seconds < 12 || seconds >= 84)
                        {
                            // Approach around the protective cover instead of crossing through it.
                            var centre = transform.position;
                            var from = startPosition - centre;
                            var to = position - centre;
                            float a = Mathf.Atan2(from.x, from.z) * Mathf.Rad2Deg;
                            float b = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                            float angle = (a + Mathf.DeltaAngle(a,b) * blend) * Mathf.Deg2Rad;
                            float radius = Mathf.Lerp(new Vector2(from.x,from.z).magnitude,new Vector2(to.x,to.z).magnitude,blend);
                            var approach = centre + new Vector3(Mathf.Sin(angle)*radius,Mathf.Lerp(from.y,to.y,blend),Mathf.Cos(angle)*radius);
                            var aim = Quaternion.LookRotation(centre+Vector3.up*1.12f-approach);
                            revealCamera.transform.SetPositionAndRotation(approach,Quaternion.Slerp(startRotation,aim,blend));
                        }
                        revealCamera.fieldOfView = Mathf.Lerp(sourceCamera.fieldOfView, fov, blend);
                    }
                }
                else if (t >= .25f)
                {
                    trophy.SetActive(true);
                    float lift = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .25f) / .27f));
                    if (!keepCoverVisible)
                    {
                        cover.localPosition = coverPosition + Vector3.up * (.85f * lift);
                        cover.localScale = coverScale * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((lift - .45f) / .55f)));
                    }
                    if (displayLight != null) displayLight.intensity = Mathf.Lerp(.25f, 3f, lift);
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            ApplyDisplay(true);
            FinishSequence();
        }

        void FinishSequence()
        {
            bool restoreGameplay = Application.isPlaying && sourceCamera != null && sourceCamera.gameObject.activeInHierarchy;
#if UNITY_EDITOR
            restoreGameplay &= UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#endif
            // Never re-register a motor after its physics system has been torn down during scene unload.
            if (Player.Instance != null && Player.Instance.Motor != null &&
                KinematicCharacterController.KinematicCharacterSystem.GetInstance() == null) restoreGameplay = false;
            bool hadCamera = revealCamera != null;
            if (revealCamera != null)
            { revealCamera.enabled = false; Destroy(revealCamera.gameObject); revealCamera = null; }
            if (sourceCamera != null && hadCamera) sourceCamera.enabled = sourceEnabled;
            sourceCamera = null;
            foreach (var canvas in hiddenCanvases) if (canvas != null) canvas.enabled = true;
            hiddenCanvases.Clear();
            var flow = GameFlowManager.Instance;
            if (ownsFreeze && restoreGameplay && flow != null && flow.IsFrozen)
            {
                flow.UnfreezeGame();
                Cursor.visible = cursorVisible;
                Cursor.lockState = cursorLock;
            }
            ownsFreeze = false;
            IsRevealing = false;
            sequence = null;
        }

        void CancelSequence()
        {
            if (sequence != null) StopCoroutine(sequence);
            FinishSequence();
        }

        void OnDisable()
        {
            bool interrupted = IsRevealing;
            CancelSequence();
            if (interrupted) ApplyDisplay(true);
        }
    }
}
