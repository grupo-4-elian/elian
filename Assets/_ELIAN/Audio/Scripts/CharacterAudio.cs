using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CharacterAudio : MonoBehaviour
{
    [Header("Disparo")]
    [SerializeField] private AudioClip[] shootClips;

    [Header("Ataque")]
    [SerializeField] private AudioClip[] attackClips;

    [Header("Daño")]
    [SerializeField] private AudioClip[] hurtClips;

    [Header("Muerte")]
    [SerializeField] private AudioClip[] deathClips;

    [Header("Salto")]
    [SerializeField] private AudioClip[] jumpClips;

    [Header("Aterrizaje")]
    [SerializeField] private AudioClip[] landClips;

    [Header("Risa")]
    [SerializeField] private AudioClip[] laughClips;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
            audioSource.playOnAwake = false;
    }

    public void PlayShoot()
    {
        PlayRandom(shootClips);
    }

    public void PlayAttack()
    {
        PlayRandom(attackClips);
    }

    public void PlayHurt()
    {
        PlayRandom(hurtClips);
    }

    public void PlayDeath()
    {
        PlayRandom(deathClips);
    }

    public void PlayJump()
    {
        PlayRandom(jumpClips);
    }

    public void PlayLand()
    {
        PlayRandom(landClips);
    }

    public void PlayLaugh()
    {
        PlayRandom(laughClips);
    }

    private void PlayRandom(AudioClip[] clips)
    {
        if (audioSource == null ||
            clips == null ||
            clips.Length == 0)
            return;

        int index = Random.Range(0, clips.Length);

        AudioClip clip = clips[index];

        if (clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}
