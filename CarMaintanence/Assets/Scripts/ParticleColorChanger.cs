using UnityEngine;

public class ParticleColorChanger : MonoBehaviour
{
    [Header("Particle System")]
    public new ParticleSystem particleSystem; // The particle system to detect collisions
    public string filterTag = "Filter";
    [Header("Target Object")]
    public GameObject targetObject; // The object to change texture
    public Material startmaterial; // The material whose texture will change
    public Material endMaterial; // Material with the target texture
    private Texture startTexture; // The initial texture
    private Texture endTexture; // The target texture
    private Material currentMaterial; // Material instance to modify
    public float transitionDuration = 2f; // Duration of the texture transition

    private Renderer objectRenderer;
    private float transitionProgress = 0f;
    private bool isTransitioning = false;

    private void Start()
    {
        objectRenderer = targetObject.GetComponent<Renderer>();
        currentMaterial = new Material(startmaterial);
        //objectRenderer.material = currentMaterial;
        //currentMaterial.SetTexture("_BaseMap", startTexture); // Set the initial texture
        startTexture = startmaterial.mainTexture;
        endTexture = endMaterial.mainTexture;

        // Enable particle collision callback
        //var collision = particleSystem.collision;
        //collision.enabled = true;
        //collision.type = ParticleSystemCollisionType.World;
        //collision.sendCollisionMessages = true;

        Debug.Log($"Initial Texture: {startmaterial.mainTexture.name}");
    }

    private void Update()
    {
        if (isTransitioning)
        {
            // Gradually increase the transition progress
            transitionProgress += Time.deltaTime / transitionDuration;
            transitionProgress = Mathf.Clamp01(transitionProgress);

            objectRenderer.material.Lerp(objectRenderer.material, endMaterial, transitionProgress);

            // Smoothly transition the material texture by blending the alpha
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

            }
            objectRenderer.material = currentMaterial;*/
        }
        
    }

    private void OnParticleCollision(GameObject other)
    {
        Debug.Log($"Particle collision detected:{other.tag}");
        if (other.CompareTag(filterTag)&& currentMaterial.mainTexture!= endTexture)
        {
            Debug.Log("Particle collision with Filter detected");
            isTransitioning = true;
            //transitionProgress = 0f;
        }

    }
}
