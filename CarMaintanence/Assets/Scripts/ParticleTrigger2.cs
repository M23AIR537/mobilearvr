using UnityEngine;

public class ParticleTrigger2 : MonoBehaviour
{
    public new ParticleSystem particleSystem;
    public AudioSource emitter;

    public void OnSelect()
    {
        if (!particleSystem.isPlaying)
        {
            particleSystem.Play();
            emitter.Play();
        }
    }

    public void OnUnselect()
    {
        if (particleSystem.isPlaying)
        {
            particleSystem.Stop();
            emitter.Stop();
        }
    }
}
