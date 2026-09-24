using UnityEngine;

public class SophiaBullet : MonoBehaviour
{
    [Header("Persecución")]
    [Tooltip("Tiempo aproximado que debería tardar la bala en alcanzar a Elian al ser disparada.")]
    [SerializeField] private float targetTravelTime = 1.2f;

    [SerializeField] private float minSpeed = 4f;
    [SerializeField] private float maxSpeed = 7f;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Daño")]
    [SerializeField, Min(1)] private int damage = 1;

    [Header("General")]
    [SerializeField] private float lifeTime = 8f;

    private SophiaController sophia;
    private Transform target;

    private float currentSpeed = 6f;
    private bool resolutionNotified = false;

    public void Initialize(
        SophiaController owner,
        Transform player
    )
    {
        sophia = owner;
        target = player;

        CalculateSpeed();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (target == null || sophia == null)
            return;

        Vector2 targetPosition =
            sophia.GetPlayerAimPoint();

        Vector2 direction =
            (targetPosition -
             (Vector2)transform.position).normalized;

        float targetAngle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        float angle =
            Mathf.MoveTowardsAngle(
                transform.eulerAngles.z,
                targetAngle,
                rotationSpeed * Time.deltaTime
            );

        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        transform.Translate(
            Vector2.right *
            currentSpeed *
            Time.deltaTime,
            Space.Self
        );
    }

    private void CalculateSpeed()
    {
        if (sophia == null)
            return;

        Vector2 targetPosition =
            sophia.GetPlayerAimPoint();

        float distance =
            Vector2.Distance(
                transform.position,
                targetPosition
            );

        float calculatedSpeed =
            distance /
            Mathf.Max(targetTravelTime, 0.1f);

        currentSpeed =
            Mathf.Clamp(
                calculatedSpeed,
                minSpeed,
                maxSpeed
            );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        Health playerHealth =
            other.GetComponentInParent<Health>();

        // Elian estuvo quieto el tiempo necesario:
        // acepta el error.
        if (player.IsAcceptingError)
        {
            if (sophia != null)
                sophia.ReceiveError();

            ResolveBullet();
            return;
        }

        // Si intentaba moverse, saltar o esquivar,
        // el error sí le hace daño.
        if (playerHealth != null)
            playerHealth.TakeDamage(damage);

        ResolveBullet();
    }

    private void ResolveBullet()
    {
        NotifyResolved();

        Destroy(gameObject);
    }

    private void NotifyResolved()
    {
        if (resolutionNotified)
            return;

        resolutionNotified = true;

        if (sophia != null)
            sophia.NotifyChasingBulletResolved();
    }

    private void OnDestroy()
    {
        // También desbloquea a Sophia si la bala desaparece
        // por tiempo de vida u otra causa.
        NotifyResolved();
    }
}