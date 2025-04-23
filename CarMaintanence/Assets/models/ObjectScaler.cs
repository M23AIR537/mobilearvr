using UnityEngine;

public class ObjectScaler : MonoBehaviour
{
    [Header("Scaling Settings")]
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1.5f;
    [SerializeField] private float scalingSpeed = 1.0f;

    private bool growingPhase = true;

    // Update is called once per frame
    void Update()
    {
        // Calculate scaling direction
        float scaleDirection = growingPhase ? 1.0f : -1.0f;

        // Apply scaling
        transform.localScale += new Vector3(1, 1, 1) * (scaleDirection * scalingSpeed * Time.deltaTime);

        // Check if we need to switch direction
        if (transform.localScale.x >= maxScale)
        {
            growingPhase = false;
            // Clamp to max scale to prevent overshooting
            transform.localScale = new Vector3(maxScale, maxScale, maxScale);
        }
        else if (transform.localScale.x <= minScale)
        {
            growingPhase = true;
            // Clamp to min scale to prevent undershooting
            transform.localScale = new Vector3(minScale, minScale, minScale);
        }
    }
}