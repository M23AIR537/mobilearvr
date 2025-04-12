using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using System.Collections;

public class WrenchSnapToNut : MonoBehaviour
{
    [Header("Snap Settings")]
    [SerializeField] private Transform wrenchTransform;
    [SerializeField] private Transform wrenchHeadTransform; // The "socket" part of the wrench
    [SerializeField] private float snapDistance = 0.1f; // How close the wrench head needs to be to a nut to snap
    [SerializeField] private float snapForce = 10f; // How strongly to pull the wrench into position
    [SerializeField] private float rotationMatchSpeed = 10f; // How quickly to rotate the wrench to match the nut

    [Header("Attachment Settings")]
    [SerializeField] private bool disableWrenchPhysics = true; // Whether to disable physics while attached
    [SerializeField] private bool maintainGrabPoint = true; // Keep the grab point offset consistent while snapped

    [Header("Audio Feedback")]
    [SerializeField] private AudioClip snapSound;
    [SerializeField] private AudioClip detachSound;

    // References
    private LugNut currentNut;
    private Rigidbody wrenchRigidbody;
    private HandGrabInteractable handGrabInteractable;
    private AudioSource audioSource;

    // Internal state
    private bool isSnapped = false;
    private bool isGrabbed = false;
    private Vector3 grabPointOffset; // Offset from wrench to hand when grabbed
    private Quaternion initialRotationOffset; // Initial rotation difference between wrench and nut
    private Transform originalParent;

    private void Awake()
    {
        // Get references
        if (wrenchTransform == null)
            wrenchTransform = transform;

        if (wrenchHeadTransform == null)
            wrenchHeadTransform = wrenchTransform; // Default to the wrench's transform if no specific head is set

        wrenchRigidbody = GetComponent<Rigidbody>();
        handGrabInteractable = GetComponentInChildren<HandGrabInteractable>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null && (snapSound != null || detachSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D sound
            audioSource.playOnAwake = false;
        }

        originalParent = transform.parent;

        // Set up event listeners for the Oculus hand grab system
        if (handGrabInteractable != null)
        {
            handGrabInteractable.WhenSelectingInteractorViewAdded += OnWrenchGrabbed;
            handGrabInteractable.WhenSelectingInteractorViewRemoved += OnWrenchReleased;
        }
    }

    private void OnWrenchGrabbed(IInteractorView interactor)
    {
        isGrabbed = true;

        // If we're snapped and want to maintain grab point, store the current position
        if (isSnapped && maintainGrabPoint)
        {
            // Instead of using interactor transform directly, we'll use the current wrench position
            // relative to the nut as our reference point
            if (currentNut != null)
            {
                grabPointOffset = wrenchTransform.position - currentNut.transform.position;
                grabPointOffset = Quaternion.Inverse(wrenchTransform.rotation) * grabPointOffset;
            }
        }

        // If grabbed while snapped, detach from the nut
        if (isSnapped && !WrenchInteraction.IsRotating)
        {
            DetachFromNut();
        }
    }

    private void OnWrenchReleased(IInteractorView interactor)
    {
        isGrabbed = false;

        // Optional: If released and close to a nut, snap to it
        LugNut nearbyNut = FindNearestNut();
        if (nearbyNut != null && !isSnapped)
        {
            AttachToNut(nearbyNut);
        }
    }

    private void Update()
    {
        // If not snapped and grabbed, check for nearby nuts
        if (!isSnapped && isGrabbed)
        {
            LugNut nearbyNut = FindNearestNut();
            if (nearbyNut != null)
            {
                AttachToNut(nearbyNut);
            }
        }

        // If snapped, maintain position relative to the nut
        if (isSnapped && currentNut != null && isGrabbed)
        {
            // We'll handle the positioning without directly using the interactor transform
            // This will be managed by the HandGrabInteractable component from Oculus

            // We can still apply constraints or additional positioning logic here
            // if needed for improving the feel of the interaction
        }
    }

