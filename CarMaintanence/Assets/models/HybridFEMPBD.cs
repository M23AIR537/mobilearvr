using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HybridFEMPBD : MonoBehaviour
{
    // Parameters for inflation, elasticity, and damping
    public float inflationForce = 0.05f; // Force applied to vertices based on normal direction
    public float elasticity = 10f; // Controls how much the mesh resists deformation
    public float damping = 0.98f; // Reduces velocity over time to prevent excessive stretching
    public int solverIterations = 5; // Number of iterations for constraint solving
    public bool autoUpdate = true; // Checkbox to enable/disable automatic updates

    private MeshFilter meshFilter;
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;
    private Vector3[] vertexVelocities;
    private int[] triangles;
    private Dictionary<int, List<int>> adjacencyMap; // Stores vertex neighbors for constraint solving

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        originalVertices = mesh.vertices;
        deformedVertices = mesh.vertices;
        vertexVelocities = new Vector3[mesh.vertexCount];
        triangles = mesh.triangles;

        BuildAdjacencyMap(); // Precompute vertex neighbors for efficient constraint solving
    }

    void Update()
    {
        if (autoUpdate)
        {
            ApplyFEMForces(); // Apply forces based on finite element method principles
            SolvePBDConstraints(); // Enforce positional constraints using PBD
            UpdateMesh(); // Update mesh with new vertex positions
        }
    }

    void ApplyFEMForces()
    {
        for (int i = 0; i < deformedVertices.Length; i++)
        {
            Vector3 normal = mesh.normals[i]; // Get vertex normal
            if (!float.IsFinite(normal.x) || !float.IsFinite(normal.y) || !float.IsFinite(normal.z))
                continue; // Skip invalid normals

            Vector3 force = normal * inflationForce; // Calculate force based on normal
            vertexVelocities[i] += force * Time.deltaTime; // Apply force to velocity
        }
    }

    void SolvePBDConstraints()
    {
        for (int iter = 0; iter < solverIterations; iter++) // Iterate multiple times for stability
        {
            foreach (var pair in adjacencyMap)
            {
                int index = pair.Key;
                List<int> neighbors = pair.Value;

                if (neighbors.Count == 0) continue; // Prevent division by zero

                Vector3 avgNeighborPos = Vector3.zero;
                foreach (int neighbor in neighbors)
                {
                    avgNeighborPos += deformedVertices[neighbor]; // Compute average position of neighbors
                }
                avgNeighborPos /= neighbors.Count;

                Vector3 correction = (avgNeighborPos - deformedVertices[index]) * elasticity * Time.deltaTime; // Apply correction to enforce smooth deformation
                deformedVertices[index] += correction;
            }
        }
    }

    void UpdateMesh()
    {
        for (int i = 0; i < deformedVertices.Length; i++)
        {
            if (!float.IsFinite(vertexVelocities[i].x) || !float.IsFinite(vertexVelocities[i].y) || !float.IsFinite(vertexVelocities[i].z))
                continue; // Skip invalid velocities

            deformedVertices[i] += vertexVelocities[i]; // Apply velocity to vertex position
            vertexVelocities[i] *= damping; // Apply damping to reduce velocity over time
        }

        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals(); // Update normals for correct shading
        mesh.RecalculateBounds(); // Update bounds for correct physics interactions
    }

    void BuildAdjacencyMap()
    {
        adjacencyMap = new Dictionary<int, List<int>>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v0 = triangles[i];
            int v1 = triangles[i + 1];
            int v2 = triangles[i + 2];

            AddNeighbor(v0, v1);
            AddNeighbor(v1, v2);
            AddNeighbor(v2, v0);
        }
    }

    void AddNeighbor(int v1, int v2)
    {
        if (!adjacencyMap.ContainsKey(v1)) adjacencyMap[v1] = new List<int>();
        if (!adjacencyMap.ContainsKey(v2)) adjacencyMap[v2] = new List<int>();

        if (!adjacencyMap[v1].Contains(v2)) adjacencyMap[v1].Add(v2);
        if (!adjacencyMap[v2].Contains(v1)) adjacencyMap[v2].Add(v1);
    }
}
