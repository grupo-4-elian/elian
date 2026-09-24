using UnityEngine;

public class SophiaBulletPhase2 : MonoBehaviour
{
    [Header("Persecución")]
    [Tooltip("Tiempo aproximado que debería tardar la bala en alcanzar a Elian.")]
    [SerializeField] private float targetTravelTime = 1.2f;

    [SerializeField] private float minSpeed = 4f;
    [SerializeField] private float maxSpeed = 7f;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Daño a Elian")]
    [SerializeField, Min(1)]
    private int damageToPlayer = 1;

    [Header("Almacenamiento")]
    [SerializeField]
    private float storageMoveSpeed = 12f;

    [SerializeField]
    private float slotArrivalDistance = 0.05f;

    [Header("Retorno")]
    [SerializeField]
    private float returnSpeed = 12f;

    [SerializeField, Min(1)]
    private int damageToSophia = 3;

    [Header("General")]
    [SerializeField]
    private float lifeTime = 15f;

    private SophiaController sophia;
    private Transform player;

    private Collider2D sophiaCollider;
    private Collider2D bulletCollider;

    private bool stored = false;
    private bool returning = false;
    private bool chaseResolved = false;

    // Solo será true cuando la bala
    // haya llegado realmente al slot central.
    private bool readyToBeShot = false;

    private int reservedSlot = -1;
    private float currentSpeed = 6f;

    private void Awake()
    {
        bulletCollider =
            GetComponent<Collider2D>();
    }

    public void Initialize(
        SophiaController owner,
        Transform target
    )
    {
        sophia = owner;
        player = target;

        if (sophia != null)
        {
            sophiaCollider =
                sophia.GetComponent<Collider2D>();

            if (sophiaCollider == null)
            {
                sophiaCollider =
                    sophia.GetComponentInChildren<Collider2D>();
            }
        }

        CalculateSpeed();
    }

    private void Start()
    {
        Destroy(
            gameObject,
            lifeTime
        );
    }

    private void Update()
    {
        if (sophia == null)
            return;

        if (returning)
        {
            MoveTowardsSophia();
            return;
        }

        if (player == null)
            return;

        if (stored)
        {
            MoveToReservedSlot();
            return;
        }

        FollowPlayer();
    }

    private void CalculateSpeed()
    {
        if (sophia == null)
            return;

        float distance =
            Vector2.Distance(
                transform.position,
                sophia.GetPlayerAimPoint()
            );

        float calculatedSpeed =
            distance /
            Mathf.Max(
                targetTravelTime,
                0.1f
            );

        currentSpeed =
            Mathf.Clamp(
                calculatedSpeed,
                minSpeed,
                maxSpeed
            );
    }

    private void FollowPlayer()
    {
        Vector2 direction =
            (sophia.GetPlayerAimPoint() -
             (Vector2)transform.position)
            .normalized;

        RotateTowards(direction);

        transform.Translate(
            Vector2.right *
            currentSpeed *
            Time.deltaTime,
            Space.Self
        );
    }

    private void MoveToReservedSlot()
    {
        if (reservedSlot < 0)
            return;

        Vector2 targetPosition =
            sophia.GetErrorSlotPosition(
                reservedSlot
            );

        transform.position =
            Vector2.MoveTowards(
                transform.position,
                targetPosition,
                storageMoveSpeed *
                Time.deltaTime
            );

        float distanceToSlot =
            Vector2.Distance(
                transform.position,
                targetPosition
            );

        // Solo cuando llegó realmente al
        // slot central se puede disparar.
        if (reservedSlot == 0 &&
            distanceToSlot <= slotArrivalDistance)
        {
            readyToBeShot = true;

            if (bulletCollider != null)
                bulletCollider.enabled = true;
        }
        else
        {
            readyToBeShot = false;

            if (bulletCollider != null)
                bulletCollider.enabled = false;
        }
    }

