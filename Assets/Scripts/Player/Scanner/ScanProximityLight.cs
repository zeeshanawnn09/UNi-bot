using System.Collections.Generic;
using UnityEngine;

public class RayconeProximityLight : MonoBehaviour
{
    [Header("Proximity Settings")]
    [SerializeField] float detectionRadius = 10f;   // outer radius where flicker happens
    [SerializeField] float alwaysOnRadius = 2f;     // inner radius where light stays on

    [Header("Flicker Settings")]
    [SerializeField] float minFlickerInterval = 0.5f;  // slowest flicker (far)
    [SerializeField] float maxFlickerInterval = 0.05f; // fastest flicker (near)

    [Header("Light")]
    [SerializeField] Light pointLight;              // one point light to toggle

    [Header("Layers (same idea as RayconeScanner)")]
    [SerializeField] LayerMask hitLayers;

    readonly Collider[] overlapBuf = new Collider[32];
    float flickerTimer;

    void Reset()
    {
        // mirror your RayconeScanner default
        hitLayers = LayerMask.GetMask("Geometry", "Scannable");

        if (!pointLight)
            pointLight = GetComponentInChildren<Light>();
    }

    void Awake()
    {
        // start OFF
        if (pointLight != null)
            pointLight.enabled = false;
    }

    void Update()
    {
        if (!pointLight) return;

        float nearestDist;
        bool hasScannable = TryGetNearestScannable(out nearestDist);

        // No scannable or too far -> OFF
        if (!hasScannable || nearestDist > detectionRadius)
        {
            pointLight.enabled = false;
            flickerTimer = 0f;
            return;
        }

        // Inside "always on" radius -> solid ON
        if (nearestDist <= alwaysOnRadius)
        {
            pointLight.enabled = true;
            return;
        }

        // Between alwaysOnRadius and detectionRadius -> FLICKER
        float t = Mathf.InverseLerp(detectionRadius, alwaysOnRadius, nearestDist);
        float interval = Mathf.Lerp(minFlickerInterval, maxFlickerInterval, t);

        flickerTimer += Time.deltaTime;
        if (flickerTimer >= interval)
        {
            flickerTimer -= interval;
            pointLight.enabled = !pointLight.enabled;
        }
    }

    bool TryGetNearestScannable(out float nearestDist)
    {
        nearestDist = float.MaxValue;
        bool found = false;
        Vector3 origin = transform.position;

        int count = Physics.OverlapSphereNonAlloc(
            origin,
            detectionRadius,
            overlapBuf,
            hitLayers,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < count; i++)
        {
            var col = overlapBuf[i];
            if (!col) continue;

            // same approach as your Raycone: use IScannable
            if (!col.TryGetComponent<IScannable>(out var s))
                continue;

            float d = Vector3.Distance(origin, col.transform.position);
            if (d < nearestDist)
            {
                nearestDist = d;
                found = true;
            }
        }

        return found;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, alwaysOnRadius);
    }
#endif
}
