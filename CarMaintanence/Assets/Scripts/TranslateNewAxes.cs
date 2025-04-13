using UnityEngine;

public class TranslateNewAxes : MonoBehaviour
{
    [Header("Target Position for New Axes")]
    public Vector3 newPosition = new Vector3(0, 0, 0); // Set desired new position in Unity world space

    [Header("New Rotation for Local Axes")]
    public Vector3 newRotation = new Vector3(0, 0, 0); // Set desired rotation in degrees (Euler angles)

    [Header("Smooth Translation Settings")]
    public float moveSpeed = 2f; // Speed of movement
    public float rotationSpeed = 100f; // Speed of rotation

    private void Update()
    {
        // Smoothly translate to the new position
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * moveSpeed);

        // Smoothly rotate to the new local axes orientation
        Quaternion targetRotation = Quaternion.Euler(newRotation);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
