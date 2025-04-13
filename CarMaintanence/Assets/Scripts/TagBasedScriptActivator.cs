using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

public class DynamicScriptActivator : MonoBehaviour
{
    public GameObject targetObject; // The GameObject to check
    public string requiredTag = "Loose"; // The tag to check on the target object
    //public string scriptTypeName = "OVRGrabbable"; // The name of the script to activate/deactivate

    

    void Start()
    {
        
    }

    void Update()
    {
        GrabInteractable targetScript = GetComponent<GrabInteractable>();
        HandGrabInteractable targetScript1 = GetComponent<HandGrabInteractable>();
        
        if (targetObject != null && targetScript != null)
        {
            if (targetObject.CompareTag(requiredTag))
            {
                //EnableScript(true); // Activate the script if the tag matches
                targetScript.enabled = true;
                targetScript1.enabled = true;
                //targetScript2.enabled = true;
                //targetScript3.enabled = true;
            }
            else
            {
                //EnableScript(false); // Deactivate if the tag doesn't match
                targetScript.enabled = false;
                targetScript1.enabled = false;
                
            }
        }
    }

   
}
