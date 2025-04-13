using UnityEngine;

public class ScrewDriverHold : MonoBehaviour
{
    bool isHolding = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void onHold()
    {
        isHolding = true;
    }

    public void onRelease() {  isHolding = false; }

    public bool getState()
    {
        return isHolding;
    }
}
