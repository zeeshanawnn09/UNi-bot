using System;
using System.Collections;
using UnityEngine;

public class RBProceduralAnimation : MonoBehaviour
{
    [SerializeField] private float stepDistance = 1f;
    [SerializeField] private float stepHeight = 1f;
    [SerializeField] private float stepSpeed = 5f;
    [SerializeField] private float velocityMultiplier = .4f;
    [SerializeField] private float cycleSpeed = 1;
    [SerializeField] private float cycleLimit = 1;
    [SerializeField] private bool SetTimingsManually;
    [SerializeField] private float[] manualTimings;
    [SerializeField] private float timigsOffset = 0.25f;
    [SerializeField] private float velocityClamp = 4;

    [Header("Rigidbody")]
    [SerializeField] private bool useRigidbodyVelocity = true;
    [SerializeField] private Rigidbody bodyRigidbody;
    [SerializeField] private bool cacheVelocityInFixedUpdate = true;
    [SerializeField] private float velocitySmoothing = 45f;

    [SerializeField] private LayerMask layerMask;
    [SerializeField]
    private AnimationCurve legArcPathY = new AnimationCurve(
        new Keyframe(0, 0, 0, 2.5f),
        new Keyframe(0.5f, 1),
        new Keyframe(1, 0, -2.5f, 0)
    );
    [SerializeField] private AnimationCurve easingFunction = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Transform[] legIktargets;

    [Header("Raycasts")]
    public bool showGizmoz = true;
    [SerializeField] float legRayoffset = 3;
    [SerializeField] float legRayLength = 6;
    [SerializeField] float sphereCastRadius = 1;

    [Header("Advansed")]
    [SerializeField] private float refreshTimingRate = 60f;

    public EventHandler<Vector3> OnStepFinished;

    private Vector3[] lastLegPositions;
    private Vector3[] defaultLegPositions;
    private Vector3[] targetStepPosition;

    private Vector3 velocity;
    private Vector3 lastVelocity;
    private Vector3 lastBodyPos;

    private Vector3 cachedRbVelocity;

    private float[] footTimings;
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

        if (useRigidbodyVelocity && bodyRigidbody == null)
            bodyRigidbody = GetComponentInParent<Rigidbody>();

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
            footTimings[i] = SetTimingsManually ? manualTimings[i] : i * timigsOffset;

            lastLegPositions[i] = legIktargets[i].position;
            defaultLegPositions[i] = legIktargets[i].localPosition;
        }

        lastBodyPos = transform.position;

        StartCoroutine(UpdateTimings(refreshTimingRate));
    }

    private void FixedUpdate()
    {
        if (!useRigidbodyVelocity) return;
        if (bodyRigidbody == null) return;

        if (cacheVelocityInFixedUpdate)
            cachedRbVelocity = bodyRigidbody.linearVelocity;
    }

    private void Update()
    {
        Vector3 rawVelocity;

        if (useRigidbodyVelocity && bodyRigidbody != null)
        {
            rawVelocity = cacheVelocityInFixedUpdate ? cachedRbVelocity : bodyRigidbody.linearVelocity;
        }
        else
        {
            float dt = Mathf.Max(Time.deltaTime, 0.000001f);
            rawVelocity = (transform.position - lastBodyPos) / dt;
        }

        velocity = Vector3.MoveTowards(lastVelocity, rawVelocity, Time.deltaTime * velocitySmoothing);
        clampDevider = 1f / Remap(velocity.magnitude, 0, velocityClamp, 1, 2);

        lastVelocity = velocity;

        indexTomove = -1;

        for (int i = 0; i < nbLegs; ++i)
        {
            if (i == indexTomove) continue;
            legIktargets[i].position = FitToTheGround(lastLegPositions[i], layerMask, legRayoffset, legRayLength, sphereCastRadius);
        }

        float cycleSpeedMultiplyer = Remap(velocity.magnitude, 0f, velocityClamp, 1f, 2f);

        for (int i = 0; i < nbLegs; ++i)
        {
            footTimings[i] += Time.deltaTime * cycleSpeed * cycleSpeedMultiplyer;

            if (footTimings[i] >= cycleLimit)
            {
                footTimings[i] = 0;
                indexTomove = i;
                SetUp(i);
            }
        }

        lastBodyPos = transform.position;
    }

    public void SetUp(int index)
    {
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
            StartCoroutine(MakeStep(targetStepPosition[index], indexTomove));
        }
    }

    private IEnumerator MakeStep(Vector3 targetPosition, int index)
    {
        isLegMoving[index] = true;

        float current = 0;

        while (current < 1)
        {
            current += Time.deltaTime * stepSpeed;

            float positionY = legArcPathY.Evaluate(current) * stepHeight * Mathf.Clamp(arcHeitMultiply[index], 0, 1f);

            Vector3 desiredStepPosition = new Vector3(
                targetPosition.x,
                positionY + targetPosition.y,
                targetPosition.z);

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
    }

    private IEnumerator UpdateTimings(float time)
    {
        yield return new WaitForSecondsRealtime(time);

        for (int i = 0; i < nbLegs; ++i)
        {
            footTimings[i] = SetTimingsManually ? manualTimings[i] : i * timigsOffset;
        }

        StartCoroutine(UpdateTimings(refreshTimingRate));
    }

    public Transform[] GetLegArray()
    {
        return legIktargets;
    }

    private float GetDistanceToTarget(int index)
    {
        return Vector3.Distance(legIktargets[index].position, transform.TransformPoint(defaultLegPositions[index]));
    }

    public float GetDistanceToGround(int index)
    {
        Vector3 leg = legIktargets[index].position;
        Ray ray = new Ray(leg + Vector3.up * .1f, -Vector3.up);
        RaycastHit hit;

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
        float averageHeight = 0;
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
            Vector3 v = transform.TransformPoint(defaultLegPositions[i]) +
                        velocity.normalized *
                        Mathf.Clamp(velocity.magnitude, 0, velocityClamp * clampDevider) *
                        velocityMultiplier;

            Vector3 v2 = FitToTheGround(v, layerMask, legRayoffset, legRayLength, sphereCastRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(v2, .2f);

            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.TransformPoint(defaultLegPositions[i]) + Vector3.up * legRayoffset, -Vector3.up * legRayLength);
            Gizmos.DrawWireSphere(transform.TransformPoint(defaultLegPositions[i]), sphereCastRadius);
        }
    }

    public static Vector3 FitToTheGround(Vector3 origin, LayerMask layerMask, float yOffset, float rayLength, float sphereCastRadius)
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

    public static bool IsValidStepPoint(Vector3 origin, LayerMask layerMask, float yOffset, float rayLength, float sphereCastRadius)
    {
        RaycastHit hit;

        if (Physics.SphereCast(origin + Vector3.up * yOffset, sphereCastRadius, -Vector3.up, out hit, rayLength, layerMask))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool IsPointInsideCollider(Vector3 point)
    {
        Vector3 yOffset = new Vector3(0, .01f, 0);

        Collider[] hitColiiders = Physics.OverlapSphere(point + yOffset, 0f);
        bool isUnderCollider = Physics.Raycast(point, Vector3.up, 1);

        return hitColiiders.Length > 0 || isUnderCollider;
    }
}
