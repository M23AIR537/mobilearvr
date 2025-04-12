using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TireRemovalManager : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Transform tireTransform;
    [SerializeField] private List<LugNut> lugNuts = new List<LugNut>();
    [SerializeField] private WrenchInteraction wrenchInteraction;

    [Header("Tire Removal Settings")]
    [SerializeField] private float minLooseNutsToRemoveTire = 3; // How many nuts need to be loose before tire can come off
    [SerializeField] private float tireRemovalDistance = 0.3f; // How far the tire moves out when removed
    [SerializeField] private float tireRemovalSpeed = 0.5f;
    [SerializeField] private bool autoRemoveTireWhenReady = false;

    [Header("Effects")]
    [SerializeField] private AudioClip tireRemovalSound;
    [SerializeField] private ParticleSystem removalEffect;

    [Header("Events")]
    public UnityEvent onAllNutsLoosened;
    public UnityEvent onTireRemoved;

    // Internal state
    private Vector3 initialTirePosition;
    private bool isTireRemoved = false;
    private int loosedNutsCount = 0;
    private bool isTireRemovable = false;
    private AudioSource audioSource;
    private Coroutine tireAnimationCoroutine;

    private void Awake()
    {
        if (tireTransform == null)
            tireTransform = transform;

        initialTirePosition = tireTransform.localPosition;

        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && tireRemovalSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D sound
            audioSource.playOnAwake = false;
        }

        // Connect to wrench events
        if (wrenchInteraction != null)
        {
            wrenchInteraction.onNutFullyLoosened.AddListener(CheckLugNutsStatus);
        }
    }

    private void Start()
    {
        // Connect to individual lug nut events
        foreach (var nut in lugNuts)
        {
            nut.SetFullyLoosened(false);
        }
    }

    // Check if enough lug nuts are loose to remove the tire
    private void CheckLugNutsStatus()
    {
        // Count loosened nuts
        loosedNutsCount = 0;
        foreach (var nut in lugNuts)
        {
            if (nut.IsRemovable())
            {
                loosedNutsCount++;
            }
        }

        // Check if we've reached the threshold
        bool wasRemovable = isTireRemovable;
        isTireRemovable = loosedNutsCount >= minLooseNutsToRemoveTire;

        // If we just reached the threshold
        if (isTireRemovable && !wasRemovable)
        {
            Debug.Log($"Tire is now removable! {loosedNutsCount}/{lugNuts.Count} nuts loosened");
            onAllNutsLoosened.Invoke();

            // Auto-remove tire if that option is enabled
            if (autoRemoveTireWhenReady)
            {
                RemoveTire();
            }
        }
    }

    // Called to remove the tire once enough lug nuts are loosened
    public void RemoveTire()
    {
        if (isTireRemoved || !isTireRemovable)
            return;

        isTireRemoved = true;

        // Start tire removal animation
        if (tireAnimationCoroutine != null)
            StopCoroutine(tireAnimationCoroutine);

        tireAnimationCoroutine = StartCoroutine(AnimateTireRemoval());

        // Play removal sound
        if (audioSource != null && tireRemovalSound != null)
        {
            audioSource.clip = tireRemovalSound;
            audioSource.Play();
        }

        // Play particle effect if available
        if (removalEffect != null)
        {
            removalEffect.Play();
        }

        // Trigger event
        onTireRemoved.Invoke();

        Debug.Log("Tire removed!");
    }

    // Animate the tire coming off
    private IEnumerator AnimateTireRemoval()
    {
        Vector3 targetPosition = initialTirePosition + tireTransform.TransformDirection(Vector3.right * tireRemovalDistance);
        float elapsed = 0f;
        float duration = 1f / tireRemovalSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Add slight easing to the movement
            float easedT = Mathf.SmoothStep(0, 1, t);

            tireTransform.localPosition = Vector3.Lerp(initialTirePosition, targetPosition, easedT);

            // Add slight rotation as it comes off
            tireTransform.Rotate(Vector3.right, 15f * Time.deltaTime);

            yield return null;
        }

        tireAnimationCoroutine = null;
    }

    // Reset everything back to initial state
    public void ResetTire()
    {
        if (tireAnimationCoroutine != null)
            StopCoroutine(tireAnimationCoroutine);

        // Reset tire position
        tireTransform.localPosition = initialTirePosition;
        tireTransform.localRotation = Quaternion.identity;

        // Reset all lug nuts
        foreach (var nut in lugNuts)
        {
            nut.ResetNut();
        }

        // Reset state
        isTireRemoved = false;
        isTireRemovable = false;
        loosedNutsCount = 0;

        Debug.Log("Tire reset to initial state");
    }

    // Helper to check if specific number of nuts are required
    public int GetRemainingNutsNeeded()
    {
        int required = Mathf.CeilToInt(minLooseNutsToRemoveTire);
        return Mathf.Max(0, required - loosedNutsCount);
    }

    // Get a formatted string of progress
    public string GetProgressText()
    {
        return $"Lug nuts loosened: {loosedNutsCount}/{lugNuts.Count}";
    }

    // Public property to check if tire is removable
    public bool IsTireRemovable()
    {
        return isTireRemovable && !isTireRemoved;
    }
}