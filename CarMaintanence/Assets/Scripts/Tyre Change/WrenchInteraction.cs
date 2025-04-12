using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using System.Collections;
using UnityEngine.Events;

public class WrenchInteraction : MonoBehaviour
{
    [Header("Wrench Settings")]
    [SerializeField] private Transform wrenchTransform;
    [SerializeField] private float rotationThreshold = 25f; // Degrees of rotation needed to loosen a nut
    [SerializeField] private int rotationsNeeded = 6; // Number of full rotations needed to remove a nut
    [SerializeField] private float wrenchReturnSpeed = 5f;

    [Header("Debug Options")]
    [SerializeField] private bool visualizeRotation = true;
    [SerializeField] private LineRenderer rotationVisualizer;

    [Header("Events")]
    public UnityEvent onNutFullyLoosened;
    public FloatEvent onRotationProgress = new FloatEvent(); // 0-1 progress value

    // Define a serializable float event class
    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }

    // Internal state
    private bool isGrabbed = false;
    private Vector3 lastWrenchDirection;
    private float currentRotationAngle = 0f;
    private int completedRotations = 0;
    private bool isClockwiseRotation = false;
    private bool wasRotatedPastThreshold = false;
    private IInteractableView handGrabInteractable;
    private LugNut currentLugNut;
    private Quaternion initialRotation;
    public static bool IsRotating;
    public Rigidbody CarRigidBody;
    private void Awake()
    {
        if (wrenchTransform == null)
            wrenchTransform = transform;

        initialRotation = wrenchTransform.localRotation;

        // Get the HandGrabInteractable component
        handGrabInteractable = GetComponentInChildren<HandGrabInteractable>();
        if (handGrabInteractable == null)
        {
            Debug.LogError("HandGrabInteractable component not found! Please add it to this GameObject or a child.");
            return;
        }

        // Setup line renderer for visualization if needed
        if (visualizeRotation && rotationVisualizer == null)
        {
            rotationVisualizer = gameObject.AddComponent<LineRenderer>();
            rotationVisualizer.startWidth = 0.01f;
            rotationVisualizer.endWidth = 0.01f;
            rotationVisualizer.positionCount = 2;
            rotationVisualizer.material = new Material(Shader.Find("Sprites/Default"));
            rotationVisualizer.startColor = Color.green;
            rotationVisualizer.endColor = Color.green;
        }

        // Set up event listeners for the Oculus hand grab system
        if (handGrabInteractable != null)
        {
            handGrabInteractable.WhenSelectingInteractorViewAdded += OnWrenchGrabbed;
            handGrabInteractable.WhenSelectingInteractorViewRemoved += OnWrenchReleased;
            Debug.Log("Successfully connected HandGrabInteractable events for wrench");
        }
    }

    private void OnWrenchGrabbed(IInteractorView interactor)
    {
        CarRigidBody.isKinematic = true;
        isGrabbed = true;
        lastWrenchDirection = GetWrenchDirection();
        Debug.Log("Wrench grabbed");
    }

    private void OnWrenchReleased(IInteractorView interactor)
    {
        CarRigidBody.isKinematic = false;
        isGrabbed = false;
        Debug.Log("Wrench released");

        // Reset rotation when wrench is released
        StartCoroutine(ReturnWrenchToNeutral());
    }

    private void Update()
    {
        if (isGrabbed && currentLugNut != null)
        {
            TrackWrenchRotation();

            // Visualize rotation if enabled
            if (visualizeRotation && rotationVisualizer != null)
            {
                UpdateRotationVisualizer();
            }
        }
    }

    private void TrackWrenchRotation()
    {
        Vector3 currentDirection = GetWrenchDirection();

        // Calculate angle between last and current direction
        float deltaAngle = Vector3.SignedAngle(lastWrenchDirection, currentDirection, transform.up);

        // Ignore very small movements
        if (Mathf.Abs(deltaAngle) < 1f)
        {
            lastWrenchDirection = currentDirection;
            return;
        }

        // Track accumulated rotation
        currentRotationAngle += deltaAngle;

        // Determine rotation direction (clockwise or counterclockwise)
        bool isCurrentlyClockwise = deltaAngle < 0;

        // Check if we've completed a threshold of rotation
        if (Mathf.Abs(currentRotationAngle) >= rotationThreshold && !wasRotatedPastThreshold)
        {
            wasRotatedPastThreshold = true;
            isClockwiseRotation = isCurrentlyClockwise;
            currentLugNut.ApplyRotation(isClockwiseRotation ? -1 : 1);

            // Increment rotation counter
            completedRotations++;

            // Report progress (0-1)
            float progress = (float)completedRotations / rotationsNeeded;
            onRotationProgress.Invoke(progress);

            Debug.Log($"Rotation threshold reached: {completedRotations}/{rotationsNeeded}, Direction: {(isClockwiseRotation ? "Clockwise" : "Counter-Clockwise")}");

            // Check if we've fully loosened the nut
            if (completedRotations >= rotationsNeeded)
            {
                Debug.Log("Nut fully loosened!");
                onNutFullyLoosened.Invoke();
                currentLugNut.SetFullyLoosened(true);
            }
        }
        else if (Mathf.Abs(currentRotationAngle) < rotationThreshold && wasRotatedPastThreshold)
        {
            // Reset rotation tracking once we go back below threshold
            wasRotatedPastThreshold = false;
        }

        // Update last direction
        lastWrenchDirection = currentDirection;
    }

    private Vector3 GetWrenchDirection()
    {
        // This represents the direction the wrench's "handle" is pointing
        // Adjust based on your wrench model's orientation
        return wrenchTransform.forward;
    }

    private void UpdateRotationVisualizer()
    {
        if (rotationVisualizer != null)
        {
            rotationVisualizer.SetPosition(0, wrenchTransform.position);
            rotationVisualizer.SetPosition(1, wrenchTransform.position + GetWrenchDirection() * 0.2f);

            // Color based on progress
            float progress = (float)completedRotations / rotationsNeeded;
            rotationVisualizer.startColor = Color.Lerp(Color.red, Color.green, progress);
            rotationVisualizer.endColor = rotationVisualizer.startColor;
        }
    }

    private IEnumerator ReturnWrenchToNeutral()
    {
        // Optionally reset wrench rotation when released
        while (!isGrabbed && Vector3.Angle(wrenchTransform.forward, transform.forward) > 1f)
        {
            wrenchTransform.localRotation = Quaternion.Slerp(
                wrenchTransform.localRotation,
                initialRotation,
                Time.deltaTime * wrenchReturnSpeed
            );
            yield return null;
        }
    }

    // Called when wrench is placed on a lug nut
    public void SetCurrentLugNut(LugNut lugNut)
    {
        if (lugNut != currentLugNut)
        {
            // Reset rotation tracking when switching to a new nut
            ResetRotationTracking();
        }

        currentLugNut = lugNut;
        Debug.Log($"Wrench connected to lug nut: {lugNut.name}");
    }

    // Called when wrench is removed from a lug nut
    public void ClearCurrentLugNut()
    {
        currentLugNut = null;
        ResetRotationTracking();
        Debug.Log("Wrench disconnected from lug nut");
    }

    // Reset rotation tracking variables
    private void ResetRotationTracking()
    {
        currentRotationAngle = 0f;
        completedRotations = 0;
        wasRotatedPastThreshold = false;
        onRotationProgress.Invoke(0f);
    }
}