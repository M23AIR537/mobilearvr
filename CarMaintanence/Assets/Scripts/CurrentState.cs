using TMPro;
using UnityEngine;

public class CurrentState : MonoBehaviour
{
    public TMP_Text state;
    public GameObject clamp;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        state.SetText("Current State: "+clamp.tag);
    }
}
