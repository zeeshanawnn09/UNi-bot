using System;
using System.Collections;
using UnityEngine;

public class SProceduralAnimation : MonoBehaviour
{
    [Tooltip("Step distance is used to calculate step height. When the character makes a very short step there is no need to raise the foot all the way up, so if this step distance value is small, step height will be lower than usual.")]
    [SerializeField] private float stepDistance = 1f;
    [SerializeField] private float stepHeight = 1f;
    [SerializeField] private float stepSpeed = 5f;

    [Tooltip("Velocity multiplier used to make step wider when moving fast (step will move further ahead if you increase this).")]
    [SerializeField] private float velocityMultiplier = .4f;

    [SerializeField] private float cycleSpeed = 1f;

    [Tooltip("How often in seconds legs will move (every one second by default).")]
    [SerializeField] private float cycleLimit = 1f;

    [Tooltip("If you want some legs to move together enable this and set timings manually. For example on a 4-leg creature you can use [0, 0, 0.5, 0.5] so first two legs move together and only 0.5 seconds later the second two will move.")]
    [SerializeField] private bool SetTimingsManually;
    [SerializeField] private float[] manualTimings;

    [Tooltip("If you want only one leg to move at a time then you can set this to a non-zero offset. Example: for 4 legs, 0.25 means script will offset the cycle of every leg by 0.25 seconds.")]
    [SerializeField] private float timigsOffset = 0.25f;

    [Tooltip("Velocity clamp limits the step distance while moving at high speed.")]
    [SerializeField] private float velocityClamp = 4f;

    [SerializeField] private LayerMask layerMask;

