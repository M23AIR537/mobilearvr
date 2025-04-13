using UnityEngine;

public class RotateScrew : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public Transform rotationPivot; // Empty GameObject at the desired pivot

    void Update()
    {
        transform.RotateAround(rotationPivot.position, rotationPivot.right, rotationSpeed * Time.deltaTime);
    }
}

