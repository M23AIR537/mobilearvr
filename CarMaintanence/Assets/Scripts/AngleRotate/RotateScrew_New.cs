using UnityEngine;

public class RotateScrew_New : MonoBehaviour
{
    public float translationSpeed = 1f; // Speed of translation
    public Transform rotationPivot; // Empty GameObject at the desired pivot

    void Update()
    {

        // Move in the negative local x direction of the pivot
        transform.Translate(-rotationPivot.right * translationSpeed * Time.deltaTime, Space.World);
    }
}
