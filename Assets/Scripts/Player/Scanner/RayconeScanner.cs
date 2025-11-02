using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayconeScanner: MonoBehaviour
{
    //Create a raycast that is cone shaped
    [Header("Raycone Settings")]
    [SerializeField] float maxDistance;                      //Range of the cone
    [SerializeField,Range(1f,89f)] float halfAngleDeg = 45f; // ring radius using angele
    [SerializeField, Range(6, 256)] int rays = 48;           //total rays used to construct the cone
    [SerializeField] int rings = 6;                          //radial rings(>=2)

    [Header("Timing & Layers")]
    [SerializeField] float tick = 0.1f;                      //scan frequency in seconds
    [SerializeField] LayerMask hitLayers;

    readonly HashSet<IScannable> current = new();
    readonly HashSet<IScannable> previous = new();
    RaycastHit[] hitBuf = new RaycastHit[8];
    float timer;

    private void Reset()
    {
        hitLayers = LayerMask.GetMask("Geometry","Scannable");
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= tick)
        {
            timer -= tick;
            ScanCone();
        }
    }

    void ScanCone()
    {
        previous.Clear();
        foreach (var s in current) previous.Add(s); //storing previous hits for comparison
        current.Clear();

        Vector3 origin = transform.position;       //initial point of the raycasts 
        Vector3 forward = transform.forward;

        // for cone ring direction
        Vector3 ortho1 = Vector3.Cross(forward, Vector3.up);
        if (ortho1.sqrMagnitude < 1e-6f) ortho1 = Vector3.Cross(forward, Vector3.right);
        ortho1.Normalize();
        Vector3 ortho2 = Vector3.Cross(forward, ortho1);

        // center ray
        CastAndAccumulate(origin, forward);

        // rings
        int raysPerRing = Mathf.Max(1, rays / Mathf.Max(1, rings - 1));      //avoid div by zero
        float halfRad = halfAngleDeg * Mathf.Deg2Rad;
        for (int r = 1; r < rings; r++)
        {
            float t = (float)r / (rings - 1);                                // 0..1
            float ringAngle = t * halfRad;                                   // radians from axis
            float ringR = Mathf.Tan(ringAngle);                              // lateral factor

            for (int i = 0; i < raysPerRing; i++)
            {
                float az = (i + (r * 0.5f)) * (2f * Mathf.PI / raysPerRing); // cast rays as a circle
                Vector3 dir = (forward + Mathf.Cos(az) * ringR * ortho1 + Mathf.Sin(az) * ringR * ortho2).normalized;
                CastAndAccumulate(origin, dir);
            }
        }

        // compare hits. If difference throw a callbacks
        foreach (var s in current)
            if (!previous.Contains(s)) s.OnScanEnter(this);

        foreach (var s in current)
            s.OnScanStay(this, tick);

        foreach (var s in previous)
            if (!current.Contains(s)) s.OnScanExit(this);

    }

    void CastAndAccumulate(Vector3 origin, Vector3 dir)
    {
        int hits = Physics.RaycastNonAlloc(origin, dir, hitBuf, maxDistance, hitLayers, QueryTriggerInteraction.Ignore); //raycast ignores trigger colliders
        if (hits == 0) return;

        // sort nearest first 
        System.Array.Sort(hitBuf, 0, hits, new HitComparer());
        for (int i = 0; i < hits; i++)
        {
            var h = hitBuf[i];
            if (!h.collider) continue;

            // stop at first scannable (cant scan 2 scanables behind each other)
            if (h.collider.TryGetComponent<IScannable>(out var s))
            {
                current.Add(s);
                break;
            }

            // Needs to be discussed, can a object be scanned if covered by non-scannable object?
            // If yes, remove "else break"
            else break;
        }
    }

    //To sort raycast hits by distance and get the nearest one first (used in CastAndAccumulate)
    class HitComparer : System.Collections.Generic.IComparer<RaycastHit>
    {
        public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
    }

    //Gizmo to visualize the raycone in the editor for debugging purposes
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * maxDistance);

        int segs = 24;
        float halfRad = halfAngleDeg * Mathf.Deg2Rad;
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        Vector3 ortho1 = Vector3.Cross(forward, Vector3.up);
        if (ortho1.sqrMagnitude < 1e-6f) ortho1 = Vector3.Cross(forward, Vector3.right);
        ortho1.Normalize();
        Vector3 ortho2 = Vector3.Cross(forward, ortho1);

        float ringR = Mathf.Tan(halfRad);
        for (int i = 0; i < segs; i++)
        {
            float a0 = (i * 2f * Mathf.PI) / segs;
            float a1 = ((i + 1) * 2f * Mathf.PI) / segs;
            Vector3 d0 = (forward + Mathf.Cos(a0) * ringR * ortho1 + Mathf.Sin(a0) * ringR * ortho2).normalized;
            Vector3 d1 = (forward + Mathf.Cos(a1) * ringR * ortho1 + Mathf.Sin(a1) * ringR * ortho2).normalized;

            Gizmos.DrawLine(origin + d0 * maxDistance, origin + d1 * maxDistance);
            Gizmos.DrawLine(origin, origin + d0 * maxDistance);
        }
    }
#endif
}

