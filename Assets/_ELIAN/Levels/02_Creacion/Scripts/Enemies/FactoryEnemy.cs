using System.Collections;
using UnityEngine;

// Base de los enemigos del Nivel 2 (Ensamblador, Corrector, Dron).
//
// - Aparece a una distancia minima de Elian (nunca "en la cara").
// - Se pausa en dialogos, en el menu de pausa y con Elian muerto.
// - Parpadea al recibir daño y reproduce su animacion de muerte.
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody2D))]
public abstract class FactoryEnemy : MonoBehaviour
{
    [Header("Aparicion segura")]
    [Tooltip("Distancia minima a Elian al activarse. Si esta mas cerca, se corre.")]
    [SerializeField] private float minSpawnDistance = 8f;
    [Tooltip("Limites horizontales de la seccion (no aparece detras de una compuerta).")]
    [SerializeField] private float sectionMinX = -1000f;
    [SerializeField] private float sectionMaxX = 1000f;

    [Header("Feedback")]
    [SerializeField] private Color hurtColor = new Color(1f, 0.45f, 0.35f);

    protected Health health;
    protected Rigidbody2D rb;
    protected FrameAnimator anim;
    protected SpriteRenderer sr;
    protected CharacterAudio characterAudio;
    protected Transform player;
    protected Health playerHealth;

    protected bool spawning;
    protected bool dead;
    protected bool facingRight = true;

    protected virtual bool IsFlying => false;

    protected bool Paused =>
        dead || spawning ||
        DialogueManager.IsDialogueActive ||
        PauseMenu.IsPaused ||
        (playerHealth != null && playerHealth.IsDead);

    public bool IsDead => dead;

    public void SetSectionBounds(float minX, float maxX)
    {
        sectionMinX = minX;
        sectionMaxX = maxX;

        // Los fabricados por FABER reciben sus limites despues de OnEnable:
        // se vuelve a comprobar para no quedar detras de un muro.
        KeepSafeDistance();
    }

    protected virtual void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<FrameAnimator>();
        sr = GetComponent<SpriteRenderer>();
        characterAudio = GetComponent<CharacterAudio>();

        if (IsFlying)
            rb.gravityScale = 0f;
    }

    protected virtual void OnEnable()
    {
        health.HealthChanged += OnHealthChanged;
        health.Died += OnDied;

        FindPlayer();
        KeepSafeDistance();

        if (anim != null && anim.Has("spawn"))
            StartCoroutine(SpawnRoutine());
        else if (anim != null)
            anim.Play("idle", true);
    }

    protected virtual void OnDisable()
    {
        health.HealthChanged -= OnHealthChanged;
        health.Died -= OnDied;
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<Health>();
        }
    }

    // Si al activarse la oleada Elian esta demasiado cerca, el enemigo se
    // corre al lado con espacio dentro de su seccion.
    private void KeepSafeDistance()
    {
        if (player == null)
            return;

        float dx = transform.position.x - player.position.x;
        bool insideSection = transform.position.x >= sectionMinX + 1f && transform.position.x <= sectionMaxX - 1f;
        if (insideSection && Mathf.Abs(dx) >= minSpawnDistance)
            return;

        float side = dx >= 0f ? 1f : -1f;
        float x = player.position.x + side * minSpawnDistance;

        if (x < sectionMinX + 1f || x > sectionMaxX - 1f)
            x = player.position.x - side * minSpawnDistance;

        x = Mathf.Clamp(x, sectionMinX + 1f, sectionMaxX - 1f);
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
        if (rb != null)
            rb.position = new Vector2(x, rb.position.y);
    }

    private IEnumerator SpawnRoutine()
    {
        spawning = true;
        anim.Play("spawn", true);
        yield return new WaitForSeconds(anim.LengthOf("spawn"));
        spawning = false;
        if (!dead)
            anim.Play("idle", true);
    }

    protected void Face(float direction)
    {
        bool right = direction >= 0f;
        if (right == facingRight)
            return;

        facingRight = right;
        transform.Rotate(0f, 180f, 0f);
    }

    protected float DirectionToPlayer()
    {
        return player == null ? 0f : Mathf.Sign(player.position.x - transform.position.x);
    }

    protected float HorizontalDistanceToPlayer()
    {
        return player == null ? float.MaxValue : Mathf.Abs(player.position.x - transform.position.x);
    }

    protected void StopMoving()
    {
        rb.linearVelocity = IsFlying ? Vector2.zero : new Vector2(0f, rb.linearVelocity.y);
    }

    private void OnHealthChanged(int current, int max)
    {
        if (current <= 0 || dead)
            return;

        Sfx.PlayHurtFallback(this, Sfx.GolpeEnemigo);
        StartCoroutine(HurtFlash());
    }

    private IEnumerator HurtFlash()
    {
        if (sr == null)
            yield break;

        sr.color = hurtColor;
        yield return new WaitForSeconds(0.08f);
        sr.color = Color.white;
    }

    private void OnDied()
    {
        dead = true;
        StopAllCoroutines();
        if (sr != null)
            sr.color = Color.white;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        foreach (Collider2D c in GetComponents<Collider2D>())
            c.enabled = false;

        Sfx.PlayDeathFallback(this, Sfx.Explosion);

        float delay = 0.6f;
        if (anim != null && anim.Has("dead"))
        {
            anim.Play("dead", true);
            delay = anim.LengthOf("dead") + 0.2f;
        }

        Destroy(gameObject, delay);
    }

    protected void DamagePlayer(int amount)
    {
        if (playerHealth != null && !playerHealth.IsDead)
            playerHealth.TakeDamage(amount);
    }
}
