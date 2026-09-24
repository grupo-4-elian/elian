using System.Collections;
using UnityEngine;

public class SophiaController : MonoBehaviour
{
    [Header("Disparo")]
    [SerializeField] private float shootCooldown = 1.5f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;

    [Header("Ritmo del combate")]
    [SerializeField] private float initialAttackDelay = 0.8f;
    [SerializeField] private float postBulletPause = 0.7f;
    [SerializeField] private float columnAttackLockDuration = 1.5f;

    [Header("Mecánica - Recibir el error")]
    [SerializeField] private float vulnerabilityDuration = 3f;

    [Header("Fase 2")]
    [SerializeField] private GameObject bulletPrefabPhase2;

    [SerializeField, Range(0f, 1f)]
    private float phase2HealthThreshold = 0.5f;

    [SerializeField]
    private string phase2AnimatorTrigger = "EnterPhase2";

    [Header("Ataque de Columna")]
    [SerializeField] private GameObject columnPrefabPhase1;
    [SerializeField] private GameObject columnPrefabPhase2;
    [SerializeField] private float columnCooldown = 4f;
    [SerializeField] private float columnSpawnYOffset = 1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRaycastDistance = 20f;

    [Header("Escudo Visual")]
    [SerializeField] private GameObject shieldVisual;

    [Tooltip("Duración del destello blanco al recibir una bala devuelta.")]
    [SerializeField] private float shieldWhiteFlashDuration = 0.08f;

    [Tooltip("Duración del momento casi transparente después del destello.")]
    [SerializeField] private float shieldFadeDuration = 0.10f;

    [Tooltip("Aumento breve del tamaño del escudo durante el impacto.")]
    [SerializeField] private float shieldImpactScaleMultiplier = 1.10f;

    [Tooltip("Alpha del escudo durante el momento de interferencia.")]
    [SerializeField, Range(0f, 1f)] private float shieldFadeAlpha = 0.04f;

    private Animator animator;
    private Health health;
    private CharacterAudio characterAudio;

    private Transform player;
    private Collider2D playerCollider;
    private Health playerHealth;

    private SpriteRenderer shieldRenderer;
    private Color shieldBaseColor;
    private Vector3 shieldBaseScale;

    private bool phase2Active = false;
    private bool facingRight = true;
    private bool battleActive = false;
    private bool shieldActive = false;

    private bool chasingBulletActive = false;

    private float lastShootTime = -99f;
    private float lastColumnTime = -99f;
    private float attackLockedUntil = 0f;

    private Coroutine vulnerabilityCoroutine;
    private Coroutine shieldImpactCoroutine;

    // Fase 2:
    // 0 = error activo detrás de Elian
    // 1 = espera superior
    // 2 = espera inferior
    private readonly SophiaBulletPhase2[] storedErrors =
        new SophiaBulletPhase2[3];

    public bool IsPhase2Active => phase2Active;
    public bool IsBattleActive => battleActive;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
        characterAudio = GetComponent<CharacterAudio>();

        // Sophia permanece protegida hasta que comience su combate.
        if (health != null)
            health.SetInvulnerable(true);

        if (shieldVisual == null)
        {
            Transform child = transform.Find("ShieldVisual");

            if (child != null)
                shieldVisual = child.gameObject;
        }

        CacheShieldVisual();
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
        GameObject p =
            GameObject.FindGameObjectWithTag("Player");

        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<Health>();

            playerCollider =
                p.GetComponent<Collider2D>();

