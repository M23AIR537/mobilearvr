using UnityEngine;
using System.Linq;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class TirePressureDeformation : MonoBehaviour
{
    [Header("Tire Pressure Simulation")]
    public float pressureLevel = 1f;  // 0 (completely flat) to 1 (fully inflated)
    public float maxDeformationAmount = 0.2f;  // Maximum deformation distance

    [Header("Deformation Parameters")]
    public float centralBulgeFactor = 1.5f;  // How much the center bulges
    public float rimStiffnessFactor = 0.5f;  // How rigid the rim area is

    [Header("Auto Update Settings")]
    public bool autoUpdate = false;
    public float autoUpdateFrequency = 1f;
    public float autoUpdateAmplitude = 0.1f;
    public AutoUpdateType autoUpdateType = AutoUpdateType.Sine;

    [Header("Debug")]
    public bool debugVisualization = false;

    // Auto update tracking
    private float autoUpdateTimer = 0f;

    // Mesh Components
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;
    private Vector3 meshCenter;

    // Enum for auto update types
    public enum AutoUpdateType
    {
        Sine,
        Triangle,
        Random
    }

    void Start()
    {
        InitializeMesh();
    }

    void InitializeMesh()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        deformedVertices = new Vector3[originalVertices.Length];
        System.Array.Copy(originalVertices, deformedVertices, originalVertices.Length);

        // Calculate mesh center
        meshCenter = CalculateMeshCenter();
    }

    Vector3 CalculateMeshCenter()
    {
        Vector3 center = Vector3.zero;
        foreach (Vector3 vertex in originalVertices)
        {
            center += vertex;
        }
        return center / originalVertices.Length;
    }

    public void SimulateTirePressure(float pressureOverride = -1f)
    {
        // Use provided pressure or current pressure level
        float currentPressure = pressureOverride >= 0 ? pressureOverride : pressureLevel;

        // Reset to original vertices
        System.Array.Copy(originalVertices, deformedVertices, originalVertices.Length);

        // Calculate deformation based on pressure level
        for (int i = 0; i < deformedVertices.Length; i++)
        {
            Vector3 vertexDeformation = CalculatePressureDeformation(i, currentPressure);
            deformedVertices[i] += vertexDeformation;
        }

        // Update mesh
        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshCollider>().sharedMesh = mesh;

        // Debug visualization
        if (debugVisualization)
        {
            VisualizeDeformation();
        }
    }

    Vector3 CalculatePressureDeformation(int vertexIndex, float currentPressure)
    {
        Vector3 vertex = originalVertices[vertexIndex];
        Vector3 toCenter = vertex - meshCenter;

        // Calculate distance from mesh center (normalized)
        float distanceFromCenter = toCenter.magnitude;
        float normalizedDistance = distanceFromCenter / (distanceFromCenter + 0.001f);

        // Pressure-based deformation
        float deformationStrength = Mathf.Lerp(0, maxDeformationAmount, currentPressure);

        // Apply different deformation based on distance from center
        Vector3 deformationDirection = toCenter.normalized;

        // Central area bulges more
        float centralBulge = Mathf.Exp(-normalizedDistance * centralBulgeFactor);

        // Rim area is stiffer
        float rimStiffness = Mathf.Clamp01(1 - normalizedDistance * rimStiffnessFactor);

        // Calculate final deformation
        Vector3 pressureDeformation = deformationDirection
            * deformationStrength
            * centralBulge
            * (1 - rimStiffness);

        return pressureDeformation;
    }

    void Update()
    {
        // Manual inflation/deflation
        if (Input.GetKey(KeyCode.UpArrow))
        {
            // Inflate
            pressureLevel = Mathf.Clamp01(pressureLevel + Time.deltaTime * 0.5f);
            SimulateTirePressure();
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            // Deflate
            pressureLevel = Mathf.Clamp01(pressureLevel - Time.deltaTime * 0.5f);
            SimulateTirePressure();
        }

        // Auto update logic
        if (!autoUpdate) return;

        autoUpdateTimer += Time.deltaTime;

        // Calculate pressure intensity based on update type
        float pressureIntensity = CalculateAutoUpdateIntensity();

        // Periodically apply deformation
        if (autoUpdateTimer >= 1f / autoUpdateFrequency)
        {
            // Transform intensity to pressure level (0-1 range)
            float newPressureLevel = Mathf.Clamp01(pressureLevel + pressureIntensity);
            SimulateTirePressure(newPressureLevel);
            autoUpdateTimer = 0f;
        }
    }

    float CalculateAutoUpdateIntensity()
    {
        switch (autoUpdateType)
        {
            case AutoUpdateType.Sine:
                return Mathf.Sin(Time.time * autoUpdateFrequency) * autoUpdateAmplitude;

            case AutoUpdateType.Triangle:
                // Triangle wave oscillation
                float t = Mathf.Repeat(Time.time * autoUpdateFrequency, 1f);
                return (t < 0.5f ? t * 2 : (1 - t) * 2) * autoUpdateAmplitude * 2 - autoUpdateAmplitude;

            case AutoUpdateType.Random:
                return Random.Range(-autoUpdateAmplitude, autoUpdateAmplitude);

            default:
                return 0f;
        }
    }

    void VisualizeDeformation()
    {
        // Visualize tire deformation
        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 originalPos = transform.TransformPoint(originalVertices[i]);
            Vector3 deformedPos = transform.TransformPoint(deformedVertices[i]);

            // Draw line showing vertex movement
            Debug.DrawLine(originalPos, deformedPos, Color.red, 0.1f);
        }
    }

    public void ResetTire()
    {
        System.Array.Copy(originalVertices, deformedVertices, originalVertices.Length);
        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshCollider>().sharedMesh = mesh;
        pressureLevel = 1f;
    }
}