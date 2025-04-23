using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshFaceExpander : MonoBehaviour
{
    [Header("Expansion Settings")]
    [SerializeField] private float minExpansion = 0f;
    [SerializeField] private float maxExpansion = 0.5f;
    [SerializeField] private float expansionSpeed = 1.0f;
    [SerializeField] private bool autoAnimate = true;

    private Mesh originalMesh;
    private Mesh clonedMesh;
    private Vector3[] originalVertices;
    private Vector3[] originalNormals;
    private float currentExpansion = 0f;
    private bool expanding = true;

    // Start is called before the first frame update
    void Start()
    {
        // Get the original mesh
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        originalMesh = meshFilter.sharedMesh;

        // Create a clone of the mesh to modify
        clonedMesh = new Mesh();
        clonedMesh.vertices = originalMesh.vertices;
        clonedMesh.triangles = originalMesh.triangles;
        clonedMesh.normals = originalMesh.normals;
        clonedMesh.uv = originalMesh.uv;

        // Save the original data
        originalVertices = originalMesh.vertices;
        originalNormals = originalMesh.normals;

        // Assign the cloned mesh to the mesh filter
        meshFilter.mesh = clonedMesh;
    }

    // Update is called once per frame
    void Update()
    {
        if (autoAnimate)
        {
            // Update expansion direction
            if (expanding)
            {
                currentExpansion += expansionSpeed * Time.deltaTime;
                if (currentExpansion >= maxExpansion)
                {
                    currentExpansion = maxExpansion;
                    expanding = false;
                }
            }
            else
            {
                currentExpansion -= expansionSpeed * Time.deltaTime;
                if (currentExpansion <= minExpansion)
                {
                    currentExpansion = minExpansion;
                    expanding = true;
                }
            }

            UpdateMesh(currentExpansion);
        }
    }

    public void UpdateMesh(float expansionAmount)
    {
        // Create new vertices array
        Vector3[] newVertices = new Vector3[originalVertices.Length];

        // Move each vertex along its normal direction
        for (int i = 0; i < originalVertices.Length; i++)
        {
            newVertices[i] = originalVertices[i] + (originalNormals[i] * expansionAmount);
        }

        // Apply the new vertices to the mesh
        clonedMesh.vertices = newVertices;

        // Recalculate bounds to ensure proper rendering
        clonedMesh.RecalculateBounds();
    }

    // Manually set the expansion amount
    public void SetExpansion(float amount)
    {
        currentExpansion = Mathf.Clamp(amount, minExpansion, maxExpansion);
        UpdateMesh(currentExpansion);
    }

    // Reset mesh to original state
    public void ResetMesh()
    {
        currentExpansion = 0f;
        UpdateMesh(0f);
    }
}