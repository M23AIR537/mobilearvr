using UnityEngine;

public class RotateScrewAndControlClamp : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public Transform rotationPivot; // Empty GameObject at the desired pivot
    public Transform clamp; // The clamp object to expand and contract

    public float openAmount = 0.1f; // Maximum expansion factor for the clamp
    public float rotationDuration = 10f; // Duration of forward rotation
    public float pauseDuration = 5f; // Duration of pause before reversing

    private float elapsedTime = 0f;
    private bool isRotatingForward = true;
    private bool isRotatingBackwards = false;
    private bool isPaused = false;
    private bool isPausedRev = false;

    private Vector3 initialClampScale;
    public GameObject screwDriver;
    private ScrewDriverHold driverstate;
    private bool isHolding = false;
    private bool isActive = false;
    private bool startRot = false;

    void Start()
    {
        initialClampScale = clamp.localScale;
        driverstate = screwDriver.GetComponent<ScrewDriverHold>();
    }

    void Update()
    {
        isHolding = driverstate.getState();

        elapsedTime += Time.deltaTime;

        if (isRotatingForward && startRot)
        {
            if (elapsedTime <= rotationDuration)
            {
                // Rotate forward and expand clamp
                transform.RotateAround(rotationPivot.position, rotationPivot.right, rotationSpeed * Time.deltaTime);
                ExpandClamp(elapsedTime / rotationDuration);
            }
            else
            {
                clamp.tag = "Loose";
                // Switch to pause state
                isRotatingForward = false;
                //isPaused = true;
                elapsedTime = 0f;
            }
        }
        else if (isPaused)
        {
            if (elapsedTime >= pauseDuration)
            {
                // End pause and start reverse rotation
                isPaused = false;
                elapsedTime = 0f;
            }
        }
        else if (isPausedRev)
        {
            if (elapsedTime >= pauseDuration)
            {
                // End pause and start reverse rotation
                //isRotatingForward = true;
                //isPausedRev = false;
                elapsedTime = 0f;
            }
        }
        else if (isRotatingBackwards && startRot)
        {
            if (elapsedTime <= rotationDuration)
            {
                // Rotate backward and contract clamp
                transform.RotateAround(rotationPivot.position, -rotationPivot.right, rotationSpeed * Time.deltaTime);
                ExpandClamp(1f - (elapsedTime / rotationDuration));
            }
            else
            {
                clamp.tag = "Tight";
                // Reset for the next cycle
                isRotatingBackwards = false;
                //isPausedRev = true;
                elapsedTime = 0f;
            }
        }
    }

    // Method to scale the clamp based on a normalized factor (0 to 1)
    private void ExpandClamp(float factor)
    {
        clamp.localScale = initialClampScale + new Vector3(openAmount * factor, 0, 0);
    }

    public void onActive()
    {
        isActive = true;
        Debug.Log("DEbugging:isActive:"+isActive);
    }

    public void rotateForward()
    {
        isRotatingForward = true;
        isRotatingBackwards = false;
        Debug.Log("DEbugging:IsRotate Back:" + isRotatingBackwards);
    }

    public void rotateReverse()
    {
        isRotatingForward = false;
        isRotatingBackwards = true;
        Debug.Log("DEbugging:IsRotate Back:" + isRotatingBackwards);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collision Detected-for Rotation");
        if (other.CompareTag("ScrewDriver"))
        {
            Debug.Log("Screwdriver detected");
        }
        if (other.CompareTag("ScrewDriver") && isHolding && isActive)
        {
            startRot= true;
        }
        else
        {
            startRot = false;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        startRot = false;
    }
}