    private void MoveTowardsSophia()
    {
        Vector2 targetPosition;

        if (sophiaCollider != null)
        {
            targetPosition =
                sophiaCollider.bounds.center;
        }
        else
        {
            targetPosition =
                (Vector2)sophia.transform.position +
                Vector2.up * 3.5f;
        }

        Vector2 direction =
            (targetPosition -
             (Vector2)transform.position)
            .normalized;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        transform.position =
            Vector2.MoveTowards(
                transform.position,
                targetPosition,
                returnSpeed *
                Time.deltaTime
            );
    }

    private void RotateTowards(
        Vector2 direction
    )
    {
        float targetAngle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        float angle =
            Mathf.MoveTowardsAngle(
                transform.eulerAngles.z,
                targetAngle,
                rotationSpeed *
                Time.deltaTime
            );

        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }

    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        // -------------------------
        // 1. PERSIGUIENDO A ELIAN
        // -------------------------

        if (!stored && !returning)
        {
            PlayerController playerController =
                other.GetComponentInParent<
                    PlayerController
                >();

            if (playerController != null)
            {
                Health playerHealth =
                    other.GetComponentInParent<
                        Health
                    >();

                // Elian acepta el error.
                if (playerController.IsAcceptingError)
                {
                    stored = true;
                    readyToBeShot = false;

                    // Al tocar a Elian dejamos
                    // temporalmente de colisionar.
                    // Así debe terminar de atravesarlo
                    // antes de poder recibir disparos.
                    if (bulletCollider != null)
                        bulletCollider.enabled = false;

                    if (sophia.TryStoreError(this))
                    {
                        NotifyChaseResolved();
                    }
                    else
                    {
                        stored = false;

                        NotifyChaseResolved();

                        Destroy(gameObject);
                    }

                    return;
                }

                // Si Elian intentó evitar el error
                // recibe daño y la bala desaparece.
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(
                        damageToPlayer
                    );
                }

                NotifyChaseResolved();

                Destroy(gameObject);

                return;
            }
        }

        // -------------------------
        // 2. REGRESANDO A SOPHIA
        // -------------------------

        if (returning)
        {
            SophiaController hitSophia =
                other.GetComponentInParent<
                    SophiaController
                >();

            if (hitSophia == sophia)
            {
                sophia.ReceiveReturnedError(
                    damageToSophia
                );

                Destroy(gameObject);
            }
        }
    }

    // Bullet.cs llama a este método.
    // Mientras persigue a Elian o se está
    // moviendo hacia un slot devuelve false,
    // por lo que el disparo simplemente la atraviesa.
    public bool TryReturnFromPlayerShot()
    {
        if (!stored)
            return false;

        if (returning)
            return false;

        if (reservedSlot != 0)
            return false;

        if (!readyToBeShot)
            return false;

        ReleaseStoredSlot();

        stored = false;
        readyToBeShot = false;
        returning = true;

        // Debe volver a tener collider
        // para impactar contra Sophia.
        if (bulletCollider != null)
            bulletCollider.enabled = true;

        return true;
    }

    // SophiaController llama a esto
    // al almacenar o reorganizar las balas.
    public void SetReservedSlot(
        int slotIndex
    )
    {
        reservedSlot = slotIndex;

        // Cada vez que cambia de slot,
        // debe llegar primero a su nueva posición.
        readyToBeShot = false;

        if (bulletCollider != null &&
            stored &&
            !returning)
        {
            bulletCollider.enabled = false;
        }
    }

    private void ReleaseStoredSlot()
    {
        if (sophia != null &&
            reservedSlot >= 0)
        {
            sophia.RemoveStoredError(
                this
            );
        }

        reservedSlot = -1;
    }

    private void NotifyChaseResolved()
    {
        if (chaseResolved)
            return;

        chaseResolved = true;

        if (sophia != null)
        {
            sophia.NotifyChasingBulletResolved();
        }
    }

    private void OnDestroy()
    {
        if (stored)
            ReleaseStoredSlot();

        NotifyChaseResolved();
    }
}