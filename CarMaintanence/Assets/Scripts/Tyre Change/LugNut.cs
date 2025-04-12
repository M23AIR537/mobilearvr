using UnityEngine;
using System.Collections;
using Oculus.Interaction;

public class LugNut : MonoBehaviour
{
    [Header("Lug Nut Settings")]
    [SerializeField] private Transform nutTransform;
    [SerializeField] private float rotationSpeed = 45f; // Degrees per second
    [SerializeField] private float looseningDistance = 0.1f; // How far the nut moves out when fully loosened
    [SerializeField] private Vector3 rotationAxis = Vector3.forward; // Default rotation axis
    [SerializeField] private GameObject nutModel; // The visual model of the nut

    [Header("Optional")]
    [SerializeField] private Material standardMaterial;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material loosenedMaterial;
    [SerializeField] private AudioClip looseningSound;
    [SerializeField] private AudioClip removedSound;

    // Internal state
    private Vector3 initialPosition;
    private bool isFullyLoosened = false;
    private bool isRemoved = false;
    private AudioSource audioSource;
    private Renderer nutRenderer;
    private Coroutine currentAnimationCoroutine;
    public bool IsRemoved;
    
    private void Awake()
    {
        if (nutTransform == null)
            nutTransform = transform;

        initialPosition = nutTransform.localPosition;

        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (looseningSound != null || removedSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D sound
            audioSource.playOnAwake = false;
        }

        // Get renderer for material changes
        if (nutModel != null)
        {
            nutRenderer = nutModel.GetComponent<Renderer>();
        }
        else
        {
            nutRenderer = GetComponent<Renderer>();
        }
    }

    // Called by the WrenchInteraction when rotated
    public void ApplyRotation(float rotationAmount)
    {
        if (isRemoved)
            return;

        // Rotate the nut
        nutTransform.Rotate(rotationAxis, rotationAmount * rotationSpeed);

        // Play loosening sound
        if (audioSource != null && looseningSound != null)
        {
            audioSource.clip = looseningSound;
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
    }

    // Set whether this nut is fully loosened
    public void SetFullyLoosened(bool loosened)
    {
        if (isFullyLoosened == loosened || isRemoved)
            return;

        isFullyLoosened = loosened;

        if (isFullyLoosened)
        {
            // Start the loosening animation if it's now fully loosened
            if (currentAnimationCoroutine != null)
                StopCoroutine(currentAnimationCoroutine);

            currentAnimationCoroutine = StartCoroutine(AnimateNutLoosening());

            // Change material if available
            if (nutRenderer != null && loosenedMaterial != null)
            {
                nutRenderer.material = loosenedMaterial;
            }
        }
    }

    // Remove the nut completely
    public void RemoveNut()
    {
        if (!isFullyLoosened || isRemoved)
            return;

        isRemoved = true;

        // Play removal sound
        if (audioSource != null && removedSound != null)
        {
            audioSource.clip = removedSound;
            audioSource.Play();
        }

        // Start removal animation
        if (currentAnimationCoroutine != null)
            StopCoroutine(currentAnimationCoroutine);

        currentAnimationCoroutine = StartCoroutine(AnimateNutRemoval());
    }

    // Animate the nut loosening (moving outward)
    private IEnumerator AnimateNutLoosening()
    {
        Vector3 targetPosition = initialPosition + transform.TransformDirection(rotationAxis.normalized * looseningDistance);
        float moveSpeed = 0.05f;

        while (Vector3.Distance(nutTransform.localPosition, targetPosition) > 0.001f)
        {
            nutTransform.localPosition = Vector3.MoveTowards(
                nutTransform.localPosition,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        nutTransform.localPosition = targetPosition;
        currentAnimationCoroutine = null;
    }

    // Animate the nut being removed completely
    private IEnumerator AnimateNutRemoval()
    {
        // Move the nut outward faster and with some rotation
        Vector3 initialPosition = nutTransform.localPosition;
        Vector3 targetPosition = initialPosition + transform.TransformDirection(rotationAxis.normalized * looseningDistance * 3);
        float moveDuration = 0.5f;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;

            // Move outward
            nutTransform.localPosition = Vector3.Lerp(initialPosition, targetPosition, t);

            // Add some rotation for effect
            nutTransform.Rotate(rotationAxis, 720f * Time.deltaTime);

            yield return null;
        }

        // Disable or hide the nut
        if (nutModel != null)
            nutModel.SetActive(false);
        else
            gameObject.SetActive(false);

        currentAnimationCoroutine = null;
    }

    // When the wrench enters the nut's trigger area
    private void OnTriggerEnter(Collider other)
    {
        if (isRemoved)
            return;

        WrenchInteraction wrench = other.GetComponentInParent<WrenchInteraction>();
        if (wrench != null)
        {
            wrench.SetCurrentLugNut(this);

            // Highlight the nut
            if (nutRenderer != null && highlightMaterial != null && !isFullyLoosened)
            {
                nutRenderer.material = highlightMaterial;
            }
        }
    }

    // When the wrench exits the nut's trigger area
    private void OnTriggerExit(Collider other)
    {
        if (isRemoved)
            return;

        WrenchInteraction wrench = other.GetComponentInParent<WrenchInteraction>();
        if (wrench != null)
        {
            wrench.ClearCurrentLugNut();

            // Reset material unless loosened
            if (nutRenderer != null && !isFullyLoosened)
            {
                nutRenderer.material = standardMaterial;
            }
        }
    }

    // Helper to check if nut is removable
    public bool IsRemovable()
    {
        return isFullyLoosened && !isRemoved;
    }

    // Reset the nut to its initial state
    public void ResetNut()
    {
        if (currentAnimationCoroutine != null)
            StopCoroutine(currentAnimationCoroutine);

        nutTransform.localPosition = initialPosition;

        if (nutModel != null)
            nutModel.SetActive(true);
        else
            gameObject.SetActive(true);

        isFullyLoosened = false;
        isRemoved = false;

        // Reset material
        if (nutRenderer != null && standardMaterial != null)
        {
            nutRenderer.material = standardMaterial;
        }
    }

    public Transform GetSocketPoint()
    {
        return nutTransform;
    }
}