using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OilDrop : MonoBehaviour
{

    public Animator Tap;
    public GameObject openText;
    public GameObject closeText;

    public ParticleSystem RunningWater;

    public AudioSource openSound;

    private bool isOpen = false;
    private bool isClosed = true;
    private bool inReach = false;

    public void ReachSet(bool inUse)
    {
        if (inUse)
        {
            if (isClosed)
            {
                inReach = true;
                openText.SetActive(true);
            }

            if (isOpen)
            {
                inReach = true;
                closeText.SetActive(true);
            }
        }
        else
        {
            inReach = false;
            openText.SetActive(false);
            closeText.SetActive(false);
        }
        

    }

    void Start()
    {
        closeText.SetActive(false);
        openText.SetActive(false);
        RunningWater.Stop();
    }

    void Update()
    {
        // Detect if VR Controller's Primary Index Trigger is pressed
        if (inReach && isClosed && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            OpenTap();
        }
        else if (inReach && isOpen && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            CloseTap();
        }
    }
    // VR-based Proximity Detection
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Hand"))
        {
            inReach = true;
            if (isClosed) openText.SetActive(true);
            if (isOpen) closeText.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Hand"))
        {
            inReach = false;
            openText.SetActive(false);
            closeText.SetActive(false);
        }
    }

    private void OpenTap()
    {
        Tap.SetBool("Open", true);
        Tap.SetBool("Closed", false);
        openText.SetActive(false);
        openSound.Play();
        isOpen = true;
        isClosed = false;
        RunningWater.Play();
    }

    private void CloseTap()
    {
        Tap.SetBool("Open", false);
        Tap.SetBool("Closed", true);
        closeText.SetActive(false);
        openSound.Pause();
        isClosed = true;
        isOpen = false;
        RunningWater.Stop();
    }    
}
