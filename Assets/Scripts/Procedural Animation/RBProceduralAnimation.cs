using System;
using System.Collections;
using UnityEngine;

public class RBProceduralAnimation : MonoBehaviour
{
    [Header("Step")]
    [SerializeField] private float stepDistance = 1f;
    [SerializeField] private float stepHeight = 1f;
    [SerializeField] private float stepSpeed = 5f;

    [SerializeField] private float velocityMultiplier = 0.4f;
    [SerializeField] private float cycleSpeed = 1f;
    [SerializeField] private float cycleLimit = 1f;

    [Header("Timing")]
    [SerializeField] private bool setTimingsManually = false;
    [SerializeField] private float[] manualTimings;
    [SerializeField] private float timingsOffset = 0.25f;

    [Header("Velocity")]
    [SerializeField] private float velocityClamp = 4f;
    [SerializeField] private float velocitySmoothing = 45f;

    [Header("Body Source")]
    [SerializeField] private Rigidbody bodyRigidbody;

    [Header("Procedural Overrides (Read From RBBodyMovement)")]
    [SerializeField] private bool readValuesFromBodyMovement = true;
    [SerializeField] private RBBodyMovement bodyMovement;

    [Header("Ground / Raycasts")]
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private float legRayoffset = 0.29f;
    [SerializeField] private float legRayLength = 0.46f;
    [SerializeField] private float sphereCastRadius = 0.17f;

    [Header("Body Ground Check")]
    [SerializeField] private bool onlyStepWhenGrounded = true;
    [SerializeField] private float bodyGroundCheckDistance = 0.2f;
    [SerializeField] private float bodyGroundCheckUpOffset = 0.05f;

    [Header("Slide Surface Rule")]
    [Tooltip("If ground hit has this tag, stepping is disabled (feet still plant under body anchors).")]
    [SerializeField] private string slideTag = "Slide";

    [Tooltip("Stop/cancel any current steps when entering Slide.")]
    [SerializeField] private bool cancelStepsOnSlide = true;

