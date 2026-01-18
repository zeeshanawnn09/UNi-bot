using UnityEngine;
using System.Collections.Generic;

public class ToggleDevca : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject scannerDecal;   // decal on the player
    [SerializeField] private AudioSource scannerAudio;  // optional hum (separate object from decal)

    [Header("Proximity Radii")]
    [SerializeField] private float outerRadius = 10f;   // start reacting when inside this
    [SerializeField] private float innerRadius = 2f;    // solid ON when inside this

    [Header("Flicker")]
    [SerializeField] private float maxFlickerInterval = 0.5f;  // slow blink at outer
    [SerializeField] private float minFlickerInterval = 0.05f; // fast blink near inner

    private float flickerTimer;

    private void Awake()
    {
        if (scannerDecal != null)
            scannerDecal.SetActive(false);

        if (scannerAudio != null)
            scannerAudio.playOnAwake = false;
    }

    private void Update()
    {
        if (scannerDecal == null)
            return;

        float nearestDist;
        bool hasTarget = TryGetNearestActiveTarget(out nearestDist);

        // No scannable in range -> decal OFF, audio OFF
        if (!hasTarget || nearestDist > outerRadius)
        {
            scannerDecal.SetActive(false);
            flickerTimer = 0f;
            SetAudio(false);
            return;
        }

        // We are within outerRadius → audio ON
        SetAudio(true);

        // Inside inner radius -> solid ON
        if (nearestDist <= innerRadius)
        {
            scannerDecal.SetActive(true);
            return;
        }

        // Between inner & outer -> flicker, faster closer to inner
        float t = Mathf.InverseLerp(outerRadius, innerRadius, nearestDist);
        float interval = Mathf.Lerp(maxFlickerInterval, minFlickerInterval, t);

        flickerTimer += Time.deltaTime;
        if (flickerTimer >= interval)
        {
            flickerTimer -= interval;
            scannerDecal.SetActive(!scannerDecal.activeSelf);
        }
    }

    private bool TryGetNearestActiveTarget(out float nearestDist)
    {
        nearestDist = float.MaxValue;
        bool found = false;

        List<ScannableDevcaTarget> list = ScannableDevcaTarget.Instances;
        Vector3 playerPos = transform.position;

        for (int i = 0; i < list.Count; i++)
        {
            var target = list[i];
            if (target == null || !target.IsScannable)
                continue;

            float d = Vector3.Distance(playerPos, target.transform.position);
            if (d < nearestDist)
            {
                nearestDist = d;
                found = true;
            }
        }

        return found;
    }

    private void SetAudio(bool shouldPlay)
    {
        if (scannerAudio == null)
            return;

        if (shouldPlay)
        {
            if (!scannerAudio.isPlaying)
                scannerAudio.Play();      // will restart from beginning each time you come back in range
        }
        else
        {
            if (scannerAudio.isPlaying)
                scannerAudio.Stop();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, outerRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, innerRadius);
    }
#endif
}
