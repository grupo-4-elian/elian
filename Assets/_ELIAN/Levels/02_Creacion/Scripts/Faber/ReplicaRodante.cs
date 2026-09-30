using UnityEngine;

// Replica de FABER (fase 3): una copia perfecta que rueda hacia Elian.
// Se esquiva saltando o se destruye con un disparo.
[RequireComponent(typeof(Health))]
public class ReplicaRodante : MonoBehaviour
{
    [SerializeField] private float speed = 4.5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifeTime = 9f;

    private Health health;
    private Collider2D body;
    private Collider2D playerBody;
    private Health playerHealth;
    private float direction = -1f;

    private void Awake()
    {
        health = GetComponent<Health>();
        body = GetComponent<Collider2D>();
        health.Died += () => { Sfx.Play(Sfx.GolpeEnemigo); Destroy(gameObject); };
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            playerBody = p.GetComponent<Collider2D>();
            playerHealth = p.GetComponent<Health>();
        }
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (PauseMenu.IsPaused)
            return;
        transform.position += Vector3.right * direction * speed * Time.deltaTime;

        // La capa Enemy no choca con la del jugador (asi los robots no lo
        // empujan), por eso el golpe se comprueba a mano: si toca a Elian, dania.
        if (playerBody != null && body != null && playerHealth != null && !playerHealth.IsDead &&
            playerBody.bounds.Intersects(body.bounds))
        {
            playerHealth.TakeDamage(damage);
            Sfx.Play(Sfx.GolpeEnemigo, 0.8f);
            Destroy(gameObject);
        }
    }
}