    [Header("Curves")]
    [SerializeField]
    private AnimationCurve legArcPathY = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2.5f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f, -2.5f, 0f)
    );
    [SerializeField] private AnimationCurve easingFunction = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Legs")]
    [SerializeField] private Transform[] legIktargets;

    [Header("Advanced")]
    [SerializeField] private float refreshTimingRate = 60f;

    [Header("Recovery")]
    [SerializeField] private bool resyncOnEnableAndLand = true;
    [SerializeField] private bool resyncSnapToGround = true;
    [SerializeField] private float resyncSnapDistance = 1.25f;

    [Header("Debug / Gizmos")]
    public bool showGizmoz = true;
    [SerializeField] private bool drawGizmosInEditMode = true;
    [SerializeField] private bool drawBodyGroundCheckGizmo = true;

    public EventHandler<Vector3> OnStepFinished;

    private int nbLegs;

    private Vector3[] lastLegPositions;
    private Vector3[] defaultLegPositionsLocalToBody; // BODY local anchors
    private Vector3[] targetStepPosition;

    private bool[] isLegMoving;
    private Coroutine[] stepCoroutines;

    private float[] footTimings;
    private float[] totalDistance;
    private float[] arcHeightMultiply;

    private Vector3 velocity;
    private Vector3 lastVelocity;
    private Vector3 lastBodyPos;

    private float clampDivider = 1f;
    private bool wasGrounded = true;
    private bool initialized = false;

    private Coroutine timingsCoroutine;
    private bool wasOnSlide = false;

    // Runtime (can be overridden by RBBodyMovement)
    private float runtimeVelocityMultiplier;
    private float runtimeCycleSpeed;
    private float runtimeVelocityClamp;

    private Transform BodyT => bodyRigidbody ? bodyRigidbody.transform : transform;

    private void Reset()
    {
        if (bodyRigidbody == null) bodyRigidbody = GetComponentInParent<Rigidbody>();
        if (bodyMovement == null) bodyMovement = GetComponentInParent<RBBodyMovement>();
    }

    private void Awake()
    {
        if (bodyRigidbody == null) bodyRigidbody = GetComponentInParent<Rigidbody>();
        if (bodyMovement == null) bodyMovement = GetComponentInParent<RBBodyMovement>();
    }

    private void Start()
    {
        nbLegs = legIktargets != null ? legIktargets.Length : 0;
        if (nbLegs == 0)
        {
            initialized = false;
            return;
        }

        if (setTimingsManually && (manualTimings == null || manualTimings.Length != nbLegs))
            Debug.LogError("manualTimings length should be equal to the leg count");

        lastLegPositions = new Vector3[nbLegs];
        defaultLegPositionsLocalToBody = new Vector3[nbLegs];
        targetStepPosition = new Vector3[nbLegs];

        isLegMoving = new bool[nbLegs];
        stepCoroutines = new Coroutine[nbLegs];

        footTimings = new float[nbLegs];
        totalDistance = new float[nbLegs];
        arcHeightMultiply = new float[nbLegs];

        Transform bt = BodyT;

        for (int i = 0; i < nbLegs; i++)
        {
            footTimings[i] = setTimingsManually ? manualTimings[i] : i * timingsOffset;

            lastLegPositions[i] = legIktargets[i].position;
            defaultLegPositionsLocalToBody[i] = bt.InverseTransformPoint(legIktargets[i].position);

            targetStepPosition[i] = lastLegPositions[i];
            isLegMoving[i] = false;
        }

        lastBodyPos = bt.position;
        lastVelocity = Vector3.zero;
        velocity = Vector3.zero;

        wasGrounded = IsBodyGroundedRuntime(out _);
        initialized = true;

        // init runtime values from inspector defaults
        runtimeVelocityMultiplier = velocityMultiplier;
        runtimeCycleSpeed = cycleSpeed;
        runtimeVelocityClamp = velocityClamp;

        if (resyncOnEnableAndLand)
            ResyncLegsImmediate();

        if (timingsCoroutine != null) StopCoroutine(timingsCoroutine);
        timingsCoroutine = StartCoroutine(UpdateTimingsLoop());
    }

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        if (!initialized) return;

        if (resyncOnEnableAndLand)
            ResyncLegsImmediate();

        if (timingsCoroutine != null) StopCoroutine(timingsCoroutine);
        timingsCoroutine = StartCoroutine(UpdateTimingsLoop());
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;

        StopAllStepCoroutines();

        if (timingsCoroutine != null)
        {
            StopCoroutine(timingsCoroutine);
            timingsCoroutine = null;
        }
    }

    private void Update()
    {
        if (!initialized || nbLegs == 0) return;

        // Live override from RBBodyMovement (walk/sprint)
        if (readValuesFromBodyMovement && bodyMovement != null)
        {
            runtimeVelocityMultiplier = bodyMovement.ProcVelocityMultiplier;
            runtimeCycleSpeed = bodyMovement.ProcCycleSpeed;
            runtimeVelocityClamp = bodyMovement.ProcVelocityClamp;
        }
        else
        {
            runtimeVelocityMultiplier = velocityMultiplier;
            runtimeCycleSpeed = cycleSpeed;
            runtimeVelocityClamp = velocityClamp;
        }

        bool grounded = IsBodyGroundedRuntime(out RaycastHit groundHit);
        bool onSlide = IsSlideSurface(groundHit);

        // Enter/Exit slide transitions
        if (onSlide && !wasOnSlide)
        {
            wasOnSlide = true;

            if (cancelStepsOnSlide)
            {
                StopAllStepCoroutines();
                for (int i = 0; i < nbLegs; i++)
                    footTimings[i] = setTimingsManually ? manualTimings[i] : i * timingsOffset;
            }
        }
        else if (!onSlide && wasOnSlide)
        {
            wasOnSlide = false;
            ResyncLegsImmediate();
        }

        // Grounded gating (optional)
        if (onlyStepWhenGrounded)
        {
            if (!grounded)
            {
                wasGrounded = false;

                StopAllStepCoroutines();

                lastBodyPos = BodyT.position;
                lastVelocity = Vector3.zero;
                velocity = Vector3.zero;

                return;
            }
            else if (!wasGrounded)
            {
                wasGrounded = true;
                if (resyncOnEnableAndLand)
                    ResyncLegsImmediate();
            }
        }

        // Compute velocity (for non-slide stepping)
        Vector3 rawVel = GetBodyVelocityWorld();
        velocity = Vector3.MoveTowards(lastVelocity, rawVel, Time.deltaTime * velocitySmoothing);
        lastVelocity = velocity;

        clampDivider = 1f / Remap(velocity.magnitude, 0f, runtimeVelocityClamp, 1f, 2f);

        Transform bt = BodyT;

        // Always plant feet to ground
        for (int i = 0; i < nbLegs; i++)
        {
            if (isLegMoving[i]) continue;

            Vector3 plantSourceWorld = onSlide
                ? bt.TransformPoint(defaultLegPositionsLocalToBody[i])
                : lastLegPositions[i];

            (Vector3 p, bool didHit) = FitToTheGroundHit(
                plantSourceWorld,
                layerMask,
                legRayoffset,
                legRayLength,
                sphereCastRadius
            );

            legIktargets[i].position = p;

            if (onSlide)
            {
                lastLegPositions[i] = p;
                targetStepPosition[i] = p;
                totalDistance[i] = 0f;
                arcHeightMultiply[i] = 0f;
            }
        }

        // Slide tag: disable stepping completely, keep planting only
        if (onSlide)
        {
            lastBodyPos = bt.position;
            return;
        }

        // Normal stepping logic
        float cycleSpeedMultiplier = Remap(velocity.magnitude, 0f, runtimeVelocityClamp, 1f, 2f);

        for (int i = 0; i < nbLegs; i++)
        {
            footTimings[i] += Time.deltaTime * runtimeCycleSpeed * cycleSpeedMultiplier;

            if (footTimings[i] >= cycleLimit && !isLegMoving[i])
            {
                footTimings[i] = 0f;
                SetUp(i);
            }
        }

        lastBodyPos = bt.position;
    }

    private bool IsSlideSurface(RaycastHit hit)
    {
        if (string.IsNullOrEmpty(slideTag)) return false;
        if (hit.collider == null) return false;
        return hit.collider.CompareTag(slideTag);
    }

    public void SetUp(int index)
    {
        Transform bt = BodyT;

        Vector3 anchorWorld = bt.TransformPoint(defaultLegPositionsLocalToBody[index]);

        // Step prediction uses planar velocity
        Vector3 planarVel = Vector3.ProjectOnPlane(velocity, Vector3.up);
        Vector3 dir = planarVel.sqrMagnitude > 0.0001f ? planarVel.normalized : Vector3.zero;

        float mag = Mathf.Clamp(planarVel.magnitude, 0f, runtimeVelocityClamp * clampDivider);

        Vector3 predicted = anchorWorld + dir * mag * runtimeVelocityMultiplier;

        (Vector3 hitPoint, bool didHit) = FitToTheGroundHit(predicted, layerMask, legRayoffset, legRayLength, sphereCastRadius);
        if (!didHit) return;

        targetStepPosition[index] = hitPoint;

        totalDistance[index] = Vector3.Distance(legIktargets[index].position, anchorWorld);

        float distToTarget = Vector3.Distance(legIktargets[index].position, targetStepPosition[index]);
        arcHeightMultiply[index] = distToTarget / Mathf.Max(0.0001f, stepDistance);

        if (stepCoroutines[index] != null)
        {
            StopCoroutine(stepCoroutines[index]);
            stepCoroutines[index] = null;
        }

        stepCoroutines[index] = StartCoroutine(MakeStep(targetStepPosition[index], index));
    }

    private IEnumerator MakeStep(Vector3 targetPosition, int index)
    {
        isLegMoving[index] = true;

        float current = 0f;

        while (current < 1f)
        {
            current += Time.deltaTime * stepSpeed;

            float y = legArcPathY.Evaluate(current) * stepHeight * Mathf.Clamp01(arcHeightMultiply[index]);

            Vector3 desired = new Vector3(
                targetPosition.x,
                targetPosition.y + y,
                targetPosition.z
            );

            legIktargets[index].position = Vector3.Lerp(
                lastLegPositions[index],
                desired,
                easingFunction.Evaluate(current)
            );

            yield return null;
        }

        LegReachedTargetPosition(targetPosition, index);
    }

    private void LegReachedTargetPosition(Vector3 targetPosition, int index)
    {
        legIktargets[index].position = targetPosition;
        lastLegPositions[index] = legIktargets[index].position;

        if (totalDistance[index] > 0.3f)
            OnStepFinished?.Invoke(this, targetPosition);

        isLegMoving[index] = false;
        stepCoroutines[index] = null;
    }

    private IEnumerator UpdateTimingsLoop()
    {
        while (enabled)
        {
            yield return new WaitForSecondsRealtime(refreshTimingRate);

            if (!initialized || nbLegs == 0) continue;

            for (int i = 0; i < nbLegs; i++)
                footTimings[i] = setTimingsManually ? manualTimings[i] : i * timingsOffset;
        }
    }

    private bool IsBodyGroundedRuntime(out RaycastHit hit)
    {
        Vector3 start = BodyT.position + Vector3.up * bodyGroundCheckUpOffset;
        return Physics.Raycast(start, Vector3.down, out hit, bodyGroundCheckDistance, layerMask, QueryTriggerInteraction.Ignore);
    }

    private Vector3 GetBodyVelocityWorld()
    {
        if (bodyRigidbody != null)
        {
#if UNITY_6000_0_OR_NEWER
            return bodyRigidbody.linearVelocity;
#else
            return bodyRigidbody.velocity;
#endif
        }

        float dt = Mathf.Max(Time.deltaTime, 0.000001f);
        return (BodyT.position - lastBodyPos) / dt;
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
    }

    private void ResyncLegsImmediate()
    {
        if (!initialized || nbLegs == 0) return;

        lastBodyPos = BodyT.position;
        velocity = Vector3.zero;
        lastVelocity = Vector3.zero;
        clampDivider = 1f;

        StopAllStepCoroutines();

        Transform bt = BodyT;

        for (int i = 0; i < nbLegs; i++)
        {
            Vector3 anchorWorld = bt.TransformPoint(defaultLegPositionsLocalToBody[i]);
            Vector3 desired = resyncSnapToGround
                ? FitToTheGroundHit(anchorWorld, layerMask, legRayoffset, legRayLength, sphereCastRadius).point
                : anchorWorld;

            if (Vector3.Distance(legIktargets[i].position, desired) > resyncSnapDistance)
                legIktargets[i].position = desired;

            lastLegPositions[i] = legIktargets[i].position;
            targetStepPosition[i] = lastLegPositions[i];

            footTimings[i] = setTimingsManually ? manualTimings[i] : i * timingsOffset;
            totalDistance[i] = 0f;
            arcHeightMultiply[i] = 0f;
        }
    }

    public static float Remap(float input, float oldLow, float oldHigh, float newLow, float newHigh)
    {
        float t = Mathf.InverseLerp(oldLow, oldHigh, input);
        return Mathf.Lerp(newLow, newHigh, t);
    }

    public static (Vector3 point, bool hit) FitToTheGroundHit(
        Vector3 origin,
        LayerMask layerMask,
        float yOffset,
        float rayLength,
        float sphereCastRadius)
    {
        RaycastHit hit;

        if (Physics.Raycast(origin + Vector3.up * yOffset, Vector3.down, out hit, rayLength, layerMask, QueryTriggerInteraction.Ignore))
            return (hit.point, true);

        if (Physics.SphereCast(origin + Vector3.up * yOffset, sphereCastRadius, Vector3.down, out hit, rayLength, layerMask, QueryTriggerInteraction.Ignore))
            return (hit.point, true);

        return (origin, false);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmoz) return;
        if (!drawGizmosInEditMode && !Application.isPlaying) return;

        if (drawBodyGroundCheckGizmo)
        {
            bool grounded = IsBodyGroundedRuntime(out RaycastHit hit);
            bool onSlide = IsSlideSurface(hit);

            Vector3 start = BodyT.position + Vector3.up * bodyGroundCheckUpOffset;
            Vector3 end = start + Vector3.down * bodyGroundCheckDistance;

            Gizmos.color = onSlide ? new Color(1f, 0.5f, 0f) : (grounded ? Color.green : Color.red);
            Gizmos.DrawLine(start, end);

            if (grounded)
                Gizmos.DrawSphere(hit.point, 0.03f);
        }
    }
}
