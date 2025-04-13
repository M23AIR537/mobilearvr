using UnityEngine;

public class ColorLerpToRed : MonoBehaviour
{
    public float duration = 10f;          // Duration of the lerp
    private float elapsedTime = 0f;       // Time since the lerp started
    private Material mat;                 // The material of the object
    private Color startColor;             // Original color
    private Color targetColor = Color.white;
    private bool isLerping = true;
    private Material startMat;
    public Material endMat;

    void Start()
    {
        // Get the object's material (creates a unique instance at runtime)
        mat = GetComponent<Renderer>().material;

        startMat = mat;
        // Save the starting color of the material
        startColor = mat.color;
    }

    void Update()
    {
        if (isLerping)
        {
            // Increase time
            elapsedTime += Time.deltaTime;

            // Calculate t value between 0 and 1
            float t = Mathf.Clamp01(elapsedTime / duration);

            

            // Lerp the color
            mat.Lerp(startMat, endMat, t);

            //mat.SetColor("_BaseMap", t < 0.5f ? startColor : targetColor);

            // Stop lerping after duration
            if (t >= 1f)
            {
                isLerping = false;
            }
        }
    }
}
