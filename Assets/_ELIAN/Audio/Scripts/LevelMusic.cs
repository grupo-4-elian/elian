using System.Collections;
using UnityEngine;

// Musica de fondo de un nivel: entra con un fundido, se repite y puede
// apagarse con otro fundido (p. ej. al vencer al jefe, para que se oiga
// la victoria). El menu de pausa ya la pausa con AudioListener.pause.
[RequireComponent(typeof(AudioSource))]
public class LevelMusic : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.45f;
    [SerializeField] private float fadeInTime = 2f;

    private static LevelMusic current;
    private AudioSource source;

    private void Awake()
    {
        current = this;
        source = GetComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
    }

    private void Start()
    {
        if (clip == null)
            return;
        source.Play();
        StartCoroutine(FadeTo(volume, fadeInTime));
    }

    private void OnDestroy()
    {
        if (current == this)
            current = null;
    }

    public static void FadeOut(float seconds = 1.5f)
    {
        if (current != null && current.isActiveAndEnabled)
        {
            current.StopAllCoroutines();
            current.StartCoroutine(current.FadeTo(0f, seconds));
        }
    }

    private IEnumerator FadeTo(float target, float seconds)
    {
        float start = source.volume;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(start, target, t / Mathf.Max(seconds, 0.01f));
            yield return null;
        }
        source.volume = target;
        if (target <= 0f)
            source.Stop();
    }
}
