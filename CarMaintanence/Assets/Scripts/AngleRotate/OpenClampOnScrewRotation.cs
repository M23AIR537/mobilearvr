using UnityEngine;

public class OpenClampOnScrewRotation : MonoBehaviour
{
    public Transform screw; // The rotating screw
    public Transform clamp; // The clamp object
    public float openAmount = 0.1f; // The maximum amount to open the clamp
    public float rotationThreshold = 30f; // Degrees of screw rotation to fully open the clamp

    private float initialScrewRotation;

    void Start()
    {
        // Store the initial rotation angle of the screw
        initialScrewRotation = screw.localEulerAngles.x;
    }

    void Update()
    {
        // Calculate the rotation delta of the screw
        float currentRotation = screw.localEulerAngles.x;
        float rotationDelta = Mathf.DeltaAngle(initialScrewRotation, currentRotation);

        // Calculate the normalized opening factor (0 to 1)
        float openFactor = Mathf.Clamp(rotationDelta / rotationThreshold, 0f, 1f);

        // Apply scaling to simulate clamp opening
        clamp.localScale = new Vector3(1 + openFactor * openAmount, 1, 1);
    }
}
