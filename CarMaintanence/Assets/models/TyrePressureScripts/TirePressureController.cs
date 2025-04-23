using UnityEngine;
using UnityEngine.UI;  // Added for Image component

[RequireComponent(typeof(InflateDeflateMesh))]
public class TirePressureController : MonoBehaviour
{
    [Header("Snap Points")]
    public Transform powerPlugSnapPoint;   // Reference to the power plug snap point
    public Transform valveSnapPoint;        // Reference to the tire valve snap point
    public float snapCheckDistance = 0.05f; // Distance threshold to consider something "snapped"

    [Header("Interactables")]
    public Transform powerPlugInteractable; // The interactable power plug object
    public Transform valveToolInteractable;  // The interactable valve tool/nozzle

    [Header("Pressure Settings")]
    public float minPressureAtm = 10f;      // Minimum pressure (50% deflated)
    public float maxPressureAtm = 40f;      // Maximum pressure (fully inflated) 
    public float currentPressureAtm = 10f;  // Starting at minimum
    public float inflationRate = 5f;        // Atm per second when inflating
    public float deflationRate = 8f;        // Atm per second when deflating

    [Header("UI References (Optional)")]
    public TMPro.TextMeshProUGUI pressureDisplay;
    public TMPro.TextMeshProUGUI modeDisplay;
    public Image pressurePanelImage;  // Reference to the UI panel image component

    [Header("UI Color References")]
    public Color underInflationColor = new Color(75f / 255f, 0f / 255f, 130f / 255f);  // Indigo
    public Color correctInflationColor = Color.green;
    public Color overInflationColor = Color.red;

    // Test controls (for editor)
    [Header("Test Controls (Editor Only)")]
    public bool testPowerPlugConnected = false;
    public bool testValveConnected = false;

    [Header("Audio")]
    public AudioSource powerPlugAudioSource; // Looping hum when power is plugged
    public AudioSource deflateAudioSource;   // Looping hiss when deflating


    // References
    private InflateDeflateMesh inflateDeflateController;
    private bool isPowerPlugConnected = false;
    private bool isValveConnected = false;

    // Mode states - determined automatically based on connections
    private bool inflateMode = false;
    private bool deflateMode = false;

    // Scale conversion - maps pressure (10-40 atm) to sin offset (-1 to 1)
    private float minSinValue = -1f;
    private float maxSinValue = 1f;

    private void Start()
    {
        inflateDeflateController = GetComponent<InflateDeflateMesh>();

        // Set initial inflation to 50% of minimum (partially deflated)
        SetInflationFromPressure(currentPressureAtm);

        // Disable auto update on the InflateDeflateMesh
        // We'll control it manually instead
        inflateDeflateController.autoUpdate = false;

        UpdateUI();

        Debug.Log("TirePressureController initialized.");
    }

    private void Update()
    {
        // In editor, use the test booleans for debugging
        if (Application.isEditor && !Application.isPlaying)
        {
            isPowerPlugConnected = testPowerPlugConnected;
            isValveConnected = testValveConnected;
        }
        else
        {
            // In playmode, check the actual positions of the interactables
            CheckSnapConnections();
        }

        // Update operation mode based on connections
        UpdateOperationMode();

        // Handle pressure changes based on current mode
        if (isValveConnected)
        {
            // Inflate mode - both connections needed
            if (inflateMode && currentPressureAtm < maxPressureAtm)
            {
                // Increase pressure over time
                currentPressureAtm = Mathf.Min(currentPressureAtm + (inflationRate * Time.deltaTime), maxPressureAtm);
                SetInflationFromPressure(currentPressureAtm);
                UpdateUI();
            }
            // Deflate mode - only valve connected, no power
            else if (deflateMode && currentPressureAtm > minPressureAtm)
            {
                // Decrease pressure over time
                currentPressureAtm = Mathf.Max(currentPressureAtm - (deflationRate * Time.deltaTime), minPressureAtm);
                SetInflationFromPressure(currentPressureAtm);
                UpdateUI();
            }
        }
    }

    private void CheckSnapConnections()
    {
        // Check if power plug is snapped
        if (powerPlugSnapPoint != null && powerPlugInteractable != null)
        {
            float distance = Vector3.Distance(powerPlugSnapPoint.position, powerPlugInteractable.position);
            bool wasConnected = isPowerPlugConnected;
            isPowerPlugConnected = (distance < snapCheckDistance);

            // Log connection changes
            if (wasConnected != isPowerPlugConnected)
            {
                Debug.Log("Power plug " + (isPowerPlugConnected ? "connected" : "disconnected"));
            }
        }

        // Check if valve tool is snapped
        if (valveSnapPoint != null && valveToolInteractable != null)
        {
            float distance = Vector3.Distance(valveSnapPoint.position, valveToolInteractable.position);
            bool wasConnected = isValveConnected;
            isValveConnected = (distance < snapCheckDistance);

            // Log connection changes
            if (wasConnected != isValveConnected)
            {
                Debug.Log("Valve " + (isValveConnected ? "connected" : "disconnected"));
            }
        }
    }

