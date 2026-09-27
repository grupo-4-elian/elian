using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Animator))]
public class SophiaDeath : MonoBehaviour
{
    [SerializeField] private float destroyDelay = 1f;
    [Tooltip("Segundos desde la muerte de Sophia hasta que aparece la pantalla de victoria.")]
    [SerializeField] private float victoryDelay = 1.5f;

    [Header("Progresion")]
    [Tooltip("Nombre que se muestra en la pantalla de victoria.")]
    [SerializeField] private string bossDisplayName = "Sophia";
    [Tooltip("Escena que se carga al continuar (debe estar en Build Settings). Vacio = ultimo nivel.")]
    [SerializeField] private string nextSceneName = "";

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
        // Sophia_AC todavia no tiene "Hurt" ni "Die": solo los usamos si existen.
        if (SophiaHurt.HasParameter(animator, "Hurt"))
            animator.ResetTrigger("Hurt");

        if (SophiaHurt.HasParameter(animator, "Die"))
            animator.SetTrigger("Die");

        Sfx.PlayDeathFallback(this, Sfx.ExplosionJefe);

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

        // Sophia es el boss final del nivel: mostramos la pantalla de victoria
        // un poco despues, cuando ya desaparecio.
        VictoryScreen.Show(victoryDelay, bossDisplayName, nextSceneName);

        Destroy(gameObject, destroyDelay);
    }
}