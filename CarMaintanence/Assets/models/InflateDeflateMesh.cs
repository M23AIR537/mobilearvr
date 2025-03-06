using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class InflateDeflateMesh : MonoBehaviour
{
    public float inflationAmount = 0.1f;
    public float tensileStrength = 1.0f;
    public float thickness = 1.0f;
    public float flexibility = 1.0f;
    public float normalAngleThreshold = 30f;
    public bool autoUpdate = false;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] modifiedVertices;
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
        modifiedVertices = new Vector3[originalVertices.Length];
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
        ApplySurfaceDeformation(intensity);
    }

    public void Deflate(float intensity)
    {
        ApplySurfaceDeformation(-intensity);
    }

    private void ApplySurfaceDeformation(float amount)
    {
        for (int i = 0; i < originalVertices.Length; i++)
        {
            if (borderVertices.Contains(i))
            {
                // Keep border vertices unchanged
                modifiedVertices[i] = originalVertices[i];
                continue;
            }

            // Compute falloff factor based on distance to the nearest border vertex
            float falloff = ComputeFalloff(originalVertices[i]);
            Vector3 deformation = normals[i] * amount * inflationAmount * flexibility * falloff / (thickness + 0.1f);
            modifiedVertices[i] = originalVertices[i] + deformation;
        }

        mesh.vertices = modifiedVertices;
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
}
