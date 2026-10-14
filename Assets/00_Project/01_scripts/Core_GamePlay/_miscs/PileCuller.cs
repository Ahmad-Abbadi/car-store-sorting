using UnityEngine;

public class PileCuller : MonoBehaviour
{
    public Camera cam;
    public int checksPerFrame = 40;   // items checked per frame
    [Range(0.5f, 1f)] public float samplePadding = 0.9f;

    Rigidbody[] bodies;
    Renderer[] rends;
    Collider[] cols;
    int cursor;

    void Start()
    {
        if (!cam) cam = Camera.main;
        bodies = GetComponentsInChildren<Rigidbody>();
        rends = new Renderer[bodies.Length];
        cols = new Collider[bodies.Length];
        for (int i = 0; i < bodies.Length; i++)
        {
            rends[i] = bodies[i].GetComponentInChildren<Renderer>();
            cols[i] = bodies[i].GetComponentInChildren<Collider>();
        }
    }

    void Update()
    {
        if (bodies.Length == 0) return;
        int n = Mathf.Min(checksPerFrame, bodies.Length);
        for (int k = 0; k < n; k++)
        {
            int i = cursor;
            cursor = (cursor + 1) % bodies.Length;
            if (!bodies[i]) continue;

            // Moving items always render
            if (!bodies[i].IsSleeping()) { rends[i].enabled = true; continue; }

            rends[i].enabled = IsVisible(i);
        }
    }

    bool IsVisible(int i)
    {
        Vector3 camPos = cam.transform.position;
        Bounds b = cols[i].bounds;

        if (RayReaches(camPos, b.center, i)) return true;

        // Also test the 8 corners so partly visible items don't vanish
        Vector3 e = b.extents * samplePadding;
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
            if (RayReaches(camPos, b.center + Vector3.Scale(e, new Vector3(x, y, z)), i))
                return true;

        return false;
    }

    bool RayReaches(Vector3 from, Vector3 to, int i)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (Physics.Raycast(from, dir / dist, out RaycastHit hit, dist + 0.01f,
                            ~0, QueryTriggerInteraction.Ignore))
            return hit.rigidbody == bodies[i];
        return true; // nothing in the way
    }
}