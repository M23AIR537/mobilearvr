using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using System.Linq;
using UnityEngine;

public class VRObjectPlacement : MonoBehaviour
{
    public Transform initialSpot; // The designated spot for the object to snap back to
    public string snapZoneTag = "SnapZone"; // Tag for identifying the snap zone

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private HandGrabInteractable _interactable;
    private GrabInteractable _grabInteractable;
    private DistanceGrabInteractable _interact2;
    private DistanceHandGrabInteractable _interact3;
    private bool isHeld = false;

    void Start()
    {
        // Store the initial position and rotation of the object
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // Fetch all interactables
        _interactable = gameObject.GetComponent<HandGrabInteractable>();
        _grabInteractable = gameObject.GetComponent<GrabInteractable>();
        _interact2 = gameObject.GetComponent<DistanceGrabInteractable>();
        _interact3 = gameObject.GetComponent<DistanceHandGrabInteractable>();
    }

    void Update()
    {
        // Check if any of the interactors are holding the object
        isHeld = (_interactable?.Interactors.FirstOrDefault() != null) ||
                 (_grabInteractable?.Interactors.FirstOrDefault() != null) ||
                 (_interact2?.Interactors.FirstOrDefault() != null) ||
                 (_interact3?.Interactors.FirstOrDefault() != null);

        Debug.Log($"Object isHeld: {isHeld}");
    }

    private void OnTriggerStay(Collider other)
    {
        // Check if object is not held and is in the correct snap zone
        if (!isHeld && other.CompareTag(snapZoneTag))
        {
            Debug.Log("Object entered snap zone and is not held.");
            SnapToInitialSpot();
        }
    }

    private void SnapToInitialSpot()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        Debug.Log("Object snapped to initial position.");
    }
}
