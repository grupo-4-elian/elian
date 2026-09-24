using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Animator))]
public class PlayerHurt : MonoBehaviour
{
    [Header("Reaccion al daño")]
    [SerializeField] private float hurtLockDuration = 0.35f;

    [Header("Invencibilidad")]
    [Tooltip("Segundos sin recibir daño despues de un golpe.")]
    [SerializeField] private float invulnerabilityDuration = 1f;
    [Tooltip("Cada cuantos segundos parpadea el sprite durante la invencibilidad.")]
    [SerializeField] private float blinkInterval = 0.1f;

    private Health health;
    private Animator animator;
    private PlayerController playerController;
    private Rigidbody2D rb;
    private SpriteRenderer[] spriteRenderers;

    private void Awake()
    {
        health = GetComponent<Health>();
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        health.HealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        health.HealthChanged -= OnHealthChanged;

        // Si se desactiva a mitad del parpadeo (ej. al morir), dejamos
        // el sprite visible y la vida en estado normal.
        StopAllCoroutines();
        SetSpritesVisible(true);
        health.IsInvulnerable = false;
    }

    private void OnHealthChanged(int current, int max)
    {
        if (current <= 0)
            return;

        animator.SetTrigger("Hurt");
        Sfx.PlayHurtFallback(this, Sfx.GolpeJugador);

        StopAllCoroutines();
        StartCoroutine(HurtLock());
        StartCoroutine(Invulnerability());
    }

    private IEnumerator HurtLock()
    {
        if (playerController != null)
            playerController.enabled = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(hurtLockDuration);

        // Si hay un dialogo abierto, es el DialogueManager quien devuelve
        // el control al terminar; no lo reactivamos aca.
        if (!health.IsDead && !DialogueManager.IsDialogueActive && playerController != null)
            playerController.enabled = true;
    }

    private IEnumerator Invulnerability()
    {
        health.IsInvulnerable = true;

        float elapsed = 0f;
        bool visible = true;

        while (elapsed < invulnerabilityDuration)
        {
            visible = !visible;
            SetSpritesVisible(visible);

            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        SetSpritesVisible(true);
        health.IsInvulnerable = false;
    }

    private void SetSpritesVisible(bool visible)
    {
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            if (sr != null)
                sr.enabled = visible;
        }
    }
}
