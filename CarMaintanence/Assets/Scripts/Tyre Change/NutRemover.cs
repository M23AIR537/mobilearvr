using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

// This script allows direct grabbing of loosened lug nuts
public class NutRemover : MonoBehaviour
{
    [SerializeField] private LugNut lugNut;
    [SerializeField] private float grabThreshold = 0.1f; // How close the hand needs to be to grab
    [SerializeField] private HandGrabInteractable handGrabInteractable;

    [Header("Visual Indicator")]
    [SerializeField] private GameObject grabIndicator;
    [SerializeField] private Color unavailableColor = Color.red;
    [SerializeField] private Color availableColor = Color.green;

    private bool isGrabbed = false;
    private Renderer indicatorRenderer;

    private void Awake()
    {
        if (lugNut == null)
            lugNut = GetComponent<LugNut>();

        if (handGrabInteractable == null)
            handGrabInteractable = GetComponentInChildren<HandGrabInteractable>();

        if (handGrabInteractable != null)
        {
            handGrabInteractable.WhenSelectingInteractorViewAdded += OnNutGrabbed;
            handGrabInteractable.WhenSelectingInteractorViewRemoved += OnNutReleased;
        }

        if (grabIndicator != null)
        {
            indicatorRenderer = grabIndicator.GetComponent<Renderer>();

            // Initially hide the indicator
            grabIndicator.SetActive(false);
        }
    }

    private void Update()
    {
        // Update grab indicator visibility and color based on whether the nut is removable
        UpdateGrabIndicator();
    }

    private void OnNutGrabbed(IInteractorView interactor)
    {
        if (lugNut != null && lugNut.IsRemovable())
        {
            isGrabbed = true;
            lugNut.RemoveNut();
            Debug.Log("Lug nut grabbed and removed");
        }
    }

    private void OnNutReleased(IInteractorView interactor)
    {
        isGrabbed = false;
    }

    private void UpdateGrabIndicator()
    {
        if (grabIndicator == null || indicatorRenderer == null)
            return;

        bool isRemovable = lugNut != null && lugNut.IsRemovable();

        // Only show the indicator if the nut is removable
        grabIndicator.SetActive(isRemovable);

        // Set the color based on whether it's currently available
        if (isRemovable && indicatorRenderer != null)
        {
            // Use the material's color property or emission color depending on your setup
            if (indicatorRenderer.material.HasProperty("_EmissionColor"))
            {
                indicatorRenderer.material.SetColor("_EmissionColor", availableColor);
            }
            else
            {
                indicatorRenderer.material.color = availableColor;
            }
        }
    }

    // Optional: Detect hand proximity to show a hover effect
    private void OnTriggerEnter(Collider other)
    {
        // Check for hand proximity
        if (lugNut.IsRemovable() && other.CompareTag("PlayerHand"))
        {
            // Scale up indicator or change its color
            if (grabIndicator != null)
            {
                grabIndicator.transform.localScale = Vector3.one * 1.2f;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Reset indicator when hand leaves
        if (other.CompareTag("PlayerHand") && grabIndicator != null)
        {
            grabIndicator.transform.localScale = Vector3.one;
        }
    }
}