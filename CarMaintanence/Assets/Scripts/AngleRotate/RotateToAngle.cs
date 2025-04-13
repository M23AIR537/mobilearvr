using UnityEngine;
using System.Collections;

public class RotateToAngle : MonoBehaviour
{
    public float targetAngle = -90f; // Target angle in degrees (negative for counter-clockwise)
    public float rotationSpeed = 100f; // Speed of rotation (degrees per second)

    private bool rotate = false;

    private IEnumerator ExecuteAfterDelay()
    {
        Debug.Log("Waiting for 5 seconds...");
        yield return new WaitForSeconds(5f); // Wait for 5 seconds
    }

    void Update()
    {
        // Calculate the target rotation
        Quaternion targetRotation = Quaternion.Euler(targetAngle, 0, 0);

        // Smoothly rotate towards the target angle
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Stop rotating when the target is reached
        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            transform.rotation = targetRotation;
            rotate = false;
        }
        
    }

    // Call this method to start the rotation
}