    [SerializeField]
    private AnimationCurve legArcPathY = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2.5f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f, -2.5f, 0f)
    );

    [SerializeField]
    private AnimationCurve easingFunction = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] private Transform[] legIktargets;

    [Header("Raycasts")]
    public bool showGizmoz = true;
    [SerializeField] private float legRayoffset = 3f;
    [SerializeField] private float legRayLength = 6f;

    [Tooltip("Ground check range for every leg.")]
    [SerializeField] private float sphereCastRadius = 1f;

    [Header("Advanced")]
    [Tooltip("Refresh timings rate. Updates timings and sets initial offsets. Example: fast pink robot in demo scene has this value set to 10.")]
    [SerializeField] private float refreshTimingRate = 60f;

    public EventHandler<Vector3> OnStepFinished;

    private Vector3[] lastLegPositions;
    private Vector3[] defaultLegPositions;
    private Vector3[] raycastPoints;
    private Vector3[] targetStepPosition;

    private Vector3 velocity;
    private Vector3 lastVelocity;
    private Vector3 lastBodyPos;

    private float[] footTimings;
    private float[] targetTimings;
    private float[] totalDistance;
    private float clampDevider;
    private float[] arcHeitMultiply;

    private int nbLegs;
    private int indexTomove;

    private bool[] isLegMoving;

    private void Start()
    {
        indexTomove = -1;
        nbLegs = legIktargets.Length;

        defaultLegPositions = new Vector3[nbLegs];
        lastLegPositions = new Vector3[nbLegs];
        targetStepPosition = new Vector3[nbLegs];

        isLegMoving = new bool[nbLegs];
        footTimings = new float[nbLegs];
        arcHeitMultiply = new float[nbLegs];
        totalDistance = new float[nbLegs];

        if (SetTimingsManually && manualTimings.Length != nbLegs)
        {
            Debug.LogError("manual footTimings length should be equal to the leg count");
        }

        for (int i = 0; i < nbLegs; ++i)
        {
            if (SetTimingsManually)
            {
                footTimings[i] = manualTimings[i];
            }
            else
            {
                footTimings[i] = i * timigsOffset;
            }

            lastLegPositions[i] = legIktargets[i].position;
            defaultLegPositions[i] = legIktargets[i].localPosition;
        }

        // make sure timings are in sync
        StartCoroutine(UpdateTimings(refreshTimingRate));
    }

    private void Update()
    {
        velocity = (transform.position - lastBodyPos) / Time.deltaTime;
        velocity = Vector3.MoveTowards(lastVelocity, velocity, Time.deltaTime * 45f);
        clampDevider = 1 / Remap(velocity.magnitude, 0, velocityClamp, 1, 2);

        lastVelocity = velocity;

        // Fit legs to the ground, but do NOT override legs that are currently stepping
        for (int i = 0; i < nbLegs; ++i)
        {
            // Skip legs that are currently making a step
            if (isLegMoving[i])
                continue;

            legIktargets[i].position = FitToTheGround(
                lastLegPositions[i],
                layerMask,
                legRayoffset,
                legRayLength,
                sphereCastRadius
            );
        }

        // move legs more frequently when speed is close to max speed
        float cycleSpeedMultiplyer = Remap(velocity.magnitude, 0f, velocityClamp, 1f, 2f);

        for (int i = 0; i < nbLegs; ++i)
        {
            // move legs whenever footTimings reaches the limit
            footTimings[i] += Time.deltaTime * cycleSpeed * cycleSpeedMultiplyer;

            // do NOT start a new step on a leg that is already moving
            if (footTimings[i] >= cycleLimit && !isLegMoving[i])
            {
                footTimings[i] = 0f;

                indexTomove = i;
                SetUp(i);
            }
        }

        lastBodyPos = transform.position;
    }

    public void SetUp(int index)
    {
        // finding target step point based on body velocity 
        Vector3 v = transform.TransformPoint(defaultLegPositions[index]) +
                    velocity.normalized *
                    Mathf.Clamp(velocity.magnitude, 0, velocityClamp * clampDevider) *
                    velocityMultiplier;

        targetStepPosition[index] = FitToTheGround(v, layerMask, legRayoffset, legRayLength, sphereCastRadius);

        totalDistance[index] = GetDistanceToTarget(index);

        float distance = Vector3.Distance(legIktargets[index].position, targetStepPosition[index]);
        arcHeitMultiply[index] = distance / stepDistance;

        if (targetStepPosition[index] != Vector3.zero &&
            IsValidStepPoint(targetStepPosition[index], layerMask, legRayoffset, legRayLength, sphereCastRadius))
        {
            // IMPORTANT FIX: use the actual leg index here, not indexTomove
            StartCoroutine(MakeStep(targetStepPosition[index], index));
        }
    }

    private IEnumerator MakeStep(Vector3 targetPosition, int index)
    {
        // IMPORTANT FIX: mark this leg as moving while the step is in progress
        isLegMoving[index] = true;

        float current = 0f;

        while (current < 1f)
        {
            current += Time.deltaTime * stepSpeed;

            float positionY = legArcPathY.Evaluate(current) *
                              stepHeight *
                              Mathf.Clamp(arcHeitMultiply[index], 0f, 1f);

            Vector3 desiredStepPosition = new Vector3(
                targetPosition.x,
                targetPosition.y + positionY,
                targetPosition.z
            );

            legIktargets[index].position = Vector3.Lerp(
                lastLegPositions[index],
                desiredStepPosition,
                easingFunction.Evaluate(current)
            );

            yield return null;
        }

        LegReachedTargetPosition(targetPosition, index);
    }

    private void LegReachedTargetPosition(Vector3 targetPosition, int index)
    {
        indexTomove = -1;
        legIktargets[index].position = targetPosition;
        lastLegPositions[index] = legIktargets[index].position;

        // Event for visual and sound effects
        if (totalDistance[index] > .3f)
        {
            OnStepFinished?.Invoke(this, targetPosition);
        }

        isLegMoving[index] = false;
    }

    private IEnumerator UpdateTimings(float time)
    {
        yield return new WaitForSecondsRealtime(time);

        for (int i = 0; i < nbLegs; ++i)
        {
            if (SetTimingsManually)
            {
                footTimings[i] = manualTimings[i];
            }
            else
            {
                footTimings[i] = i * timigsOffset;
            }
        }
    }

    public Transform[] GetLegArray()
    {
        return legIktargets;
    }

    private float GetDistanceToTarget(int index)
    {
        return Vector3.Distance(
            legIktargets[index].position,
            transform.TransformPoint(defaultLegPositions[index])
        );
    }

    public float GetDistanceToGround(int index)
    {
        Vector3 leg = legIktargets[index].position;
        Ray ray = new Ray(leg + Vector3.up * .1f, -Vector3.up);
        RaycastHit hit;

        // keeping original signature pattern here
        if (Physics.Raycast(ray, out hit, layerMask))
        {
            return Vector3.Distance(leg, hit.point);
        }

        return 0f;
    }

    public bool IsLegMoving(int index)
    {
        return isLegMoving[index];
    }

    public LayerMask GetLayerMask()
    {
        return layerMask;
    }

    public float GetAverageLegHeight()
    {
        // calculate body position and raise it when leg is moving based on legs distance to ground
        float averageHeight = 0f;

        for (int i = 0; i < nbLegs; ++i)
        {
            averageHeight += GetDistanceToGround(i);
        }

        averageHeight /= nbLegs;
        return averageHeight;
    }

    public static float Remap(float input, float oldLow, float oldHigh, float newLow, float newHigh)
    {
        float t = Mathf.InverseLerp(oldLow, oldHigh, input);
        return Mathf.Lerp(newLow, newHigh, t);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmoz || !Application.IsPlaying(this))
        {
            return;
        }

        for (int i = 0; i < nbLegs; ++i)
        {
            // target points visualization
            Vector3 v = transform.TransformPoint(defaultLegPositions[i]) +
                        velocity.normalized *
                        Mathf.Clamp(velocity.magnitude, 0, velocityClamp * clampDevider) *
                        velocityMultiplier;

            Vector3 v2 = FitToTheGround(v, layerMask, legRayoffset, legRayLength, sphereCastRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(v2, 0.1f);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(legIktargets[i].position, 0.1f);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(
                transform.TransformPoint(defaultLegPositions[i]) + Vector3.up * legRayoffset,
                -Vector3.up * legRayLength
            );
            Gizmos.DrawWireSphere(transform.TransformPoint(defaultLegPositions[i]), sphereCastRadius);
        }
    }

    public static Vector3 FitToTheGround(
        Vector3 origin,
        LayerMask layerMask,
        float yOffset,
        float rayLength,
        float sphereCastRadius)
    {
        RaycastHit hit;

        if (Physics.Raycast(origin + Vector3.up * yOffset, -Vector3.up, out hit, rayLength, layerMask))
        {
            return hit.point;
        }
        else if (Physics.SphereCast(origin + Vector3.up * yOffset, sphereCastRadius, -Vector3.up, out hit, rayLength, layerMask))
        {
            return hit.point;
        }
        else
        {
            return origin;
        }
    }

    public static bool IsValidStepPoint(
        Vector3 origin,
        LayerMask layerMask,
        float yOffset,
        float rayLength,
        float sphereCastRadius)
    {
        RaycastHit hit;

        if (Physics.SphereCast(origin + Vector3.up * yOffset, sphereCastRadius, -Vector3.up, out hit, rayLength, layerMask))
        {
            return true;
        }
        else
        {
            Vector3 point = origin;
            Vector3 yOffsetVec = new Vector3(0, .01f, 0);

            Collider[] hitColiiders = Physics.OverlapSphere(point + yOffsetVec, 0f);
            bool isUnderCollider = Physics.Raycast(point, Vector3.up, 1f);

            if (hitColiiders.Length > 0 || isUnderCollider)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
