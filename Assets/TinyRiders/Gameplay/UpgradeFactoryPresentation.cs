using UnityEngine;

namespace TinyRiders
{
    /// <summary> Cosmetic machinery only. The upgrade manager owns all spending and progression. </summary>
    public sealed class UpgradeFactoryPresentation : MonoBehaviour
    {
        public Transform turntable;
        public Transform leftArm;
        public Transform rightArm;
        public Transform scanner;
        public Renderer visibilityBody;
        public GameObject readyIndicators;
        public ParticleSystem completionParticles;
        public AudioClip completionClip;

        private static UpgradeFactoryPresentation activeStation;
        private readonly Plane[] planes = new Plane[6];
        private UpgradeBox box;
        private UpgradePanelUI panel;
        private Quaternion leftRest, rightRest;
        private Vector3 scanRest;
        private float nextVisibilityCheck, phase, cycleStart = -10f;
        private bool visible = true, pending;
        public int PurchaseFeedbackCount { get; private set; }
        public bool IsCelebrating => Time.unscaledTime - cycleStart < 1.8f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActiveStation() => activeStation = null;

        private void Awake()
        {
            if (leftArm) leftRest = leftArm.localRotation;
            if (rightArm) rightRest = rightArm.localRotation;
            if (scanner) scanRest = scanner.localPosition;
        }

        private void OnEnable()
        {
            box = GetComponentInParent<UpgradeBox>();
            if (box) box.Interacted += Activate;
            UpgradeManager.OnPurchased += Purchased;
        }

        private void OnDisable()
        {
            if (box) box.Interacted -= Activate;
            UpgradeManager.OnPurchased -= Purchased;
            if (activeStation == this) activeStation = null;
            pending = false;
            cycleStart = -10f;
            if (leftArm) leftArm.localRotation = leftRest;
            if (rightArm) rightArm.localRotation = rightRest;
            if (scanner) { scanner.localPosition = scanRest; scanner.gameObject.SetActive(false); }
            if (completionParticles) completionParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Activate()
        {
            activeStation = this;
            panel = FindFirstObjectByType<UpgradePanelUI>();
            if (readyIndicators) readyIndicators.SetActive(true);
        }

        private void Purchased(UpgradeSO upgrade)
        {
            if (activeStation != this) return;
            PurchaseFeedbackCount++;
            pending = true; // Show the machinery after the opaque upgrade panel closes.
        }

        private void Update()
        {
            if (pending && (!panel || !panel.IsOpen))
            {
                pending = false;
                cycleStart = Time.unscaledTime;
                if (completionParticles) completionParticles.Play();
                if (completionClip && SimpleAudioManager.instance)
                    SimpleAudioManager.instance.PlayUI(new GameSound(completionClip, .4f));
            }
            bool working = IsCelebrating;
            if (scanner && scanner.gameObject.activeSelf != working) scanner.gameObject.SetActive(working);
            if (Time.unscaledTime >= nextVisibilityCheck)
            {
                nextVisibilityCheck = Time.unscaledTime + .25f;
                var camera = Camera.main;
                visible = !camera || !visibilityBody;
                if (camera && visibilityBody)
                {
                    GeometryUtility.CalculateFrustumPlanes(camera, planes);
                    visible = GeometryUtility.TestPlanesAABB(planes, visibilityBody.bounds);
                }
            }
            if (!visible) return;
            phase += Time.unscaledDeltaTime;
            if (turntable) turntable.Rotate(0f, Time.unscaledDeltaTime * (working ? 65f : 14f), 0f, Space.Self);
            float t = Mathf.Clamp01((Time.unscaledTime - cycleStart) / 1.8f);
            float stroke = working ? Mathf.Sin(t * Mathf.PI) * Mathf.Sin(t * Mathf.PI * 3f) * 12f : Mathf.Sin(phase * .9f) * 2f;
            if (leftArm) leftArm.localRotation = leftRest * Quaternion.Euler(0, 0, stroke);
            if (rightArm) rightArm.localRotation = rightRest * Quaternion.Euler(0, 0, -stroke);
            if (scanner && working) scanner.localPosition = scanRest + Vector3.up * (.035f + .29f * Mathf.Sin(t * Mathf.PI));
            if (readyIndicators) readyIndicators.transform.localScale = Vector3.one * (1f + .025f * Mathf.Sin(phase * 2f));
        }
    }
}
