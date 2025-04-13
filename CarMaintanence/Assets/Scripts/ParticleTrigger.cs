using UnityEngine;

public class ParticleTrigger : MonoBehaviour
{
    public new ParticleSystem particleSystem;
    public string filterTag = "Filter"; // Tag for triggering objects

    [Header("Material Transition")]
    public GameObject targetObject; // The object whose material will change
    public Material startMaterial; // Material with the initial texture
    public Material endMaterial; // Material with the target texture
    public float transitionDuration = 2f; // Time for the transition (in seconds)

    private Material currentMaterial; // Material instance to modify
    private float transitionProgress = 0f; // Progress of the transition (0 to 1)
    private bool isTransitioning = false;
    private Renderer objectRenderer;

    private Texture startTexture;
    private Texture endTexture;
    public AudioSource audioSource;

    void Start()
    {
        objectRenderer = targetObject.GetComponent<Renderer>();

        // Create a new material instance to avoid modifying the original assets
        currentMaterial = new Material(startMaterial);
        //objectRenderer.material = currentMaterial;

        // Get the textures from the materials
        startTexture = startMaterial.mainTexture;
        endTexture = endMaterial.mainTexture;

       // Debug.Log($"Start Material Texture: {startTexture.name}");
    }

    void Update()
    {
        if (isTransitioning)
        {
            //Debug.Log("Initiating Texture Lerp...");

            // Gradually increase the transition progress
            transitionProgress += Time.deltaTime / transitionDuration;
            transitionProgress = Mathf.Clamp01(transitionProgress);

            // Apply crossfade effect between textures
            objectRenderer.material.Lerp(objectRenderer.material, endMaterial, transitionProgress);

            // Optional: If Lerp doesn't work as expected, use this manual approach:
            /*if (transitionProgress < 1f)
            {
                // Blend between the start and end textures
                currentMaterial.SetTexture("_BaseMap", transitionProgress < 0.5f ? startTexture : endTexture);
            }
            else
            {
                // Ensure the final texture is set correctly
                currentMaterial.SetTexture("_BaseMap", endTexture);
                isTransitioning = false;
                
            }*/

            //objectRenderer.material = currentMaterial;

            
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(filterTag) && !particleSystem.isPlaying)
        {
            particleSystem.Play();
            audioSource.Play();
            isTransitioning = true;
            transitionProgress = 0f;

            //Debug.Log("Texture Transition Started");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(filterTag) && particleSystem.isPlaying)
        {
            particleSystem.Stop();
            audioSource.Stop();
            //Debug.Log("Particle System Stopped");
        }
    }
}
