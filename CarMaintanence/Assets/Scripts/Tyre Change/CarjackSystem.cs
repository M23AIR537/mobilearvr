using UnityEngine;
using Oculus.Interaction;

public class CarJackSystem : MonoBehaviour
{
    [Header("Jack Configuration")]
    [SerializeField] private Transform jackBase;
    [SerializeField] private Transform jackTop;
    [SerializeField] private float maxHeight = 0.5f;
    [SerializeField] private float minHeight = 0.1f;
    [SerializeField] private float liftSpeed = 0.1f;

    [Header("Physics Configuration")]
    [SerializeField] private Rigidbody carRigidbody;
    [SerializeField] private Transform liftPoint;
    [SerializeField] private float maxForce = 20000f;

    [Header("VR Interaction")]
    [SerializeField] private GrabInteractable jackHandle;
    [SerializeField] private GrabInteractor leftInteractor;
    [SerializeField] private GrabInteractor rightInteractor;

    private float currentHeight;
    private bool isLifting;
    private ConfigurableJoint suspensionJoint;
    private Vector3 previousHandPosition;
    private GrabInteractor activeInteractor;

    private void Start()
    {
        currentHeight = minHeight;
        SetupVRInteraction();
        SetupPhysicsJoint();
    }

    private void SetupVRInteraction()
    {
        if (leftInteractor != null)
        {
            leftInteractor.WhenStateChanged += HandleInteractorStateChanged;
        }

        if (rightInteractor != null)
        {
            rightInteractor.WhenStateChanged += HandleInteractorStateChanged;
        }
    }

    private void HandleInteractorStateChanged(InteractorStateChangeArgs args)
    {
        // Check if the interactor is grabbing our handle
        //var interactor = args.interactorObject as GrabInteractor;
        //if (interactor == null || interactor.SelectedInteractable != jackHandle) return;

        if (args.NewState == InteractorState.Select)
        {
            OnHandleGrabbed(rightInteractor);
        }
        else if (args.NewState == InteractorState.Normal)
        {
            OnHandleReleased(rightInteractor);
        }
    }

    private void SetupPhysicsJoint()
    {
        suspensionJoint = gameObject.AddComponent<ConfigurableJoint>();
        suspensionJoint.connectedBody = carRigidbody;
        suspensionJoint.anchor = transform.InverseTransformPoint(jackTop.position);
        suspensionJoint.axis = transform.up;

        var limit = suspensionJoint.linearLimit;
        limit.limit = maxHeight;
        suspensionJoint.linearLimit = limit;

        var drive = suspensionJoint.yDrive;
        drive.positionSpring = 50000f;
        drive.positionDamper = 5000f;
        drive.maximumForce = maxForce;
        suspensionJoint.yDrive = drive;

        // Lock rotation
        suspensionJoint.angularXMotion = ConfigurableJointMotion.Locked;
        suspensionJoint.angularYMotion = ConfigurableJointMotion.Locked;
        suspensionJoint.angularZMotion = ConfigurableJointMotion.Locked;
    }

    private void OnHandleGrabbed(GrabInteractor interactor)
    {
        isLifting = true;
        activeInteractor = interactor;
        previousHandPosition = interactor.transform.position;
    }

    private void OnHandleReleased(GrabInteractor interactor)
    {
        if (interactor == activeInteractor)
        {
            isLifting = false;
            activeInteractor = null;
        }
    }

    private void Update()
    {
        if (isLifting && activeInteractor != null)
        {
            // Calculate movement based on hand position change
            Vector3 currentHandPosition = activeInteractor.transform.position;
            float verticalDelta = currentHandPosition.y - previousHandPosition.y;

            // Update height based on hand movement
            currentHeight = Mathf.Clamp(currentHeight + (verticalDelta * liftSpeed), minHeight, maxHeight);

            // Update jack visual position
            Vector3 newPosition = jackTop.localPosition;
            newPosition.y = currentHeight;
            jackTop.localPosition = newPosition;

            // Update physics joint target position
            suspensionJoint.targetPosition = new Vector3(0, currentHeight, 0);

            previousHandPosition = currentHandPosition;

            // Optional: Add haptic feedback when reaching limits
            if (currentHeight == maxHeight || currentHeight == minHeight)
            {
                bool isRightHand = activeInteractor == rightInteractor;
                OVRInput.SetControllerVibration(0.3f, 0.3f,
                    isRightHand ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (liftPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(liftPoint.position, 0.1f);
            Gizmos.DrawLine(transform.position, liftPoint.position);
        }
    }

    private void OnDestroy()
    {
        // Cleanup event subscriptions
        if (leftInteractor != null)
        {
            leftInteractor.WhenStateChanged -= HandleInteractorStateChanged;
        }

        if (rightInteractor != null)
        {
            rightInteractor.WhenStateChanged -= HandleInteractorStateChanged;
        }
    }
}