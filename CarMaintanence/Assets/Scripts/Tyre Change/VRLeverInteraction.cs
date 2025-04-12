using UnityEngine;
using UnityEngine.Events;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class VRLeverInteraction : MonoBehaviour
{
    [Header("Lever Settings")]
    [SerializeField] private Transform leverTransform;
    [SerializeField] private float maxLeverRotation = 45f;
    [SerializeField] private float leverReturnSpeed = 5f;
    [SerializeField] private float activationThreshold = 0.7f; // 70% of max rotation
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // Which axis the lever rotates around

    [Header("Events")]
    public UnityEvent onLeverActivated;
    public UnityEvent onLeverReleased;

    // Make this event public so it can be wired up in the Inspector
    [Header("Lever Movement Event")]
    public FloatEvent onLeverMoved = new FloatEvent(); // Reports the lever position from 0-1

    // Define a serializable float event class
    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }

    // Internal state
    private bool isGrabbed = false;
    private bool isActivated = false;
    private Quaternion initialRotation;
    private IInteractableView handGrabInteractable;

    public float CurrentLeverValue;
    private void Awake()
    {
        if (leverTransform == null)
            leverTransform = transform;

        initialRotation = leverTransform.localRotation;

        // Get the HandGrabInteractable component
        handGrabInteractable = GetComponentInChildren<HandGrabInteractable>();
        if (handGrabInteractable == null)
        {
            Debug.LogError("HandGrabInteractable component not found! Please add it to this GameObject or a child.");
            return;
        }

        // Set up event listeners for the Oculus hand grab system
        if (handGrabInteractable != null)
        {
            handGrabInteractable.WhenSelectingInteractorViewAdded += OnHandGrabbed;
            handGrabInteractable.WhenSelectingInteractorViewRemoved += OnHandReleased;
            Debug.Log("Successfully connected HandGrabInteractable events");
        }
        else
        {
            Debug.LogError("Interactable component not found! HandGrabInteractable requires an Interactable.");
        }
    }

    private void OnHandGrabbed(IInteractorView interactor)
    {
        isGrabbed = true;
        Debug.Log("Lever grabbed");
    }

    private void OnHandReleased(IInteractorView interactor)
    {
        isGrabbed = false;
        Debug.Log("Lever released");

        // Fire the release event if it was activated
        if (isActivated)
        {
            isActivated = false;
            onLeverReleased?.Invoke();
        }
    }

    private void Update()
    {
        if (isGrabbed)
        {
            // When grabbed, calculate how far the lever has been pulled/pushed
            float leverValue = CalculateLeverValue();

            // Invoke the continuous movement event
            if (onLeverMoved != null)
            {
                CurrentLeverValue = leverValue;
                onLeverMoved.Invoke(leverValue);
                Debug.Log($"Invoking onLeverMoved with value: {leverValue}");
            }

            // Check if we've crossed the activation threshold
            if (leverValue >= activationThreshold && !isActivated)
            {
                isActivated = true;
                Debug.Log($"Lever activated with value: {leverValue}");
                onLeverActivated?.Invoke();
            }
            else if (leverValue < activationThreshold && isActivated)
            {
                isActivated = false;
                Debug.Log("Lever deactivated");
                onLeverReleased?.Invoke();
            }
        }
        else
        {
            // Return to initial position when not grabbed
            leverTransform.localRotation = Quaternion.Slerp(
                leverTransform.localRotation,
                initialRotation,
                Time.deltaTime * leverReturnSpeed
            );

            // Ensure we report 0 when fully returned
            if (Quaternion.Angle(leverTransform.localRotation, initialRotation) < 1f && onLeverMoved != null)
            {
                CurrentLeverValue = 0;
                onLeverMoved.Invoke(0f);
            }
        }
    }

    private float CalculateLeverValue()
    {
        // Calculate the current angle of the lever relative to its initial rotation
        float currentAngle = 0f;

        // Calculate angle based on specified rotation axis
        if (rotationAxis == Vector3.right)
        {
            currentAngle = leverTransform.eulerAngles.x;
        }
        else if (rotationAxis == Vector3.up)
        {
            currentAngle = leverTransform.eulerAngles.y;
        }
        else if (rotationAxis == Vector3.forward)
        {
            currentAngle = leverTransform.eulerAngles.z;
        }

        // Adjust angle to be between -180 and 180 degrees
        if (currentAngle > 180f)
            currentAngle -= 360f;

        // Calculate normalized value (0 to 1)
        float normalizedValue = Mathf.Clamp01(Mathf.Abs(currentAngle) / maxLeverRotation);

        // Add debug output
        if (normalizedValue > 0.2f)
        {
            Debug.Log($"Lever value: {normalizedValue}, Angle: {currentAngle}");
        }

        return normalizedValue;
    }

    // Method to manually set the lever position (for debugging or remote activation)
    public void SetLeverPosition(float normalizedValue)
    {
        float clampedValue = Mathf.Clamp01(normalizedValue);
        float angle = clampedValue * maxLeverRotation;

        Quaternion targetRotation = initialRotation;

        // Apply rotation based on specified axis
        if (rotationAxis == Vector3.right)
        {
            targetRotation *= Quaternion.Euler(angle, 0, 0);
        }
        else if (rotationAxis == Vector3.up)
        {
            targetRotation *= Quaternion.Euler(0, angle, 0);
        }
        else if (rotationAxis == Vector3.forward)
        {
            targetRotation *= Quaternion.Euler(0, 0, angle);
        }

        leverTransform.localRotation = targetRotation;
    }
}