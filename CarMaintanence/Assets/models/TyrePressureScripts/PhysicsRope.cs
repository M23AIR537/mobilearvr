using UnityEngine;
using System;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class PhysicsRope : MonoBehaviour
{
    [SerializeField] private Transform startPoint;
    public Transform StartPoint => startPoint;

    [SerializeField] private Transform midPoint;
    public Transform MidPoint => midPoint;

    [SerializeField] private Transform endPoint;
    public Transform EndPoint => endPoint;

    [Range(2, 100)] public int linePoints = 10;
    public float stiffness = 350f;
    public float damping = 15f;
    public float ropeLength = 15;
    public float ropeWidth = 0.1f;

    [Range(1, 15)] public float midPointWeight = 1f;
    private const float StartPointWeight = 1f;
    private const float EndPointWeight = 1f;

    [Range(0.25f, 0.75f)] public float midPointPosition = 0.5f;

    private Vector3 currentValue;
    private Vector3 currentVelocity;
    private Vector3 targetValue;
    public Vector3 otherPhysicsFactors { get; set; }
    private const float valueThreshold = 0.01f;
    private const float velocityThreshold = 0.01f;

    private LineRenderer lineRenderer;
    private bool isFirstFrame = true;

    private Vector3 prevStartPointPosition;
    private Vector3 prevEndPointPosition;
    private float prevMidPointPosition;
    private float prevMidPointWeight;

    private float prevLineQuality;
    private float prevRopeWidth;
    private float prevstiffness;
    private float prevDampness;
    private float prevRopeLength;


    public bool IsPrefab => gameObject.scene.rootCount == 0;

    private void Start()
    {
        InitializeLineRenderer();
        if (AreEndPointsValid())
        {
            currentValue = GetMidPoint();
            targetValue = currentValue;
            currentVelocity = Vector3.zero;
            SetSplinePoint();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            InitializeLineRenderer();
            if (AreEndPointsValid())
            {
                currentValue = GetMidPoint();
                targetValue = currentValue;
                currentVelocity = Vector3.zero;
                SimulatePhysics();
            }
            else
            {
                lineRenderer.positionCount = 0;
            }
        }
    }

    private void InitializeLineRenderer()
    {
        if (!lineRenderer)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;
    }

    private void Update()
    {
        if (IsPrefab)
        {
            return;
        }

        if (AreEndPointsValid())
        {
            SetSplinePoint();

            if (!Application.isPlaying && (IsPointsMoved() || IsRopeSettingsChanged()))
            {
                SimulatePhysics();
            }

            prevStartPointPosition = startPoint.position;
            prevEndPointPosition = endPoint.position;
            prevMidPointPosition = midPointPosition;
            prevMidPointWeight = midPointWeight;

            prevLineQuality = linePoints;
            prevRopeWidth = ropeWidth;
            prevstiffness = stiffness;
            prevDampness = damping;
            prevRopeLength = ropeLength;
        }
    }

    private bool AreEndPointsValid()
    {
        return startPoint != null && endPoint != null;
    }

    private void SetSplinePoint()
    {
        if (lineRenderer.positionCount != linePoints + 1)
        {
            lineRenderer.positionCount = linePoints + 1;
        }

        Vector3 mid = GetMidPoint();
        targetValue = mid;
        mid = currentValue;

        if (midPoint != null)
        {
            midPoint.position = GetWeightedBezierPoint(startPoint.position, mid, endPoint.position, midPointPosition, StartPointWeight, midPointWeight, EndPointWeight);
        }

        for (int i = 0; i < linePoints; i++)
        {
            Vector3 p = GetWeightedBezierPoint(startPoint.position, mid, endPoint.position, i / (float)linePoints, StartPointWeight, midPointWeight, EndPointWeight);
            lineRenderer.SetPosition(i, p);
        }

        lineRenderer.SetPosition(linePoints, endPoint.position);
    }

    private float CalculateYFactorAdjustment(float weight)
    {
        float k = Mathf.Lerp(0.493f, 0.323f, Mathf.InverseLerp(1, 15, weight));
        float w = 1f + k * Mathf.Log(weight);
        return w;
    }

    private Vector3 GetMidPoint()
    {
        Vector3 startPointPosition = startPoint.position;
        Vector3 endPointPosition = endPoint.position;
        Vector3 midpos = Vector3.Lerp(startPointPosition, endPointPosition, midPointPosition);
        float yFactor = (ropeLength - Mathf.Min(Vector3.Distance(startPointPosition, endPointPosition), ropeLength)) / CalculateYFactorAdjustment(midPointWeight);
        midpos.y -= yFactor;
        return midpos;
    }

    private Vector3 GetWeightedBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t, float w0, float w1, float w2)
    {
        Vector3 wp0 = w0 * p0;
        Vector3 wp1 = w1 * p1;
        Vector3 wp2 = w2 * p2;

        float denominator = w0 * Mathf.Pow(1 - t, 2) + 2 * w1 * (1 - t) * t + w2 * Mathf.Pow(t, 2);
        Vector3 point = (wp0 * Mathf.Pow(1 - t, 2) + wp1 * 2 * (1 - t) * t + wp2 * Mathf.Pow(t, 2)) / denominator;

        return point;
    }

    public Vector3 GetPointAt(float t)
    {
        if (!AreEndPointsValid())
        {
            Debug.LogError("StartPoint or EndPoint is not assigned.", gameObject);
            return Vector3.zero;
        }

        return GetWeightedBezierPoint(startPoint.position, currentValue, endPoint.position, t, StartPointWeight, midPointWeight, EndPointWeight);
    }

    private void FixedUpdate()
    {
        if (IsPrefab)
        {
            return;
        }

        if (AreEndPointsValid())
        {
            if (!isFirstFrame)
            {
                SimulatePhysics();
            }

            isFirstFrame = false;
        }
    }

    private void SimulatePhysics()
    {
        float dampingFactor = Mathf.Max(0, 1 - damping * Time.fixedDeltaTime);
        Vector3 acceleration = (targetValue - currentValue) * stiffness * Time.fixedDeltaTime;
        currentVelocity = currentVelocity * dampingFactor + acceleration + otherPhysicsFactors;
        currentValue += currentVelocity * Time.fixedDeltaTime;

        if (Vector3.Distance(currentValue, targetValue) < valueThreshold && currentVelocity.magnitude < velocityThreshold)
        {
            currentValue = targetValue;
            currentVelocity = Vector3.zero;
        }
    }

    private void OnDrawGizmos()
    {
        if (!AreEndPointsValid())
            return;

        Vector3 midPos = GetMidPoint();
        // Uncomment if you need to visualize midpoint
        // Gizmos.color = Color.red;
        // Gizmos.DrawSphere(midPos, 0.2f);
    }

    private bool IsPointsMoved()
    {
        var startPointMoved = startPoint.position != prevStartPointPosition;
        var endPointMoved = endPoint.position != prevEndPointPosition;
        return startPointMoved || endPointMoved;
    }

    private bool IsRopeSettingsChanged()
    {
        var lineQualityChanged = !Mathf.Approximately(linePoints, prevLineQuality);
        var ropeWidthChanged = !Mathf.Approximately(ropeWidth, prevRopeWidth);
        var stiffnessChanged = !Mathf.Approximately(stiffness, prevstiffness);
        var dampnessChanged = !Mathf.Approximately(damping, prevDampness);
        var ropeLengthChanged = !Mathf.Approximately(ropeLength, prevRopeLength);
        var midPointPositionChanged = !Mathf.Approximately(midPointPosition, prevMidPointPosition);
        var midPointWeightChanged = !Mathf.Approximately(midPointWeight, prevMidPointWeight);

        return lineQualityChanged
               || ropeWidthChanged
               || stiffnessChanged
               || dampnessChanged
               || ropeLengthChanged
               || midPointPositionChanged
               || midPointWeightChanged;
    }
}