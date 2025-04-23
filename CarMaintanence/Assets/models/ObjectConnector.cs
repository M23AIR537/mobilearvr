using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ObjectConnector : MonoBehaviour
{
    [Header("Connection Points")]
    [SerializeField] private Transform startObject;
    [SerializeField] private Transform endObject;

    [Header("Line Settings")]
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Color lineColor = Color.white;
    [SerializeField] private bool useWorldSpace = true;

    private LineRenderer lineRenderer;

    void Start()
    {
        // Get the line renderer component
        lineRenderer = GetComponent<LineRenderer>();

        // Initialize the line renderer
        SetupLineRenderer();

        // Validate references
        if (startObject == null || endObject == null)
        {
            Debug.LogError("ObjectConnector requires both start and end objects to be assigned!");
            enabled = false;
        }
    }

    void Update()
    {
        // Update the line positions to match the objects
        UpdateLinePositions();
    }

    private void SetupLineRenderer()
    {
        lineRenderer.useWorldSpace = useWorldSpace;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material.color = lineColor;
        lineRenderer.positionCount = 2; // Line with two points
    }

    private void UpdateLinePositions()
    {
        if (startObject != null && endObject != null)
        {
            // Set the start and end positions of the line
            lineRenderer.SetPosition(0, startObject.position);
            lineRenderer.SetPosition(1, endObject.position);
        }
    }

    // Public method to change connected objects at runtime
    public void SetConnectedObjects(Transform start, Transform end)
    {
        startObject = start;
        endObject = end;
    }

    // Public method to change line appearance at runtime
    public void SetLineAppearance(float width, Color color)
    {
        lineWidth = width;
        lineColor = color;

        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.material.color = color;
    }
}
