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
    private Health health;

    private int previousHealth;
    private bool healthInitialized;
    private bool healthSubscribed;

    private float laughPlayingUntil;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        health = GetComponent<Health>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void Start()
    {
        if (health == null)
            return;

        previousHealth = health.CurrentHealth;
        healthInitialized = true;

        SubscribeHealth();
    }

    private void OnEnable()
    {
        if (healthInitialized)
            SubscribeHealth();
    }

    private void OnDisable()
    {
        UnsubscribeHealth();
    }

    private void SubscribeHealth()
    {
        if (health == null || healthSubscribed)
            return;

        health.HealthChanged += OnHealthChanged;
        health.Died += OnDied;

        healthSubscribed = true;
    }

    private void UnsubscribeHealth()
    {
        if (health == null || !healthSubscribed)
            return;

        health.HealthChanged -= OnHealthChanged;
        health.Died -= OnDied;

        healthSubscribed = false;
    }

    private void OnHealthChanged(int current, int max)
    {
        bool tookDamage = current < previousHealth;

        previousHealth = current;

        // Si este golpe lo mató, no reproduce Hurt.
        // La muerte se reproduce mediante Died.
        if (tookDamage && current > 0)
            PlayHurt();
    }

    private void OnDied()
    {
        PlayDeath();
    }

    // Permiten saber si este personaje tiene sonido propio para una accion,
    // para no superponerlo con los efectos genericos de Sfx.
    public bool HasShootSound => HasAny(shootClips);
    public bool HasHurtSound => HasAny(hurtClips);
    public bool HasDeathSound => HasAny(deathClips);
    public bool HasJumpSound => HasAny(jumpClips);

    private static bool HasAny(AudioClip[] clips)
    {
        if (clips == null)
            return false;

        foreach (AudioClip clip in clips)
        {
            if (clip != null)
                return true;
        }

        return false;
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
        if (Time.time < laughPlayingUntil)
            return;

        AudioClip clip = GetRandomClip(laughClips);

        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip);
        laughPlayingUntil = Time.time + clip.length;
    }

    private void PlayRandom(AudioClip[] clips)
    {
        AudioClip clip = GetRandomClip(clips);

        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;

        int startIndex = Random.Range(0, clips.Length);

        for (int i = 0; i < clips.Length; i++)
        {
            int index = (startIndex + i) % clips.Length;

            if (clips[index] != null)
                return clips[index];
        }

        return null;
    }
}
