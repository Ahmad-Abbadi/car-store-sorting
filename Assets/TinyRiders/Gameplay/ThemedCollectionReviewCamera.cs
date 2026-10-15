using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Linq;

namespace TinyRiders
{
    /// <summary>Navigation for the isolated artwork review scene; no gameplay or save systems.</summary>
    public sealed class ThemedCollectionReviewCamera : MonoBehaviour
    {
        public Transform[] themeCenters;
        public int initialTheme;
        public Bounds collectionBounds;
        public float moveSpeed = 18f;
        public const float ReviewDistance = 10000f;
        public int CurrentTheme => currentTheme;
        public event Action NavigationChanged;
        public bool PointerOverNavigation { get; set; }
        public bool EditingSearch { get; set; }
        int currentTheme;
        Camera view;
        bool looking;
        float yaw, pitch;

        void Start() { view = GetComponent<Camera>(); ShowTheme(initialTheme); }

        public void ConfigureVisibility()
        {
            if (view == null) view = GetComponent<Camera>();
            view.nearClipPlane = .1f;
            view.farClipPlane = ReviewDistance;
            view.useOcclusionCulling = false;
            view.layerCullDistances = new float[32];
        }

        public Transform[] Cars(int theme)
        {
            return themeCenters[theme].parent.GetComponentsInChildren<ThemedCarIdentity>(true)
                .OrderBy(car => car.collectibleId, StringComparer.Ordinal).Select(car => car.transform).ToArray();
        }

        public void ShowCar(int index)
        {
            var cars = Cars(currentTheme);
            if (index < 0 || index >= cars.Length) return;
            ConfigureVisibility();
            view.orthographic = false; view.fieldOfView = 44;
            var renderers = cars[index].GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            float distance = Mathf.Max(7, bounds.extents.magnitude / Mathf.Sin(22 * Mathf.Deg2Rad) * 1.35f);
            transform.position = bounds.center + new Vector3(.55f, .65f, 1).normalized * distance;
            transform.LookAt(bounds.center); ReadAngles();
            NavigationChanged?.Invoke();
        }

        public void ShowTheme(int index)
        {
            if (themeCenters == null || themeCenters.Length == 0) return;
            currentTheme = (index % themeCenters.Length + themeCenters.Length) % themeCenters.Length;
            ConfigureVisibility();
            view.orthographic = false; view.fieldOfView = 44;
            var center = themeCenters[currentTheme].position;
            transform.position = center + new Vector3(8, 26, 35);
            transform.LookAt(center + Vector3.up);
            ReadAngles();
            NavigationChanged?.Invoke();
        }

        public void ShowOverview()
        {
            ConfigureVisibility();
            view.orthographic = true;
            view.orthographicSize = Mathf.Max(collectionBounds.extents.z + 10, (collectionBounds.extents.x + 10) / Mathf.Max(.5f, view.aspect));
            transform.SetPositionAndRotation(collectionBounds.center + Vector3.up * 450, Quaternion.Euler(90, 180, 0));
            ReadAngles();
            NavigationChanged?.Invoke();
        }

        void ReadAngles()
        {
            yaw = transform.eulerAngles.y;
            pitch = Mathf.DeltaAngle(0, transform.eulerAngles.x);
        }

        void Update()
        {
            var keys = Keyboard.current; var mouse = Mouse.current;
            if (keys == null || mouse == null) return;
            if (EditingSearch) return;
            if (keys.pageDownKey.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame) ShowTheme(currentTheme + 1);
            if (keys.pageUpKey.wasPressedThisFrame || keys.leftArrowKey.wasPressedThisFrame) ShowTheme(currentTheme - 1);
            if (keys.homeKey.wasPressedThisFrame) ShowOverview();
            if (mouse.rightButton.wasPressedThisFrame && !PointerOverNavigation)
            {
                looking = true; ReadAngles(); view.orthographic = false;
                Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            }
            if (mouse.rightButton.wasReleasedThisFrame || keys.escapeKey.wasPressedThisFrame) ReleaseMouse();
            if (looking)
            {
                var delta = mouse.delta.ReadValue(); yaw += delta.x * .12f;
                pitch = Mathf.Clamp(pitch - delta.y * .12f, -85, 85);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0);
                var direction = Vector3.zero;
                if (keys.wKey.isPressed) direction += transform.forward;
                if (keys.sKey.isPressed) direction -= transform.forward;
                if (keys.dKey.isPressed) direction += transform.right;
                if (keys.aKey.isPressed) direction -= transform.right;
                if (keys.eKey.isPressed) direction += Vector3.up;
                if (keys.qKey.isPressed) direction -= Vector3.up;
                transform.position += direction.normalized * moveSpeed * (keys.leftShiftKey.isPressed ? 3 : 1) * Time.unscaledDeltaTime;
            }
            float scroll = mouse.scroll.ReadValue().y;
            if (PointerOverNavigation) return;
            if (view.orthographic) view.orthographicSize = Mathf.Clamp(view.orthographicSize - scroll * .025f, 3, 500);
            else transform.position += transform.forward * (scroll * .025f);
        }

        void ReleaseMouse() { looking = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void OnDisable() { ReleaseMouse(); }
    }
}
