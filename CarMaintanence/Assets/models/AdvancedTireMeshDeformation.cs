using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class AdvancedTireMeshDeformation : MonoBehaviour
{
    [Header("Deformation Parameters")]
    public float inflationAmount = 0.1f;
    public float surfaceAngleThreshold = 15f;
    public float minSurfaceArea = 0.01f;
    public float scaleFactor = 1.0f;
    public float boundaryConstraintFactor = 0.5f;

    [Header("Auto Update Settings")]
    public bool autoUpdate = false;
    public float autoUpdateFrequency = 1f;
    public float autoUpdateAmplitude = 0.05f;
    public AutoUpdateType autoUpdateType = AutoUpdateType.Sine;

    [Header("Performance & Debug")]
    public bool debugVisualization = false;

    // Mesh Components
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;
    private Vector3[] normals;
    private int[] triangles;

    // Surface classification
    private HashSet<int> largeSurfaceVertices;
    private HashSet<int> smallSurfaceVertices;
    private HashSet<int> boundaryVertices;

    // Auto update tracking
    private float autoUpdateTimer = 0f;

    public enum AutoUpdateType
    {
        Sine,
        Triangle,
        Random
    }

    void Start()
    {
        InitializeMesh();
        ClassifySurfaces();
        IdentifyBoundaryVertices();
    }

    void InitializeMesh()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        deformedVertices = new Vector3[originalVertices.Length];
        System.Array.Copy(originalVertices, deformedVertices, originalVertices.Length);

        normals = mesh.normals;
        triangles = mesh.triangles;
    }

    void IdentifyBoundaryVertices()
    {
        boundaryVertices = new HashSet<int>();
        Dictionary<int, HashSet<int>> vertexConnections = new Dictionary<int, HashSet<int>>();

        // Build vertex connection map
        for (int i = 0; i < triangles.Length; i += 3)
        {
            for (int j = 0; j < 3; j++)
            {
                int vertexIndex = triangles[i + j];
                if (!vertexConnections.ContainsKey(vertexIndex))
                {
                    vertexConnections[vertexIndex] = new HashSet<int>();
                }
            }

            // Add connected vertices
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            vertexConnections[v1].Add(v2);
            vertexConnections[v1].Add(v3);
            vertexConnections[v2].Add(v1);
            vertexConnections[v2].Add(v3);
            vertexConnections[v3].Add(v1);
            vertexConnections[v3].Add(v2);
        }

        // Identify boundary vertices
        foreach (var kvp in vertexConnections)
        {
            int vertexIndex = kvp.Key;
            HashSet<int> connectedVertices = kvp.Value;

            // Check normal variations of connected vertices
            bool isBoundary = connectedVertices.Any(connectedIndex =>
                Vector3.Angle(normals[vertexIndex], normals[connectedIndex]) > surfaceAngleThreshold);

            if (isBoundary)
            {
                boundaryVertices.Add(vertexIndex);
            }
        }
    }

    void ClassifySurfaces()
    {
        largeSurfaceVertices = new HashSet<int>();
        smallSurfaceVertices = new HashSet<int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            float triangleArea = CalculateTriangleArea(
                originalVertices[v1],
                originalVertices[v2],
                originalVertices[v3]
            );

            float normalVariation = Vector3.Angle(normals[v1], normals[v2]);
            normalVariation += Vector3.Angle(normals[v2], normals[v3]);
            normalVariation += Vector3.Angle(normals[v3], normals[v1]);
            normalVariation /= 3f;

            if (triangleArea > minSurfaceArea && normalVariation < surfaceAngleThreshold)
            {
                largeSurfaceVertices.Add(v1);
                largeSurfaceVertices.Add(v2);
                largeSurfaceVertices.Add(v3);
            }
            else
            {
                smallSurfaceVertices.Add(v1);
                smallSurfaceVertices.Add(v2);
                smallSurfaceVertices.Add(v3);
            }
        }
    }

    float CalculateTriangleArea(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        return 0.5f * Vector3.Cross(v2 - v1, v3 - v1).magnitude;
    }

    public void Deform(float intensity)
    {
        System.Array.Copy(originalVertices, deformedVertices, originalVertices.Length);

        // Deform large surfaces
        foreach (int vertexIndex in largeSurfaceVertices)
        {
            if (boundaryVertices.Contains(vertexIndex))
            {
                // Constrain boundary vertices
                Vector3 falloffDirection = CalculateFalloffDirection(vertexIndex);
                deformedVertices[vertexIndex] += falloffDirection * intensity * inflationAmount * boundaryConstraintFactor;
            }
            else
            {
                Vector3 falloffDirection = CalculateFalloffDirection(vertexIndex);
                deformedVertices[vertexIndex] += falloffDirection * intensity * inflationAmount;
            }
        }

        // Scale small surfaces
        foreach (int vertexIndex in smallSurfaceVertices)
        {
            if (boundaryVertices.Contains(vertexIndex))
            {
                // Constrain boundary vertices of small surfaces
                Vector3 referenceNormal = FindNearestLargeSurfaceNormal(vertexIndex);
                deformedVertices[vertexIndex] += referenceNormal * intensity * scaleFactor * boundaryConstraintFactor;
            }
            else
            {
                Vector3 referenceNormal = FindNearestLargeSurfaceNormal(vertexIndex);
                deformedVertices[vertexIndex] += referenceNormal * intensity * scaleFactor;
            }
        }

        // Update mesh
        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    Vector3 CalculateFalloffDirection(int vertexIndex)
    {
        Vector3 averageNearbyNormal = Vector3.zero;
        int nearbyCount = 0;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            if (triangles[i] == vertexIndex ||
                triangles[i + 1] == vertexIndex ||
                triangles[i + 2] == vertexIndex)
            {
                averageNearbyNormal += (normals[triangles[i]] +
                                        normals[triangles[i + 1]] +
                                        normals[triangles[i + 2]]) / 3f;
                nearbyCount++;
            }
        }

        return nearbyCount > 0 ? (averageNearbyNormal / nearbyCount).normalized : normals[vertexIndex];
    }

    Vector3 FindNearestLargeSurfaceNormal(int vertexIndex)
    {
        float minDistance = float.MaxValue;
        Vector3 nearestNormal = normals[vertexIndex];

        foreach (int largeVertexIndex in largeSurfaceVertices)
        {
            float distance = Vector3.Distance(originalVertices[vertexIndex], originalVertices[largeVertexIndex]);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestNormal = normals[largeVertexIndex];
            }
        }

        return nearestNormal;
    }

    void Update()
    {
        if (!autoUpdate) return;

        autoUpdateTimer += Time.deltaTime;

        // Calculate deformation intensity based on update type
        float intensity = CalculateAutoUpdateIntensity();

        // Periodically apply deformation
        if (autoUpdateTimer >= 1f / autoUpdateFrequency)
        {
            Deform(intensity);
            autoUpdateTimer = 0f;
        }

        // Debug visualization
        if (debugVisualization)
        {
            VisualizeVertexClassification();
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

    // Method to manually reset to original mesh
    public void ResetMesh()
    {
        System.Array.Copy(originalVertices, deformedVertices, originalVertices.Length);
        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    // Optional debug visualization
    void VisualizeVertexClassification()
    {
        if (!debugVisualization) return;

        // Visualize large surface vertices
        foreach (int vertexIndex in largeSurfaceVertices)
        {
            Debug.DrawRay(
                transform.TransformPoint(originalVertices[vertexIndex]),
                transform.TransformDirection(normals[vertexIndex]) * 0.1f,
                boundaryVertices.Contains(vertexIndex) ? Color.yellow : Color.green
            );
        }

        // Visualize small surface vertices
        foreach (int vertexIndex in smallSurfaceVertices)
        {
            Debug.DrawRay(
                transform.TransformPoint(originalVertices[vertexIndex]),
                transform.TransformDirection(normals[vertexIndex]) * 0.1f,
                boundaryVertices.Contains(vertexIndex) ? Color.magenta : Color.red
            );
        }
    }
}