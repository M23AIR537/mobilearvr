using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class VertexInflateDeflateMesh : MonoBehaviour
{
    public float inflationAmount = 0.1f;
    public float tensileStrength = 1.0f;
    public float thickness = 1.0f;
    public float flexibility = 1.0f;
    public float normalAngleThreshold = 30f;
    public bool autoUpdate = false;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] currentVertices;
    private Vector3[] normals;
    private HashSet<int> borderVertices;

    void Start()
    {
        InitializeMesh();
    }

    void InitializeMesh()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        currentVertices = new Vector3[originalVertices.Length];
        System.Array.Copy(originalVertices, currentVertices, originalVertices.Length);

        normals = mesh.normals;
        borderVertices = DetectBorderVertices();
    }

    private HashSet<int> DetectBorderVertices()
    {
        HashSet<int> borderVerts = new HashSet<int>();
        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            if (IsEdge(v1, v2)) { borderVerts.Add(v1); borderVerts.Add(v2); }
            if (IsEdge(v2, v3)) { borderVerts.Add(v2); borderVerts.Add(v3); }
            if (IsEdge(v3, v1)) { borderVerts.Add(v3); borderVerts.Add(v1); }
        }
        return borderVerts;
    }

    private bool IsEdge(int v1, int v2)
    {
        return Vector3.Angle(normals[v1], normals[v2]) > normalAngleThreshold;
    }

    public void Inflate(float intensity)
    {
        ApplyVertexDeformation(intensity);
    }

    public void Deflate(float intensity)
    {
        ApplyVertexDeformation(-intensity);
    }

    private void ApplyVertexDeformation(float amount)
    {
        // Reset to original vertices before applying new deformation
        System.Array.Copy(originalVertices, currentVertices, originalVertices.Length);

        for (int i = 0; i < originalVertices.Length; i++)
        {
            if (borderVertices.Contains(i))
            {
                // Keep border vertices unchanged
                continue;
            }

            // Compute falloff factor based on distance to the nearest border vertex
            float falloff = ComputeFalloff(originalVertices[i]);

            // Calculate vertex movement along its normal
            Vector3 vertexMovement = normals[i] * amount * inflationAmount * flexibility * falloff / (thickness + 0.1f);

            // Move the vertex
            currentVertices[i] += vertexMovement;
        }

        // Update mesh with new vertex positions
        mesh.vertices = currentVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    private float ComputeFalloff(Vector3 vertex)
    {
        float minDistance = float.MaxValue;
        foreach (int borderIndex in borderVertices)
        {
            float distance = Vector3.Distance(vertex, originalVertices[borderIndex]);
            if (distance < minDistance) minDistance = distance;
        }

        // Smoothstep Falloff (0 at borders, 1 at farthest points)
        float maxDist = 0.2f; // Adjust based on model scale
        return Mathf.SmoothStep(0, 1, Mathf.Clamp01(minDistance / maxDist));
    }

    void Update()
    {
        if (autoUpdate)
        {
            Inflate(Mathf.Sin(Time.time) * 0.05f);
        }
    }

    // Method to reset to original mesh state
    public void ResetMesh()
    {
        mesh.vertices = originalVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshCollider>().sharedMesh = mesh;
        System.Array.Copy(originalVertices, currentVertices, originalVertices.Length);
    }
}