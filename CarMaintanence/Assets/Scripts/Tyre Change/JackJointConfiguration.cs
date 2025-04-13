using UnityEngine;

[RequireComponent(typeof(ConfigurableJoint))]
public class JackJointConfiguration : MonoBehaviour
{
    [Header("Connected Objects")]
    [SerializeField] private Rigidbody connectedCar;
    [SerializeField] private Transform jackTopPoint;

    [Header("Motion Limits")]
    [SerializeField] private float maxVerticalMovement = 0.5f;
    [SerializeField] private float minVerticalMovement = 0f;
    [SerializeField] private bool lockHorizontalMovement = true;

    [Header("Joint Drive Settings")]
    [SerializeField] private float positionSpringForce = 50000f;
    [SerializeField] private float positionDamping = 5000f;
    [SerializeField] private float maxForce = 20000f;

    [Header("Angular Limits")]
    [SerializeField] private float angularLimit = 0f;
    [SerializeField] private bool lockRotation = true;

    private ConfigurableJoint joint;

    private void Awake()
    {
        InitializeJoint();
    }

    private void InitializeJoint()
    {
        joint = GetComponent<ConfigurableJoint>();
        if (joint == null)
        {
            Debug.LogError("ConfigurableJoint component not found!");
            return;
        }

        SetupJointConnection();
        ConfigureMotionLimits();
        ConfigureDriveSettings();
        ConfigureRotationLimits();
    }

    private void SetupJointConnection()
    {
        if (connectedCar == null)
        {
            Debug.LogWarning("No connected car Rigidbody assigned!");
            return;
        }

        joint.connectedBody = connectedCar;
        joint.anchor = transform.InverseTransformPoint(jackTopPoint.position);
        joint.axis = transform.up;

        // Auto-configure connected anchor based on closest point on car
        Vector3 closestPoint = connectedCar.ClosestPointOnBounds(jackTopPoint.position);
        joint.connectedAnchor = connectedCar.transform.InverseTransformPoint(closestPoint);
    }

    private void ConfigureMotionLimits()
    {
        // Configure linear limits
        var linearLimit = new SoftJointLimit
        {
            limit = maxVerticalMovement,
            bounciness = 0,
            contactDistance = 0
        };
        joint.linearLimit = linearLimit;

        // Set motion types for each axis
        if (lockHorizontalMovement)
        {
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
        }
        else
        {
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;
        }

        joint.yMotion = ConfigurableJointMotion.Limited;
    }

    private void ConfigureDriveSettings()
    {
        // Configure drive settings for vertical movement
        var yDrive = new JointDrive
        {
            positionSpring = positionSpringForce,
            positionDamper = positionDamping,
            maximumForce = maxForce
        };
        joint.yDrive = yDrive;

        // Optionally configure XZ drives if horizontal movement is allowed
        if (!lockHorizontalMovement)
        {
            var xzDrive = new JointDrive
            {
                positionSpring = positionSpringForce * 0.5f,
                positionDamper = positionDamping * 0.5f,
                maximumForce = maxForce * 0.5f
            };
            joint.xDrive = xzDrive;
            joint.zDrive = xzDrive;
        }
    }

    private void ConfigureRotationLimits()
    {
        if (lockRotation)
        {
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
        }
        else
        {
            // Configure angular limits if rotation is allowed
            var angularXLimit = new SoftJointLimit
            {
                limit = angularLimit,
                bounciness = 0,
                contactDistance = 0
            };

            //joint.angularXLimit = angularXLimit;
            joint.angularYLimit = angularXLimit;
            joint.angularZLimit = angularXLimit;

            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;
        }
    }

    public void UpdateJointTargetPosition(float height)
    {
        if (joint == null) return;

        // Clamp height between min and max values
        height = Mathf.Clamp(height, minVerticalMovement, maxVerticalMovement);

        // Update joint target position
        joint.targetPosition = new Vector3(0, height, 0);
    }

    public void SetJointBreakForce(float breakForce)
    {
        if (joint == null) return;

        joint.breakForce = breakForce;
        joint.breakTorque = breakForce;
    }

    private void OnDrawGizmos()
    {
        if (jackTopPoint != null)
        {
            // Visualize joint connection points
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(jackTopPoint.position, 0.05f);

            if (connectedCar != null)
            {
                Gizmos.color = Color.blue;
                Vector3 connectedPoint = connectedCar.transform.TransformPoint(joint?.connectedAnchor ?? Vector3.zero);
                Gizmos.DrawLine(jackTopPoint.position, connectedPoint);
                Gizmos.DrawWireSphere(connectedPoint, 0.05f);
            }
        }
    }
}