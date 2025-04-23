using UnityEngine;

public class PressureGaugeNeedle : MonoBehaviour
{
    [Header("Reference to TirePressureController script")]
    public TirePressureController pressureSource;

    [Header("Needle Rotation Settings")]
    public Transform needleTransform;

    [Tooltip("Min and max PSI values shown on the gauge")]
    public float minPSI = 0f;
    public float maxPSI = 220f;

    [Tooltip("Needle X rotation at min and max PSI")]
    public float minNeedleX = -90f; // 0 PSI
    public float maxNeedleX = 22f;  // 220 PSI

    [Tooltip("Fixed Y and Z axis values for the needle")]
    public float fixedY = 90f;
    public float fixedZ = -90f;

    void Start()
    {
        // Always default to the minimum rotation at start
        if (needleTransform != null)
        {
            needleTransform.localRotation = Quaternion.Euler(minNeedleX, fixedY, fixedZ);
        }
    }

    void Update()
    {
        if (pressureSource == null || needleTransform == null)
            return;

        // Convert ATM to PSI
        float currentPSI = pressureSource.currentPressureAtm * 7f;

        // Clamp PSI to gauge range
        float clampedPSI = Mathf.Clamp(currentPSI, minPSI, maxPSI);

        // Interpolation factor (0 to 1)
        float t = clampedPSI / maxPSI;

        // Interpolate only the X angle
        float needleX = Mathf.Lerp(minNeedleX, maxNeedleX, t);

        // Compose the final rotation with fixed Y and Z
        needleTransform.localRotation = Quaternion.Euler(needleX, fixedY, fixedZ);
    }
}
