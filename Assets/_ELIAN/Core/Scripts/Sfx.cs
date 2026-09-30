using System.Collections.Generic;
using UnityEngine;

// Efectos de sonido del juego. Uso: Sfx.Play(Sfx.Salto);
//
// Los clips se cargan desde Assets/Resources/SFX/<nombre>.wav, asi que para
// cambiar un sonido basta con reemplazar el archivo manteniendo el nombre.
// No hace falta poner nada en las escenas: el reproductor se crea solo.
public static class Sfx
{
    public const string Salto = "salto";
    public const string Disparo = "disparo";
    public const string DisparoEnemigo = "disparo_enemigo";
    public const string GolpeJugador = "golpe_jugador";
    public const string GolpeEnemigo = "golpe_enemigo";
    public const string Explosion = "explosion";
    public const string ExplosionJefe = "explosion_jefe";
    public const string Muerte = "muerte";
    public const string GameOver = "game_over";
    public const string Victoria = "victoria";
    public const string Descarga = "descarga";
    public const string Menu = "menu";
    public const string MenuOk = "menu_ok";

    private const float MasterVolume = 0.8f;

    private static AudioSource source;
    private static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    public static void Play(string name, float volume = 1f)
    {
        AudioClip clip = GetClip(name);
        if (clip == null)
            return;

        EnsureSource();
        source.PlayOneShot(clip, volume * MasterVolume);
    }

    // Reproduce el efecto generico solo si el personaje no tiene su propio
    // sonido de daño en CharacterAudio (que ya suena solo al recibir daño).
    public static void PlayHurtFallback(Component character, string name, float volume = 1f)
    {
        CharacterAudio ca = character != null ? character.GetComponent<CharacterAudio>() : null;
        if (ca == null || !ca.HasHurtSound)
            Play(name, volume);
    }

    // Igual que PlayHurtFallback, para el sonido de muerte.
    public static void PlayDeathFallback(Component character, string name, float volume = 1f)
    {
        CharacterAudio ca = character != null ? character.GetComponent<CharacterAudio>() : null;
        if (ca == null || !ca.HasDeathSound)
            Play(name, volume);
    }

    private static AudioClip GetClip(string name)
    {
        if (clips.TryGetValue(name, out AudioClip clip))
            return clip;

        clip = Resources.Load<AudioClip>("SFX/" + name);
        if (clip == null)
            Debug.LogWarning($"[Sfx] No se encontro Resources/SFX/{name}");

        clips[name] = clip; // se cachea tambien el null para no avisar cada vez
        return clip;
    }

    private static void EnsureSource()
    {
        if (source != null)
            return;

        GameObject go = new GameObject("Sfx");
        Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D
        // Los sonidos de menu tienen que oirse aunque el juego este en pausa.
        source.ignoreListenerPause = true;
    }

    // Con "Enter Play Mode" sin recarga de dominio los estaticos sobreviven.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        source = null;
        clips.Clear();
    }
}
