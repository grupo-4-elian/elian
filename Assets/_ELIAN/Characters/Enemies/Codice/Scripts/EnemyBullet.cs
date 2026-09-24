using UnityEngine;

public class EnemyBullet : MonoBehaviour
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
        // Ignora a otros enemigos (para que no se hagan dano entre si).
        // Todo lo demas (Player, paredes, suelo) la destruye igual que
        // tu Bullet.cs original destruye contra todo lo que no sea Player.
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            return;

        Health health = other.GetComponentInParent<Health>();

        // Zonas invisibles (dialogos, encuentros), columnas y otras balas:
        // son triggers sin vida, la bala los atraviesa.
        if (other.isTrigger && health == null)
            return;

        if (health != null)
            health.TakeDamage(damage);

        Destroy(gameObject);
    }
}
