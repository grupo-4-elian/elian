using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 3f;

    [Header("Daño")]
    [SerializeField, Min(1)] private int damage = 1;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            return;

        Health health = other.GetComponentInParent<Health>();

        // Un collider hijo del jugador que no tenga el tag "Player".
        if (health != null && health.CompareTag("Player"))
            return;

        // Zonas invisibles (dialogos, encuentros), columnas y otras balas:
        // son triggers sin vida, la bala los atraviesa.
        if (other.isTrigger && health == null)
            return;

        if (health != null)
            health.TakeDamage(damage);

        Destroy(gameObject);
    }
}
