using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FEMPBD_ARAP : MonoBehaviour
{
    public float inflationForce = 0.05f;
    public float elasticity = 10f;
    public float damping = 0.98f;
    public int solverIterations = 5;
    public bool autoUpdate = true; // Toggle for automatic inflation/deflation
    private bool isInflated = false;

    private MeshFilter meshFilter;
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;
    private Vector3[] vertexVelocities;
    private int[] triangles;
    private Dictionary<int, List<int>> adjacencyMap;
    private HashSet<int> constrainedVertices;
    private HashSet<int> smallSharpSurfaces;

    public ComputeShader deformationShader;
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer velocityBuffer;
    private ComputeBuffer normalBuffer;
    private ComputeBuffer adjacencyBuffer;
    private int kernelHandle;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        originalVertices = mesh.vertices;
        deformedVertices = mesh.vertices;
        vertexVelocities = new Vector3[mesh.vertexCount];
        triangles = mesh.triangles;
        constrainedVertices = new HashSet<int>();
        smallSharpSurfaces = new HashSet<int>();

        BuildAdjacencyMap();
        IdentifyConstrainedVertices();
        InitializeComputeShader();
        StartCoroutine(AutoUpdateRoutine());
    }

    void Update()
    {
        if (!autoUpdate)
        {
            RunComputeShader();
            UpdateMesh();
        }
    }

    IEnumerator AutoUpdateRoutine()
    {
        while (autoUpdate)
        {
            isInflated = !isInflated;
            inflationForce = isInflated ? 0.05f : -0.05f; // Toggle inflation/deflation
            for (int i = 0; i < 100; i++) // Gradual transformation over time
            {
                RunComputeShader();
                UpdateMesh();
                yield return new WaitForSeconds(0.02f);
            }
            yield return new WaitForSeconds(1f);
        }
    }

    void InitializeComputeShader()
    {
        kernelHandle = deformationShader.FindKernel("CSMain");

        vertexBuffer = new ComputeBuffer(deformedVertices.Length, sizeof(float) * 3);
        velocityBuffer = new ComputeBuffer(vertexVelocities.Length, sizeof(float) * 3);
        normalBuffer = new ComputeBuffer(mesh.normals.Length, sizeof(float) * 3);
        adjacencyBuffer = new ComputeBuffer(triangles.Length, sizeof(int));

        vertexBuffer.SetData(deformedVertices);
        velocityBuffer.SetData(vertexVelocities);
        normalBuffer.SetData(mesh.normals);
        adjacencyBuffer.SetData(triangles);

        deformationShader.SetBuffer(kernelHandle, "vertices", vertexBuffer);
        deformationShader.SetBuffer(kernelHandle, "velocities", velocityBuffer);
        deformationShader.SetBuffer(kernelHandle, "normals", normalBuffer);
        deformationShader.SetBuffer(kernelHandle, "triangles", adjacencyBuffer);
    }

    void RunComputeShader()
    {
        deformationShader.SetFloat("inflationForce", inflationForce);
        deformationShader.SetFloat("elasticity", elasticity);
        deformationShader.SetFloat("damping", damping);
        deformationShader.SetInt("vertexCount", deformedVertices.Length);
        deformationShader.SetInt("triangleCount", triangles.Length); // Fix: Pass triangle count manually

        deformationShader.Dispatch(kernelHandle, deformedVertices.Length / 64 + 1, 1, 1);

        vertexBuffer.GetData(deformedVertices);
        velocityBuffer.GetData(vertexVelocities);
    }


    void UpdateMesh()
    {
        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
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

    void IdentifyConstrainedVertices()
    {
        for (int i = 0; i < originalVertices.Length; i++)
        {
            foreach (var pair in adjacencyMap)
            {
                int index = pair.Key;
                List<int> neighbors = pair.Value;
                float maxAngle = 0f;

                foreach (int neighbor in neighbors)
                {
                    float angle = Vector3.Angle(mesh.normals[index], mesh.normals[neighbor]);
                    maxAngle = Mathf.Max(maxAngle, angle);
                }

                if (maxAngle > 45f)
                {
                    constrainedVertices.Add(index);
                }
                else if (maxAngle > 75f)
                {
                    smallSharpSurfaces.Add(index);
                }
            }
        }
    }

    void OnDestroy()
    {
        vertexBuffer.Release();
        velocityBuffer.Release();
        normalBuffer.Release();
        adjacencyBuffer.Release();
    }
}
