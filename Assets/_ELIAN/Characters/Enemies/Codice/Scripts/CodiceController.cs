using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class CodiceController : MonoBehaviour
{
    [Header("Deteccion")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float shootRange = 5f;

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private bool isFlying = false;

    [Header("Disparo")]
    [SerializeField] private float shootCooldown = 1.5f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;

    private Rigidbody2D rb;
    private Animator animator;
    private Transform player;

    private bool facingRight = true;
    private float lastShootTime = -99f;

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
            player = p.transform;
    }

    private void Update()
    {
        if (player == null)
            return;

        // Distancia solo en X: a Codice no le importa la diferencia de
        // altura, para que se quede flotando arriba en vez de bajar
        // hasta la altura del jugador para "entrar en rango".
        float distance = Mathf.Abs(transform.position.x - player.position.x);

        if (distance <= shootRange)
        {
            animator.SetFloat("Speed", 0f);
            LookAtPlayer();

            if (Time.time >= lastShootTime + shootCooldown)
            {
                animator.SetTrigger("Attack");
                lastShootTime = Time.time;
                Shoot();
            }
        }
        else if (distance <= detectionRange)
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

        float distance = Mathf.Abs(transform.position.x - player.position.x);

        if (distance <= detectionRange && distance > shootRange)
        {
            // Solo se mueve en X, tanto volando como caminando: mantiene
            // su altura actual siempre, nunca persigue en Y.
            float dir = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(
                dir * moveSpeed,
                isFlying ? 0f : rb.linearVelocity.y
            );
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, isFlying ? 0f : rb.linearVelocity.y);
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null || player == null)
            return;

        // Apunta hacia donde esta el jugador en este instante (no
        // predictivo), calculando el angulo real en vez de usar
        // siempre la rotacion horizontal del FirePoint.
        Vector2 direction = ((Vector2)player.position - (Vector2)firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion bulletRotation = Quaternion.Euler(0f, 0f, angle);

        Instantiate(bulletPrefab, firePoint.position, bulletRotation);
    }

    private void LookAtPlayer()
    {
        bool playerIsRight = player.position.x > transform.position.x;

        if (playerIsRight != facingRight)
        {
            facingRight = playerIsRight;
            transform.Rotate(0f, 180f, 0f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootRange);
    }
}