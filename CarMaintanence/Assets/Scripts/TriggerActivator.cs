using UnityEngine;

public class TriggerActivator : MonoBehaviour
{
    public GameObject targetObject;  // Assign this in the Inspector
    bool switchActive=false;

    void Start()
    {
        targetObject.SetActive(switchActive);
        switchActive=!switchActive;
    }

    public void OnActive()
    {
        targetObject.SetActive(switchActive);
        switchActive = !switchActive;
    }
}
