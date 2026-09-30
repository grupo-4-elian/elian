using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Animator))]
public class CentinelaDeath : MonoBehaviour
{
    [SerializeField] private float destroyDelay = 1f;

    private Health health;
    private Animator animator;

    private void Awake()
    {
        health = GetComponent<Health>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        health.Died += OnDeath;
    }

    private void OnDisable()
    {
        health.Died -= OnDeath;
    }

    private void OnDeath()
    {
        // Limpia un Hurt pendiente para que no compita con la muerte
        animator.ResetTrigger("Hurt");
        animator.SetTrigger("Die");
        Sfx.PlayDeathFallback(this, Sfx.Explosion);

        // Impide que un dano posterior interrumpa la animacion
        CentinelaHurt ch = GetComponent<CentinelaHurt>();
        if (ch != null) ch.enabled = false;

        EnemyController ec = GetComponent<EnemyController>();
        if (ec != null) ec.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Destroy(gameObject, destroyDelay);
    }
}