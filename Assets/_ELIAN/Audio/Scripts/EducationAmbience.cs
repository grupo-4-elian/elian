using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class EducationAmbience : MonoBehaviour
{
    [Header("Ambiente")]
    [SerializeField] private AudioClip baseAmbience;
    [SerializeField] private AudioClip machineHum;
    [SerializeField] private AudioClip glitchClip;

    [Header("Volúmenes")]
    [SerializeField, Range(0f, 1f)]
    private float baseVolume = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float machineHumVolume = 0.3f;

    [SerializeField, Range(0f, 1f)]
    private float glitchVolume = 0.5f;

    [Header("Glitch aleatorio")]
    [SerializeField, Min(0.5f)]
    private float minGlitchInterval = 8f;

    [SerializeField, Min(0.5f)]
    private float maxGlitchInterval = 18f;

    private AudioSource baseSource;
    private AudioSource humSource;
    private AudioSource glitchSource;

    private void Awake()
    {
        baseSource = CreateSource(
            baseVolume,
            true
        );

        humSource = CreateSource(
            machineHumVolume,
            true
        );

        glitchSource = CreateSource(
            glitchVolume,
            false
        );
    }

    private void Start()
    {
        if (baseAmbience != null)
        {
            baseSource.clip = baseAmbience;
            baseSource.Play();
        }

        if (machineHum != null)
        {
            humSource.clip = machineHum;
            humSource.Play();
        }

        if (glitchClip != null)
            StartCoroutine(GlitchRoutine());
    }

    private AudioSource CreateSource(
        float volume,
        bool loop)
    {
        AudioSource source =
            gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume;

        return source;
    }

    private IEnumerator GlitchRoutine()
    {
        while (true)
        {
            float minimum =
                Mathf.Min(
                    minGlitchInterval,
                    maxGlitchInterval
                );

            float maximum =
                Mathf.Max(
                    minGlitchInterval,
                    maxGlitchInterval
                );

            yield return new WaitForSeconds(
                Random.Range(minimum, maximum)
            );

            if (glitchSource != null &&
                glitchClip != null &&
                !glitchSource.isPlaying)
            {
                glitchSource.PlayOneShot(
                    glitchClip
                );
            }
        }
    }
}