            if (playerCollider == null)
            {
                playerCollider =
                    p.GetComponentInChildren<Collider2D>();
            }
        }

        SetShieldVisual(false);
    }

    private void Update()
    {
        if (player == null)
            return;

        LookAtPlayer();

        if (!battleActive)
            return;

        // No ataca durante un dialogo ni con Elian muerto (mientras se
        // reinicia el nivel).
        if (DialogueManager.IsDialogueActive ||
            (playerHealth != null && playerHealth.IsDead))
            return;

        // No mezcla una bala perseguidora
        // con otro ataque de Sophia.
        if (chasingBulletActive)
            return;

        if (Time.time < attackLockedUntil)
            return;

        bool columnReady =
            Time.time >=
            lastColumnTime + columnCooldown;

        bool bulletReady =
            Time.time >=
            lastShootTime + shootCooldown;

        if (columnReady)
        {
            lastColumnTime = Time.time;

            SpawnColumnAttack();

            attackLockedUntil =
                Time.time +
                columnAttackLockDuration;

            return;
        }

        if (bulletReady)
        {
            if (!phase2Active ||
                !AreErrorSlotsFull())
            {
                lastShootTime = Time.time;

                Shoot();
            }
        }
    }

    public void StartBattle()
    {
        if (battleActive)
            return;

        battleActive = true;
        shieldActive = true;
        chasingBulletActive = false;

        attackLockedUntil =
            Time.time + initialAttackDelay;

        lastColumnTime = Time.time;

        if (health != null)
            health.SetInvulnerable(true);

        SetShieldVisual(true);
    }

    public Vector2 GetPlayerAimPoint()
    {
        if (playerCollider != null)
            return playerCollider.bounds.center;

        if (player != null)
        {
            return
                (Vector2)player.position +
                Vector2.up * 1.6f;
        }

        return transform.position;
    }

    public void NotifyChasingBulletResolved()
    {
        if (!chasingBulletActive)
            return;

        chasingBulletActive = false;

        attackLockedUntil =
            Time.time + postBulletPause;
    }

    // -------------------------
    // FASE 1
    // -------------------------

    public void ReceiveError()
    {
        if (!battleActive ||
            phase2Active ||
            !shieldActive)
            return;

        shieldActive = false;

        if (health != null)
            health.SetInvulnerable(false);

        SetShieldVisual(false);

        if (vulnerabilityCoroutine != null)
        {
            StopCoroutine(
                vulnerabilityCoroutine
            );
        }

        vulnerabilityCoroutine =
            StartCoroutine(
                VulnerabilityWindow()
            );
    }

    private IEnumerator VulnerabilityWindow()
    {
        yield return new WaitForSeconds(
            vulnerabilityDuration
        );

        if (!phase2Active)
        {
            shieldActive = true;

            if (health != null)
                health.SetInvulnerable(true);

            SetShieldVisual(true);
        }

        vulnerabilityCoroutine = null;
    }

    // -------------------------
    // FASE 2
    // -------------------------

    public void ReceiveReturnedError(int damage)
    {
        if (!battleActive ||
            !phase2Active ||
            health == null)
            return;

        // El escudo permanece activo.
        // La bala devuelta provoca el pulso visual.
        PulseShieldImpact();

        // Solo la bala devuelta puede atravesar
        // temporalmente la invulnerabilidad.
        health.SetInvulnerable(false);

        health.TakeDamage(damage);

        if (!health.IsDead)
            health.SetInvulnerable(true);

        // IMPORTANTE:
        // No llamar SetShieldVisual(true) aquí,
        // porque restauraría el escudo inmediatamente
        // y haría invisible el pulso.
    }

    public bool TryStoreError(
        SophiaBulletPhase2 bullet
    )
    {
        if (bullet == null)
            return false;

        for (int i = 0;
             i < storedErrors.Length;
             i++)
        {
            if (storedErrors[i] == null)
            {
                storedErrors[i] = bullet;

                bullet.SetReservedSlot(i);

                return true;
            }
        }

        return false;
    }

    public void RemoveStoredError(
        SophiaBulletPhase2 bullet
    )
    {
        if (bullet == null)
            return;

        for (int i = 0;
             i < storedErrors.Length;
             i++)
        {
            if (storedErrors[i] == bullet)
            {
                storedErrors[i] = null;
                break;
            }
        }

        CompactStoredErrors();
    }

    private void CompactStoredErrors()
    {
        int destination = 0;

        for (int source = 0;
             source < storedErrors.Length;
             source++)
        {
            SophiaBulletPhase2 bullet =
                storedErrors[source];

            if (bullet == null)
                continue;

            if (source != destination)
            {
                storedErrors[destination] =
                    bullet;

                storedErrors[source] = null;
            }

            // Al cambiar de posición, la bala
            // se moverá automáticamente al nuevo slot.
            bullet.SetReservedSlot(
                destination
            );

            destination++;
        }

        for (int i = destination;
             i < storedErrors.Length;
             i++)
        {
            storedErrors[i] = null;
        }
    }

    public Vector2 GetErrorSlotPosition(
        int slotIndex
    )
    {
        if (player == null)
            return transform.position;

        // Siempre del lado contrario a Sophia.
        float behindDirection =
            player.position.x >=
            transform.position.x
                ? 1f
                : -1f;

        Vector2 offset;

        switch (slotIndex)
        {
            // ERROR ACTIVO:
            // detrás de Elian a la altura
            // de su disparo normal.
            case 0:
                offset = new Vector2(
                    2.6f * behindDirection,
                    2.0f
                );
                break;

            // Espera superior.
            case 1:
                offset = new Vector2(
                    3.1f * behindDirection,
                    3.7f
                );
                break;

            // Espera inferior.
            case 2:
                offset = new Vector2(
                    3.1f * behindDirection,
                    0.8f
                );
                break;

            default:
                offset = new Vector2(
                    2.6f * behindDirection,
                    2.0f
                );
                break;
        }

        return
            (Vector2)player.position +
            offset;
    }

    private bool AreErrorSlotsFull()
    {
        for (int i = 0;
             i < storedErrors.Length;
             i++)
        {
            if (storedErrors[i] == null)
                return false;
        }

        return true;
    }

    // -------------------------
    // TRANSICIÓN FASE 2
    // -------------------------

    private void HandleHealthChanged(
        int current,
        int max
    )
    {
        if (!battleActive || phase2Active)
            return;

        int threshold =
            Mathf.CeilToInt(
                max *
                phase2HealthThreshold
            );

        if (current <= threshold)
            EnterPhase2();
    }

    private void EnterPhase2()
    {
        phase2Active = true;

        if (vulnerabilityCoroutine != null)
        {
            StopCoroutine(
                vulnerabilityCoroutine
            );

            vulnerabilityCoroutine = null;
        }

        shieldActive = true;

        if (health != null)
            health.SetInvulnerable(true);

        SetShieldVisual(true);

        if (bulletPrefabPhase2 != null)
        {
            bulletPrefab =
                bulletPrefabPhase2;
        }
        else
        {
            Debug.LogWarning(
                "[SophiaController] bulletPrefabPhase2 no está asignado."
            );
        }

        if (animator != null &&
            !string.IsNullOrEmpty(
                phase2AnimatorTrigger
            ))
        {
            animator.SetTrigger(
                phase2AnimatorTrigger
            );
        }
    }

    // -------------------------
    // ATAQUES
    // -------------------------

    private void Shoot()
    {
        if (bulletPrefab == null ||
            firePoint == null ||
            player == null)
            return;

        Vector2 targetPosition =
            GetPlayerAimPoint();

        Vector2 direction =
            (targetPosition -
             (Vector2)firePoint.position)
            .normalized;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        Quaternion rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        GameObject bullet =
            Instantiate(
                bulletPrefab,
                firePoint.position,
                rotation
            );

        if (characterAudio != null && characterAudio.HasShootSound)
            characterAudio.PlayShoot();
        else
            Sfx.Play(Sfx.DisparoEnemigo, 0.8f);

        SophiaBullet phase1Bullet =
            bullet.GetComponent<SophiaBullet>();

        if (phase1Bullet != null)
        {
            chasingBulletActive = true;

            phase1Bullet.Initialize(
                this,
                player
            );

            return;
        }

        SophiaBulletPhase2 phase2Bullet =
            bullet.GetComponent<
                SophiaBulletPhase2
            >();

        if (phase2Bullet != null)
        {
            chasingBulletActive = true;

            phase2Bullet.Initialize(
                this,
                player
            );
        }
    }

    private void SpawnColumnAttack()
    {
        GameObject prefab =
            phase2Active
                ? columnPrefabPhase2
                : columnPrefabPhase1;

        if (prefab == null ||
            player == null)
            return;

        Vector3 spawnPosition =
            GetGroundPositionBelow(
                player.position
            );

        spawnPosition.y +=
            columnSpawnYOffset;

        Instantiate(
            prefab,
            spawnPosition,
            Quaternion.identity
        );
    }

    private Vector3 GetGroundPositionBelow(
        Vector3 origin
    )
    {
        RaycastHit2D hit =
            Physics2D.Raycast(
                origin,
                Vector2.down,
                groundRaycastDistance,
                groundLayer
            );

        if (hit.collider != null)
        {
            return new Vector3(
                origin.x,
                hit.point.y,
                0f
            );
        }

        return new Vector3(
            origin.x,
            origin.y,
            0f
        );
    }

    private void LookAtPlayer()
    {
        bool playerIsRight =
            player.position.x >
            transform.position.x;

        if (playerIsRight != facingRight)
        {
            facingRight =
                playerIsRight;

            transform.Rotate(
                0f,
                180f,
                0f
            );
        }
    }

    // -------------------------
    // ESCUDO VISUAL
    // -------------------------

    private void CacheShieldVisual()
    {
        if (shieldVisual == null)
            return;

        shieldRenderer =
            shieldVisual.GetComponent<
                SpriteRenderer
            >();

        if (shieldRenderer != null)
        {
            shieldBaseColor =
                shieldRenderer.color;
        }

        shieldBaseScale =
            shieldVisual.transform.localScale;
    }

    private void SetShieldVisual(bool active)
    {
        if (shieldVisual == null)
            return;

        if (!active &&
            shieldImpactCoroutine != null)
        {
            StopCoroutine(
                shieldImpactCoroutine
            );

            shieldImpactCoroutine = null;
        }

        RestoreShieldAppearance();

        shieldVisual.SetActive(active);
    }

    private void PulseShieldImpact()
    {
        if (shieldVisual == null)
            return;

        shieldVisual.SetActive(true);

        if (shieldImpactCoroutine != null)
        {
            StopCoroutine(
                shieldImpactCoroutine
            );

            RestoreShieldAppearance();
        }

        shieldImpactCoroutine =
            StartCoroutine(
                ShieldImpactPulse()
            );
    }

    private IEnumerator ShieldImpactPulse()
    {
        // 1. DESTELLO BLANCO:
        // comunica el impacto de la bala devuelta contra el escudo.
        if (shieldRenderer != null)
        {
            Color whiteFlash = Color.white;
            whiteFlash.a = 0.85f;

            shieldRenderer.color =
                whiteFlash;
        }

        shieldVisual.transform.localScale =
            shieldBaseScale *
            shieldImpactScaleMultiplier;

        yield return new WaitForSeconds(
            shieldWhiteFlashDuration
        );

        // 2. INTERFERENCIA:
        // el escudo vuelve a su cyan original,
        // pero queda casi transparente por un instante.
        if (shieldRenderer != null)
        {
            Color fadedColor =
                shieldBaseColor;

            fadedColor.a =
                shieldFadeAlpha;

            shieldRenderer.color =
                fadedColor;
        }

        shieldVisual.transform.localScale =
            shieldBaseScale;

        yield return new WaitForSeconds(
            shieldFadeDuration
        );

        // 3. RECUPERACIÓN:
        // el escudo vuelve a su apariencia normal.
        RestoreShieldAppearance();

        shieldImpactCoroutine = null;
    }

    private void RestoreShieldAppearance()
    {
        if (shieldVisual == null)
            return;

        shieldVisual.transform.localScale =
            shieldBaseScale;

        if (shieldRenderer != null)
        {
            shieldRenderer.color =
                shieldBaseColor;
        }
    }
}