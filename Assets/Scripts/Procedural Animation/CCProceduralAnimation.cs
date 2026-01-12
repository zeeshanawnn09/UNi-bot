using System;
using System.Collections;
using UnityEngine;

public class CCProceduralAnimation : MonoBehaviour
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

    [Header("Recovery / Enable")]
    [Tooltip("When this component is enabled (or when landing), resync cached leg positions so feet don't lag behind.")]
    [SerializeField] private bool resyncOnEnableAndLand = true;

    [Tooltip("If a leg target is farther than this from its default anchor, snap it instantly on resync.")]
    [SerializeField] private float resyncSnapDistance = 1.25f;

    [Tooltip("On resync, snap legs to ground under their default anchors.")]
    [SerializeField] private bool resyncSnapToGround = true;

    [Header("Body Ground Check")]
    [Tooltip("Prevents stepping while airborne (fixes jump weirdness).")]
    [SerializeField] private bool onlyStepWhenGrounded = true;

    [SerializeField] private float bodyGroundCheckOffset = 0.2f;
    [SerializeField] private float bodyGroundCheckDistance = 1.2f;

    public EventHandler<Vector3> OnStepFinished;

    private Vector3[] lastLegPositions;
    private Vector3[] defaultLegPositions;
    private Vector3[] targetStepPosition;

    private Vector3 velocity;
    private Vector3 lastVelocity;
    private Vector3 lastBodyPos;

    private float[] footTimings;
    private float[] totalDistance;
    private float clampDevider;
    private float[] arcHeitMultiply;

    private int nbLegs;
    private int indexTomove;

    private bool[] isLegMoving;

    // Track running step coroutines so we can stop them cleanly
    private Coroutine[] stepCoroutines;

    private bool wasGrounded = true;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;

        // If toggled on/off at runtime, prevent "catch up" stepping from stale cached data.
        if (resyncOnEnableAndLand && legIktargets != null && legIktargets.Length > 0)
        {
            ResyncLegsImmediate();
        }

        // make sure timings are in sync
        StartCoroutine(UpdateTimings(refreshTimingRate));
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;

        // Stop any step in progress so it doesn't keep moving feet after disable
        if (stepCoroutines != null)
        {
            for (int i = 0; i < stepCoroutines.Length; i++)
            {
                if (stepCoroutines[i] != null)
                {
                    StopCoroutine(stepCoroutines[i]);
                    stepCoroutines[i] = null;
                }
            }
        }

        if (isLegMoving != null)
        {
            for (int i = 0; i < isLegMoving.Length; i++)
                isLegMoving[i] = false;
        }

        indexTomove = -1;
    }

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

        stepCoroutines = new Coroutine[nbLegs];

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

            // NOTE: You already fixed your default positions; keep whatever you currently use.
            // If you're using body-local defaults, this should remain:
            defaultLegPositions[i] = legIktargets[i].localPosition;
        }

        lastBodyPos = transform.position;
        lastVelocity = Vector3.zero;

        wasGrounded = IsBodyGrounded();

        // make sure timings are in sync
        StartCoroutine(UpdateTimings(refreshTimingRate));
    }

    private void Update()
    {
        bool grounded = IsBodyGrounded();

        // Land transition: resync to prevent long travel-back steps after a jump/disable period.
        if (onlyStepWhenGrounded)
        {
            if (!grounded)
            {
                wasGrounded = false;

                // Stop any steps mid-air so they don't try to plant feet on the ground while airborne
                StopAllStepCoroutines();

                // Still update body pos so velocity doesn't explode when landing
                lastBodyPos = transform.position;
                lastVelocity = Vector3.zero;
                return;
            }
            else if (!wasGrounded)
            {
                wasGrounded = true;
                if (resyncOnEnableAndLand)
                    ResyncLegsImmediate();
            }
        }

        velocity = (transform.position - lastBodyPos) / Time.deltaTime;
        velocity = Vector3.MoveTowards(lastVelocity, velocity, Time.deltaTime * 45f);
        clampDevider = 1 / Remap(velocity.magnitude, 0, velocityClamp, 1, 2);

        lastVelocity = velocity;

        // Fit legs to the ground, but do NOT override legs that are currently stepping
        for (int i = 0; i < nbLegs; ++i)
        {
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
            footTimings[i] += Time.deltaTime * cycleSpeed * cycleSpeedMultiplyer;

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
            // stop any existing step on this leg (safety)
            if (stepCoroutines[index] != null)
            {
                StopCoroutine(stepCoroutines[index]);
                stepCoroutines[index] = null;
            }

            stepCoroutines[index] = StartCoroutine(MakeStep(targetStepPosition[index], index));
        }
    }

    private IEnumerator MakeStep(Vector3 targetPosition, int index)
    {
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

        if (totalDistance[index] > .3f)
        {
            OnStepFinished?.Invoke(this, targetPosition);
        }

        isLegMoving[index] = false;
        stepCoroutines[index] = null;
    }

    private IEnumerator UpdateTimings(float time)
    {
        yield return new WaitForSecondsRealtime(time);

        for (int i = 0; i < nbLegs; ++i)
        {
            if (SetTimingsManually)
                footTimings[i] = manualTimings[i];
            else
                footTimings[i] = i * timigsOffset;
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

        if (Physics.Raycast(ray, out RaycastHit hit, 10f, layerMask))
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
        if (!showGizmoz) return;
        if (legIktargets == null || legIktargets.Length == 0) return;

        int legsCount = Application.isPlaying ? nbLegs : legIktargets.Length;

        Vector3 bodyStart = transform.position + Vector3.up * bodyGroundCheckOffset;
        Vector3 bodyEnd = bodyStart + Vector3.down * bodyGroundCheckDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(bodyStart, 0.05f);
        Gizmos.DrawLine(bodyStart, bodyEnd);
        Gizmos.DrawSphere(bodyEnd, 0.05f);

        for (int i = 0; i < legsCount; ++i)
        {
            if (legIktargets[i] == null) continue;

            Vector3 defaultLocal =
                (defaultLegPositions != null && defaultLegPositions.Length > i)
                    ? defaultLegPositions[i]
                    : legIktargets[i].localPosition;

            Vector3 vel = Application.isPlaying ? velocity : Vector3.zero;
            float clampDiv = Application.isPlaying ? clampDevider : 1f;

            Vector3 v = transform.TransformPoint(defaultLocal) +
                        (vel.sqrMagnitude > 0.0001f ? vel.normalized : Vector3.zero) *
                        Mathf.Clamp(vel.magnitude, 0, velocityClamp * clampDiv) *
                        velocityMultiplier;

            Vector3 v2 = FitToTheGround(v, layerMask, legRayoffset, legRayLength, sphereCastRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(v2, 0.1f);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(legIktargets[i].position, 0.1f);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.TransformPoint(defaultLocal) + Vector3.up * legRayoffset, -Vector3.up * legRayLength);
            Gizmos.DrawWireSphere(transform.TransformPoint(defaultLocal), sphereCastRadius);

            Vector3 castStart = legIktargets[i].position + Vector3.up * legRayoffset;
            Vector3 castEnd = castStart + Vector3.down * legRayLength;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(castStart, castEnd);

            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireSphere(castStart, sphereCastRadius);

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(legIktargets[i].position, 0.03f);
        }
    }



    public static Vector3 FitToTheGround(
        Vector3 origin,
        LayerMask layerMask,
        float yOffset,
        float rayLength,
        float sphereCastRadius)
    {
        if (Physics.Raycast(origin + Vector3.up * yOffset, -Vector3.up, out RaycastHit hit, rayLength, layerMask))
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
        if (Physics.SphereCast(origin + Vector3.up * yOffset, sphereCastRadius, -Vector3.up, out RaycastHit hit, rayLength, layerMask))
        {
            return true;
        }
        else
        {
            Vector3 point = origin;
            Vector3 yOffsetVec = new Vector3(0, .01f, 0);

            Collider[] hitColiiders = Physics.OverlapSphere(point + yOffsetVec, 0f);
            bool isUnderCollider = Physics.Raycast(point, Vector3.up, 1f);

            return hitColiiders.Length > 0 || isUnderCollider;
        }
    }

    private bool IsBodyGrounded()
    {
        Vector3 start = transform.position + Vector3.up * bodyGroundCheckOffset;
        return Physics.Raycast(start, Vector3.down, bodyGroundCheckDistance, layerMask);
    }

    private void StopAllStepCoroutines()
    {
        if (stepCoroutines == null) return;

        for (int i = 0; i < stepCoroutines.Length; i++)
        {
            if (stepCoroutines[i] != null)
            {
                StopCoroutine(stepCoroutines[i]);
                stepCoroutines[i] = null;
            }
            isLegMoving[i] = false;
        }
        indexTomove = -1;
    }

    private void ResyncLegsImmediate()
    {
        // Reset motion history so velocity prediction doesn't spike
        lastBodyPos = transform.position;
        velocity = Vector3.zero;
        lastVelocity = Vector3.zero;
        clampDevider = 1f;

        indexTomove = -1;

        // Stop any step in progress (prevents "travel back" from old step)
        StopAllStepCoroutines();

        for (int i = 0; i < nbLegs; i++)
        {
            // desired anchor under the body
            Vector3 anchorWorld = transform.TransformPoint(defaultLegPositions[i]);
            Vector3 desired = resyncSnapToGround
                ? FitToTheGround(anchorWorld, layerMask, legRayoffset, legRayLength, sphereCastRadius)
                : anchorWorld;

            float dist = Vector3.Distance(legIktargets[i].position, desired);

            // If the IK target is far away (left behind), snap it instantly.
            if (dist > resyncSnapDistance)
            {
                legIktargets[i].position = desired;
            }

            lastLegPositions[i] = legIktargets[i].position;
            targetStepPosition[i] = lastLegPositions[i];

            // Reset timing phase so legs don't all fire at once after resync
            footTimings[i] = SetTimingsManually ? manualTimings[i] : i * timigsOffset;
            totalDistance[i] = 0f;
            arcHeitMultiply[i] = 0f;
        }
    }
}
