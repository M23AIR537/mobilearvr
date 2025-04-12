using System.Collections;
using UnityEngine;

public class CarLiftManager : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private HydraulicJackController jackController;
    [SerializeField] private VRLeverInteraction leverInteraction;
    [SerializeField] private Transform carTransform;

    [Header("Lift Settings")]
    [SerializeField] private float maxCarLiftHeight = 0.8f;
    [SerializeField] private float carLiftSpeed = 0.1f;
    [SerializeField] private bool usePhysics = true;
    [SerializeField] private float jackContactThreshold = 0.05f; // How close the jack needs to be to start lifting
    [SerializeField] private Transform carJackPoint; // Reference point on the car where the jack touches

    // Internal state
    private Vector3 carInitialPosition;
    private float currentCarLiftHeight = 0f;
    private bool isCarLifting = false;
    private bool jackInPlace = false;

    private void Awake()
    {
        if (carTransform != null)
            carInitialPosition = carTransform.position;

        if (carJackPoint == null && carTransform != null)
            carJackPoint = carTransform; // Default to car transform if no specific point is set
    }

    private void Start()
    {
        // Connect lever events to jack
        if (leverInteraction != null && jackController != null)
        {
            // Connect the lever's activated event to call our OnLeverPumped method
            leverInteraction.onLeverActivated.AddListener(OnLeverPumped);

            // Connect to the now public onLeverMoved event
            leverInteraction.onLeverMoved.AddListener(OnLeverMoved);
            Debug.Log("Connected to lever events via Start method");
        }
        else
        {
            Debug.LogError("Missing references to LeverInteraction or JackController");
        }

        // Detect if the jack is in place
        CheckJackPosition();
    }

  

    public void OnLeverMoved(float leverValue)
    {
        // This gets called continuously as the lever moves
        // We pass the lever value to the jack controller
        if (jackInPlace && leverValue > 0.2f)
        {
            Debug.Log($"OnLeverMoved called with value: {leverValue}");
            jackController.PumpJack(leverValue);
        }
    }

    private void Update()
    {
        // Check if jack is in correct position
         CheckJackPosition();
        //jackInPlace = true;
        // Update car position based on jack height
        if (jackInPlace && jackController != null)
        {
            UpdateCarPosition();
        }
    }

    private void CheckJackPosition()
    {
        // This checks if the jack is close enough to the car's jack point
        if (jackController != null && carJackPoint != null)
        {
            // Get the position of the jack's lifting point (top of the threaded piston)
            Transform jackLiftPoint = jackController.transform;

            // Check if they're close enough
            float distance = Vector3.Distance(jackLiftPoint.position, carJackPoint.position);

            // Set jackInPlace based on distance check
            bool wasInPlace = jackInPlace;
            jackInPlace = (distance < jackContactThreshold);

            // Log when state changes
            if (jackInPlace != wasInPlace)
            {
                Debug.Log($"Jack in place: {jackInPlace}, Distance: {distance}");
            }
        }
    }

    private void UpdateCarPosition()
    {
        if (!jackInPlace || carTransform == null)
            return;

        // Calculate target height based on jack extension
        float jackExtension = jackController.GetTotalExtension();
        float targetLiftHeight = Mathf.Min(jackExtension, maxCarLiftHeight);

        // Smoothly move the car
        if (Mathf.Abs(currentCarLiftHeight - targetLiftHeight) > 0.001f)
        {
            // Calculate new height with smooth transition
            currentCarLiftHeight = Mathf.MoveTowards(
                currentCarLiftHeight,
                targetLiftHeight,
                carLiftSpeed * Time.deltaTime
            );

            // Apply the new position
            Vector3 newPosition = carInitialPosition;
            newPosition.y += currentCarLiftHeight;

            if (usePhysics && carTransform.GetComponent<Rigidbody>() != null)
            {
                // Use physics to move the car
                carTransform.GetComponent<Rigidbody>().MovePosition(newPosition);
            }
            else
            {
                // Directly set position
                carTransform.position = newPosition;
            }
        }
    }

    private void OnLeverPumped()
    {
        // This gets called when the lever is activated past its threshold
        if (jackInPlace)
        {
            jackController.PumpJack(1.0f);
            Debug.Log("Lever pump action triggered jack");
        }
    }

    // Method to reset everything to initial state
    public void ResetCarLift()
    {
        if (jackController != null)
            jackController.ResetJack();

        if (carTransform != null)
        {
            // Reset car position
            if (usePhysics && carTransform.GetComponent<Rigidbody>() != null)
            {
                carTransform.GetComponent<Rigidbody>().MovePosition(carInitialPosition);
            }
            else
            {
                carTransform.position = carInitialPosition;
            }
        }

        currentCarLiftHeight = 0f;
    }
}