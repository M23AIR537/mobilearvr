using UnityEngine;

public class RotateScrewAndControlClamp_New : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public Transform rotationPivot; // Pivot point for the screw
    public Transform clamp; // The clamp object to expand and contract
    public Transform screwdriver; // The VR screwdriver object
    public Transform Hinge;

    public float openAmount = 0.1f; // Maximum expansion factor for the clamp
    public float rotationDuration = 10f; // Duration of forward rotation

    private float accumulatedRotation = 0f;
    private Vector3 initialClampScale;

    private float previousScrewdriverRotation = 0f;
    private bool isScrewdriverInContact = false;
    private string currentTag;
    private int counter =0;
    float elapsedTime = 0;

    void Start()
    {
        initialClampScale = clamp.localScale;
        previousScrewdriverRotation = screwdriver.localEulerAngles.z;
        currentTag = clamp.tag;
    }

    void Update()
    {
        if (!isScrewdriverInContact) return;
        
        float currentRotation = screwdriver.localEulerAngles.z;
        float rotationDelta = Mathf.DeltaAngle(previousScrewdriverRotation, currentRotation);

        if (rotationDelta != 0f)
        {
            if (rotationDelta > 0f)
            {
                // Clockwise rotation (tighten)
                RotateAndContract(-rotationDelta);
            }
            else
            {
                // Counter-clockwise rotation (loosen)
                RotateAndExpand(+rotationDelta);
            }
        }

        previousScrewdriverRotation = currentRotation;
    }

    private void RotateAndExpand(float rotationDelta)
    {
        transform.RotateAround(rotationPivot.position, rotationPivot.right, rotationDelta * Time.deltaTime * rotationSpeed);
        accumulatedRotation += rotationDelta * Time.deltaTime;
        elapsedTime += Time.deltaTime;
        float factor = Mathf.Clamp01(accumulatedRotation/ rotationDuration);
        ExpandClamp(factor);
        if(elapsedTime >= rotationDuration)
        {
            clamp.tag = "Loose";
            currentTag = clamp.tag;
            elapsedTime = 0;
        }
        
    }

    private void RotateAndContract(float rotationDelta)
    {
        transform.RotateAround(rotationPivot.position, -rotationPivot.right, rotationDelta * Time.deltaTime * rotationSpeed);
        accumulatedRotation -= rotationDelta * Time.deltaTime;
        float factor = Mathf.Clamp01(accumulatedRotation / rotationDuration);
        ExpandClamp(factor);
        if(elapsedTime >= rotationDuration)
        {
            clamp.tag = "Tight";
            currentTag = clamp.tag;
            elapsedTime = 0;
        }
    }

    // Method to scale the clamp based on a normalized factor (0 to 1)
    private void ExpandClamp(float factor)
    {
        clamp.localScale = initialClampScale + new Vector3(openAmount * factor, 0, 0);
    }

    // Detect when the screwdriver is in contact with the screw
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("COllision Detected");
        if (other.transform == screwdriver)
        {
            isScrewdriverInContact = true;
            if(clamp.tag == currentTag)
            {
                screwdriver.transform.position = Hinge.transform.position;
                if (counter == 0)
                {
                    screwdriver.transform.rotation = Hinge.transform.rotation;
                }
            }
            counter++;
        }
    }

    // Detect when the screwdriver leaves the screw
    private void OnTriggerExit(Collider other)
    {
        if (other.transform == screwdriver)
        {
            isScrewdriverInContact = false;
            counter= 0;
        }
    }
}