    // Find the nearest lug nut within snap distance
    private LugNut FindNearestNut()
    {
        // Look for all lug nuts in the scene
        LugNut[] nuts = FindObjectsOfType<LugNut>();
        LugNut nearestNut = null;
        float nearestDistance = snapDistance;

        foreach (LugNut nut in nuts)
        {
            // Skip nuts that are already removed
            if (nut.IsRemoved)
                continue;

            // Use the nut's socket point if available
            Transform nutPoint = nut.GetSocketPoint();
            float distance = Vector3.Distance(wrenchHeadTransform.position, nutPoint.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestNut = nut;
            }
        }

        return nearestNut;
    }

    // Attach the wrench to a lug nut
    public void AttachToNut(LugNut nut)
    {
        if (isSnapped || nut == null || nut.IsRemoved)
            return;

        currentNut = nut;
        isSnapped = true;

        // Store the initial rotation difference between wrench and nut
        initialRotationOffset = Quaternion.Inverse(nut.transform.rotation) * wrenchTransform.rotation;

        // Parent the wrench to the nut or use fixed joint
        if (wrenchRigidbody != null && nut.GetComponent<Rigidbody>() != null)
        {
            // Option 1: Use a joint (more physical)
            FixedJoint joint = gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = nut.GetComponent<Rigidbody>();
            joint.breakForce = float.PositiveInfinity;
            joint.breakTorque = float.PositiveInfinity;

            // Option 2: Disable physics
            if (disableWrenchPhysics)
            {
                wrenchRigidbody.isKinematic = true;
            }
        }
        else
        {
            // If no rigidbodies, just parent the transform
            transform.parent = nut.transform;
        }

        // Align wrench head with nut
        StartCoroutine(AlignWrenchWithNut());

        // Notify the WrenchInteraction that we're connected to this nut
        WrenchInteraction wrenchInteraction = GetComponent<WrenchInteraction>();
        if (wrenchInteraction != null)
        {
            wrenchInteraction.SetCurrentLugNut(nut);
        }

        // Play snap sound
        if (audioSource != null && snapSound != null)
        {
            audioSource.clip = snapSound;
            audioSource.Play();
        }

        Debug.Log("Wrench snapped to nut: " + nut.name);
    }

    // Detach the wrench from the nut
    public void DetachFromNut()
    {
        if (!isSnapped || currentNut == null)
            return;

        // Restore original parent
        transform.parent = originalParent;

        // Remove joint if it exists
        FixedJoint joint = GetComponent<FixedJoint>();
        if (joint != null)
        {
            Destroy(joint);
        }

        // Restore rigidbody state
        if (wrenchRigidbody != null && disableWrenchPhysics)
        {
            wrenchRigidbody.isKinematic = false;
        }

        // Notify WrenchInteraction
        WrenchInteraction wrenchInteraction = GetComponent<WrenchInteraction>();
        if (wrenchInteraction != null)
        {
            wrenchInteraction.ClearCurrentLugNut();
        }

        // Play detach sound
        if (audioSource != null && detachSound != null)
        {
            audioSource.clip = detachSound;
            audioSource.Play();
        }

        Debug.Log("Wrench detached from nut");

        isSnapped = false;
        currentNut = null;
    }

    // Smoothly align the wrench with the nut
    private IEnumerator AlignWrenchWithNut()
    {
        if (currentNut == null)
            yield break;

        // Calculate target position (socket part of wrench aligned with nut socket)
        Transform nutSocket = currentNut.GetSocketPoint();
        Vector3 targetPosition = nutSocket.position;

        // Calculate target rotation (maintain original offset)
        Quaternion targetRotation = currentNut.transform.rotation * initialRotationOffset;

        float elapsedTime = 0f;
        float alignDuration = 0.2f; // How long the alignment takes

        Vector3 startPosition = wrenchTransform.position;
        Quaternion startRotation = wrenchTransform.rotation;

        while (elapsedTime < alignDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / alignDuration);

            // Update position and rotation
            wrenchTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            wrenchTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        // Ensure exact final position
        wrenchTransform.position = targetPosition;
        wrenchTransform.rotation = targetRotation;
    }

    // When something enters the wrench's trigger area
    private void OnTriggerEnter(Collider other)
    {
        if (!isGrabbed || isSnapped)
            return;

        // Check if it's a lug nut
        LugNut nut = other.GetComponentInParent<LugNut>();
        if (nut != null && !nut.IsRemoved)
        {
            AttachToNut(nut);
        }
    }
}

