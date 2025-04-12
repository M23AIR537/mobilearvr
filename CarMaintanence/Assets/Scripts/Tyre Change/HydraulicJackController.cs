using System.Collections;
using UnityEngine;

public class HydraulicJackController : MonoBehaviour
{
    [Header("Jack Components")]
    [SerializeField] private Transform mainPiston;
    [SerializeField] private Transform threadedPiston;

    [Header("Piston Movement Settings")]
    [SerializeField] private float mainPistonMaxHeight = 0.358677f;
    [SerializeField] private float threadedPistonMaxHeight = 0.7335391f;
    [SerializeField] private float pistonMoveSpeed = 0.05f;
    [SerializeField] private float threadedPistonRotationSpeed = 45f; // degrees per second

    [Header("Interaction Settings")]
    [SerializeField] private int pumpsPerMainPistonIncrement = 1;
    [SerializeField] private float minLeverValueForPump = 0.2f;

    // Internal state variables
    private Vector3 mainPistonInitialPosition;
    private Vector3 threadedPistonInitialPosition;
    private float currentMainPistonHeight = 0f;
    private float currentThreadedPistonHeight = 0f;
    private bool isMainPistonFullyExtended = false;
    private int pumpCount = 0;
    private Coroutine mainPistonCoroutine;
    private Coroutine threadedPistonCoroutine;
    private float lastPumpTime = 0f;
    private float pumpCooldown = 0.5f; // Time between successive pumps

    private void Awake()
    {
        // Store initial positions
        if (mainPiston != null)
            mainPistonInitialPosition = mainPiston.localPosition;

        if (threadedPiston != null)
            threadedPistonInitialPosition = threadedPiston.localPosition;
    }

    public void PumpJack(float leverValue)
    {
        // Don't respond to very small lever movements or if we're in cooldown
        if (leverValue <= minLeverValueForPump || Time.time - lastPumpTime < pumpCooldown)
            return;

        Debug.Log($"Pumping jack with value: {leverValue}");
        pumpCount++;
        lastPumpTime = Time.time;

        // Check if we've pumped enough to move the jack
        if (pumpCount >= pumpsPerMainPistonIncrement)
        {
            pumpCount = 0;

            if (!isMainPistonFullyExtended)
            {
                // Stop any existing coroutine before starting a new one
                if (mainPistonCoroutine != null)
                    StopCoroutine(mainPistonCoroutine);

                // Move main piston first
                mainPistonCoroutine = StartCoroutine(ExtendMainPiston(leverValue));
            }
            else
            {
                // Stop any existing coroutine before starting a new one
                if (threadedPistonCoroutine != null)
                    StopCoroutine(threadedPistonCoroutine);

                // Once main piston is fully extended, start moving threaded piston
                threadedPistonCoroutine = StartCoroutine(ExtendThreadedPiston(leverValue));
            }
        }
    }

    private IEnumerator ExtendMainPiston(float leverStrength = 1.0f)
    {
        Debug.Log("Extending main piston");
        // Calculate how much to move the piston per pump, scaled by lever strength
        float moveIncrement = (mainPistonMaxHeight / (10f * pumpsPerMainPistonIncrement)) * leverStrength;
        float targetHeight = Mathf.Min(currentMainPistonHeight + moveIncrement, mainPistonMaxHeight);

        while (currentMainPistonHeight < targetHeight)
        {
            currentMainPistonHeight += Time.deltaTime * pistonMoveSpeed;
            if (currentMainPistonHeight > targetHeight)
                currentMainPistonHeight = targetHeight;

            // Apply the new position
            Vector3 newPosition = mainPistonInitialPosition;
            newPosition.y += currentMainPistonHeight;
            mainPiston.localPosition = newPosition;

            // Check if we've reached max extension
            if (Mathf.Approximately(currentMainPistonHeight, mainPistonMaxHeight) ||
                currentMainPistonHeight >= mainPistonMaxHeight * 0.99f)
            {
                isMainPistonFullyExtended = true;
                Debug.Log("Main piston fully extended");
            }

            yield return null;
        }

        mainPistonCoroutine = null;
    }

    private IEnumerator ExtendThreadedPiston(float leverStrength = 1.0f)
    {
        Debug.Log("Extending threaded piston");
        // Calculate how much to move the piston per pump, scaled by lever strength
        float moveIncrement = (threadedPistonMaxHeight / (10f * pumpsPerMainPistonIncrement)) * leverStrength;
        float targetHeight = Mathf.Min(currentThreadedPistonHeight + moveIncrement, threadedPistonMaxHeight);

        while (currentThreadedPistonHeight < targetHeight)
        {
            currentThreadedPistonHeight += Time.deltaTime * pistonMoveSpeed;
            if (currentThreadedPistonHeight > targetHeight)
                currentThreadedPistonHeight = targetHeight;

            // Apply the new position
            Vector3 newPosition = threadedPistonInitialPosition;
            newPosition.y += currentThreadedPistonHeight;
            threadedPiston.localPosition = newPosition;

            // Apply rotation to the threaded piston as it extends
            // Scale rotation speed by how much we're extending
            float rotationAmount = threadedPistonRotationSpeed * Time.deltaTime;
            threadedPiston.Rotate(Vector3.up, rotationAmount);

            yield return null;
        }

        threadedPistonCoroutine = null;
    }

    // Method to reset the jack to its initial position
    public void ResetJack()
    {
        // Stop any running coroutines
        if (mainPistonCoroutine != null)
            StopCoroutine(mainPistonCoroutine);

        if (threadedPistonCoroutine != null)
            StopCoroutine(threadedPistonCoroutine);

        mainPistonCoroutine = null;
        threadedPistonCoroutine = null;

        // Reset state variables
        currentMainPistonHeight = 0f;
        currentThreadedPistonHeight = 0f;
        isMainPistonFullyExtended = false;
        pumpCount = 0;

        // Reset positions
        if (mainPiston != null)
            mainPiston.localPosition = mainPistonInitialPosition;

        if (threadedPiston != null)
            threadedPiston.localPosition = threadedPistonInitialPosition;
    }

    // Method to lower the jack (can be called from a UI button or another interaction)
    public void LowerJack()
    {
        StartCoroutine(LowerJackRoutine());
    }

    private IEnumerator LowerJackRoutine()
    {
        // First lower the threaded piston if it's extended
        while (currentThreadedPistonHeight > 0)
        {
            currentThreadedPistonHeight -= Time.deltaTime * pistonMoveSpeed * 2;
            if (currentThreadedPistonHeight < 0)
                currentThreadedPistonHeight = 0;

            // Apply the new position
            Vector3 newPosition = threadedPistonInitialPosition;
            newPosition.y += currentThreadedPistonHeight;
            threadedPiston.localPosition = newPosition;

            // Apply reverse rotation to the threaded piston as it retracts
            threadedPiston.Rotate(Vector3.up, -threadedPistonRotationSpeed * Time.deltaTime);

            yield return null;
        }

        // Then lower the main piston
        while (currentMainPistonHeight > 0)
        {
            currentMainPistonHeight -= Time.deltaTime * pistonMoveSpeed * 2;
            if (currentMainPistonHeight < 0)
                currentMainPistonHeight = 0;

            // Apply the new position
            Vector3 newPosition = mainPistonInitialPosition;
            newPosition.y += currentMainPistonHeight;
            mainPiston.localPosition = newPosition;

            isMainPistonFullyExtended = false;

            yield return null;
        }
    }

    // Public method to get the total extension of both pistons
    public float GetTotalExtension()
    {
        return currentMainPistonHeight + currentThreadedPistonHeight;
    }
}