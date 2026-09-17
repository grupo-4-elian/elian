using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Animator))]
public class SophiaDeath : MonoBehaviour
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
        animator.ResetTrigger("Hurt");
        animator.SetTrigger("Die");

        SophiaHurt sh = GetComponent<SophiaHurt>();
        if (sh != null)
            sh.enabled = false;

        SophiaController sc = GetComponent<SophiaController>();
        if (sc != null)
            sc.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Destroy(gameObject, destroyDelay);
    }
}