    // Determine mode based on connections
    private void UpdateOperationMode()
    {
        bool wasInflating = inflateMode;
        bool wasDeflating = deflateMode;

        // Both connected = inflate mode
        if (isPowerPlugConnected && isValveConnected)
        {
            inflateMode = true;
            deflateMode = false;
        }
        // Only valve connected = deflate mode
        else if (!isPowerPlugConnected && isValveConnected)
        {
            inflateMode = false;
            deflateMode = true;
        }
        // No valve connected = no operation
        else
        {
            inflateMode = false;
            deflateMode = false;
        }

        // Only update UI if something changed
        if (wasInflating != inflateMode || wasDeflating != deflateMode)
        {
            UpdateUI();
        }
        HandleAudioStates();

    }

    // Map pressure (atm) to sin offset
    private void SetInflationFromPressure(float pressure)
    {
        // Normalize pressure to 0-1 range
        float normalizedPressure = Mathf.InverseLerp(minPressureAtm, maxPressureAtm, pressure);

        // Map to sin offset from -1 to 1 
        float sinOffset = Mathf.Lerp(minSinValue, maxSinValue, normalizedPressure);

        // Apply it to the mesh controller
        inflateDeflateController.Inflate(sinOffset);
    }

    private void UpdateUI()
    {
        // Update pressure display
        if (pressureDisplay != null)
        {
            pressureDisplay.text = currentPressureAtm.ToString("F1") + " atm";
        }

        // Update mode display
        if (modeDisplay != null)
        {
            if (inflateMode)
                modeDisplay.text = "MODE: INFLATE";
            else if (deflateMode)
                modeDisplay.text = "MODE: DEFLATE";
            else
                modeDisplay.text = "MODE: STANDBY";
        }

        // Update panel color based on pressure
        UpdatePanelColor();
    }

    // Add this method to update panel color based on pressure
    private void UpdatePanelColor()
    {
        if (pressurePanelImage == null)
            return;

        // Under inflation range (5-31 atm)
        if (currentPressureAtm <= 31f)
        {
            pressurePanelImage.color = underInflationColor;
        }
        // Correct inflation range (31.1-33.9 atm)
        else if (currentPressureAtm > 31f && currentPressureAtm < 34f)
        {
            pressurePanelImage.color = correctInflationColor;
        }
        // Over inflation range (34-40 atm)
        else
        {
            pressurePanelImage.color = overInflationColor;
        }
    }

    private void HandleAudioStates()
    {
        // Power plug sound
        if (isPowerPlugConnected && !powerPlugAudioSource.isPlaying)
        {
            powerPlugAudioSource.loop = true;
            powerPlugAudioSource.Play();
        }
        else if (!isPowerPlugConnected && powerPlugAudioSource.isPlaying)
        {
            powerPlugAudioSource.Stop();
        }

        // Deflation sound
        if (deflateMode && !deflateAudioSource.isPlaying)
        {
            deflateAudioSource.loop = true;
            deflateAudioSource.Play();
        }
        else if (!deflateMode && deflateAudioSource.isPlaying)
        {
            deflateAudioSource.Stop();
        }
    }


    // Public method to manually set pressure (for testing or reset)
    public void SetPressure(float newPressure)
    {
        currentPressureAtm = Mathf.Clamp(newPressure, minPressureAtm, maxPressureAtm);
        SetInflationFromPressure(currentPressureAtm);
        UpdateUI();
    }

    // Public method to reset tire to minimum pressure
    public void ResetTire()
    {
        SetPressure(minPressureAtm);
    }

    // Alternative methods using collider triggers if position checking isn't reliable

    // You can call these methods from the SnapInteractor objects using UnityEvents
    public void NotifyPowerPlugConnected()
    {
        isPowerPlugConnected = true;
        Debug.Log("Power plug connected via direct notification");
    }

    public void NotifyPowerPlugDisconnected()
    {
        isPowerPlugConnected = false;
        Debug.Log("Power plug disconnected via direct notification");
    }

    public void NotifyValveConnected()
    {
        isValveConnected = true;
        Debug.Log("Valve connected via direct notification");
    }

    public void NotifyValveDisconnected()
    {
        isValveConnected = false;
        Debug.Log("Valve disconnected via direct notification");
    }
}