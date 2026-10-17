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
        public Light displayLight;
        public TMP_Text placard;
        public Transform cameraViewpoint;
        [Min(1f)] public float duration = 4f;

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
                cover.gameObject.SetActive(!revealed);
            }
            if (displayLight != null) displayLight.intensity = revealed ? 3f : .25f;
            if (placard != null) placard.text = revealed
                ? "GOLDEN CAR\n<size=55%>COLLECTION COMPLETE</size>"
                : "GOLDEN CAR\n<size=55%>Sort all cars to reveal</size>";
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
            }
            var startPosition = sourceCamera.transform.position;
            var startRotation = sourceCamera.transform.rotation;
            revealCamera.transform.SetPositionAndRotation(startPosition, startRotation);
            sourceCamera.enabled = false;
            float elapsed = 0;
            while (elapsed < duration)
            {
                // External pause/scene transitions end the cinematic without stealing their state.
                if (flow != null && !flow.IsFrozen) break;
                float t = elapsed / duration;
                float travel = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .23f));
                if (t > .78f) travel = 1 - Mathf.SmoothStep(0, 1, (t - .78f) / .22f);
                revealCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, cameraViewpoint.position, travel),
                    Quaternion.Slerp(startRotation, cameraViewpoint.rotation, travel));
                revealCamera.fieldOfView = Mathf.Lerp(sourceCamera.fieldOfView, 43f, travel);
                if (t >= .25f)
                {
                    trophy.SetActive(true);
                    float lift = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .25f) / .27f));
                    cover.localPosition = coverPosition + Vector3.up * (.85f * lift);
                    cover.localScale = coverScale * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((lift - .45f) / .55f)));
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
