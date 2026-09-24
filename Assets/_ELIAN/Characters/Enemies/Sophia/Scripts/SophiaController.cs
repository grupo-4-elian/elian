using UnityEngine;

public class SophiaController : MonoBehaviour
{
    [Header("Disparo")]
    [SerializeField] private float shootCooldown = 1.5f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;

    [Header("Fase 2")]
    [Tooltip("Prefab de bala que se usa a partir de Fase 2 (SophiaBullet_Rojo).")]
    [SerializeField] private GameObject bulletPrefabPhase2;
    [Tooltip("Porcentaje de vida (0-1) al que se activa la Fase 2. 0.5 = 50%.")]
    [SerializeField, Range(0f, 1f)] private float phase2HealthThreshold = 0.5f;
    [Tooltip("Nombre del Trigger en el Animator Controller (Sophia_AC) que hace la transición visual a Fase 2.")]
    [SerializeField] private string phase2AnimatorTrigger = "EnterPhase2";

    [Header("Ataque de Columna")]
    [Tooltip("Prefab de la columna dorada (Fase 1).")]
    [SerializeField] private GameObject columnPrefabPhase1;
    [Tooltip("Prefab de la columna roja (Fase 2).")]
    [SerializeField] private GameObject columnPrefabPhase2;
    [SerializeField] private float columnCooldown = 4f;
    [Tooltip("Sube o baja dónde aparece la columna respecto al piso detectado. Positivo = más arriba.")]
    [SerializeField] private float columnSpawnYOffset = 1f;
    [Tooltip("Layer del suelo, para encontrar la altura correcta donde aparece la columna.")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRaycastDistance = 20f;

    private Animator animator;
    private Health health;
    private bool phase2Active = false;

    private Transform player;
    private Health playerHealth;
    private bool facingRight = true;
    private bool battleActive = false;
    private float lastShootTime = -99f;
    private float lastColumnTime = -99f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();

        if (health == null)
            Debug.LogWarning("[SophiaController] Falta el componente Health: la Fase 2 nunca se va a activar.");
        else
            health.IsInvulnerable = true; // No se la puede dañar hasta que arranque la pelea.
    }

    private void OnEnable()
    {
        if (health != null)
            health.HealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.HealthChanged -= HandleHealthChanged;
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

    private void Update()
    {
        if (player == null)
            return;

        // Siempre mira al jugador, incluso antes de que arranque la pelea
        // (por ejemplo mientras transcurre el diálogo previo).
        LookAtPlayer();

        if (!battleActive || DialogueManager.IsDialogueActive)
            return;

        // Con Elian muerto deja de atacar mientras se reinicia el nivel.
        if (playerHealth != null && playerHealth.IsDead)
            return;

        if (Time.time >= lastShootTime + shootCooldown)
        {
            lastShootTime = Time.time;
            Shoot();
        }

        if (Time.time >= lastColumnTime + columnCooldown)
        {
            lastColumnTime = Time.time;
            SpawnColumnAttack();
        }
    }

    // Llamar esto cuando termine el dialogo y arranque la pelea de
    // verdad (desde el script que maneje esa secuencia).
    public void StartBattle()
    {
        battleActive = true;

        if (health != null)
            health.IsInvulnerable = false;
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (phase2Active)
            return;

        int threshold = Mathf.CeilToInt(max * phase2HealthThreshold);

        if (current <= threshold)
            EnterPhase2();
    }

    private void EnterPhase2()
    {
        phase2Active = true;

        if (bulletPrefabPhase2 != null)
            bulletPrefab = bulletPrefabPhase2;
        else
            Debug.LogWarning("[SophiaController] Entró a Fase 2 pero bulletPrefabPhase2 no está asignado en el Inspector.");

        if (animator != null && !string.IsNullOrEmpty(phase2AnimatorTrigger))
            animator.SetTrigger(phase2AnimatorTrigger);
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null || player == null)
            return;

        Vector2 direction = ((Vector2)player.position - (Vector2)firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion bulletRotation = Quaternion.Euler(0f, 0f, angle);

        Instantiate(bulletPrefab, firePoint.position, bulletRotation);
        Sfx.Play(Sfx.DisparoEnemigo, 0.8f);
    }

    private void SpawnColumnAttack()
    {
        GameObject prefab = phase2Active ? columnPrefabPhase2 : columnPrefabPhase1;

        if (prefab == null || player == null)
        {
            Debug.LogWarning("[SophiaController] No se lanzó el ataque de columna: prefab o player nulo.");
            return;
        }

        Vector3 spawnPosition = GetGroundPositionBelow(player.position);
        spawnPosition.y += columnSpawnYOffset;

        Instantiate(prefab, spawnPosition, Quaternion.identity);
    }

    private Vector3 GetGroundPositionBelow(Vector3 origin)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundRaycastDistance, groundLayer);

        if (hit.collider != null)
            return new Vector3(origin.x, hit.point.y, 0f);

        // Si no encontramos suelo debajo (por ejemplo, LayerMask mal configurado),
        // usamos la misma altura del jugador para no romper el ataque.
        return new Vector3(origin.x, origin.y, 0f);
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
}