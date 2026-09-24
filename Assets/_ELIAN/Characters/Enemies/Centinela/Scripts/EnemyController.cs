using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class EnemyController : MonoBehaviour
{
    [Header("Deteccion")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private bool isFlying = false;

    [Header("Ataque")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField, Min(1)] private int attackDamage = 1;
    [SerializeField] private Vector2 attackCenterOffset = Vector2.zero;

    private Rigidbody2D rb;
    private Animator animator;
    private Transform player;
    private Health playerHealth;

    private bool facingRight = true;
    private float lastAttackTime = -99f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (isFlying)
            rb.gravityScale = 0f;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<Health>();
        }
    }

    // Durante un dialogo (o con Elian muerto) el enemigo se queda quieto
    // y no ataca: el jugador no puede moverse ni defenderse.
    private bool IsPaused =>
        DialogueManager.IsDialogueActive ||
        (playerHealth != null && playerHealth.IsDead);

    private void Update()
    {
        if (player == null)
            return;

        if (IsPaused)
        {
            animator.SetFloat("Speed", 0f);
            return;
        }

        float detectionDistance = Vector2.Distance(
            transform.position,
            player.position
        );

        float attackDistance = Vector2.Distance(
            GetAttackCenter(),
            player.position
        );

        if (attackDistance <= attackRange)
        {
            animator.SetFloat("Speed", 0f);
            LookAtPlayer();

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                animator.SetTrigger("Attack");
                lastAttackTime = Time.time;

                if (playerHealth != null && !playerHealth.IsDead)
                    playerHealth.TakeDamage(attackDamage);
            }
        }
        else if (detectionDistance <= detectionRange)
        {
            animator.SetFloat("Speed", 1f);
            LookAtPlayer();
        }
        else
        {
            animator.SetFloat("Speed", 0f);
        }
    }

    private void FixedUpdate()
    {
        if (player == null)
            return;

        float detectionDistance = Vector2.Distance(
            transform.position,
            player.position
        );

        float attackDistance = Vector2.Distance(
            GetAttackCenter(),
            player.position
        );

        if (!IsPaused &&
            detectionDistance <= detectionRange &&
            attackDistance > attackRange)
        {
            if (isFlying)
            {
                Vector2 direction =
                    ((Vector2)player.position - rb.position).normalized;

                rb.linearVelocity =
                    direction * moveSpeed;
            }
            else
            {
                float dir = Mathf.Sign(
                    player.position.x - transform.position.x
                );

                rb.linearVelocity = new Vector2(
                    dir * moveSpeed,
                    rb.linearVelocity.y
                );
            }
        }
        else
        {
            if (isFlying)
            {
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                rb.linearVelocity = new Vector2(
                    0f,
                    rb.linearVelocity.y
                );
            }
        }
    }

    private void LookAtPlayer()
    {
        bool playerIsRight =
            player.position.x > transform.position.x;

        if (playerIsRight != facingRight)
        {
            facingRight = playerIsRight;
            transform.Rotate(0f, 180f, 0f);
        }
    }

    private Vector2 GetAttackCenter()
    {
        return transform.TransformPoint(attackCenterOffset);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            GetAttackCenter(),
            attackRange
        );
    }
}