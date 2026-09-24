using UnityEngine;

// "Prensa" del Limbo del Trabajo: cada cierto tiempo instancia un peligro
// (por ejemplo una columna con ColumnHazard, que avisa antes de dañar)
// en un punto fijo del suelo. Solo funciona con el jugador cerca, fuera de
// dialogos y mientras Elian este vivo.
public class PeriodicHazardSpawner : MonoBehaviour
{
    [Header("Peligro")]
    [SerializeField] private GameObject hazardPrefab;
    [SerializeField] private float interval = 3.5f;
    [Tooltip("Retraso del primer disparo, para desfasar varias prensas entre si.")]
    [SerializeField] private float startDelay = 0f;

    [Header("Colocacion")]
    [Tooltip("Sube o baja el peligro respecto del suelo detectado.")]
    [SerializeField] private float spawnYOffset = 2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRaycastDistance = 20f;

    [Header("Activacion")]
    [Tooltip("Distancia horizontal maxima al jugador para que la prensa funcione.")]
    [SerializeField] private float activationRange = 14f;

    private Transform player;
    private Health playerHealth;
    private float nextSpawnTime;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<Health>();
        }

        nextSpawnTime = Time.time + startDelay;
    }

    private void Update()
    {
        if (hazardPrefab == null || player == null)
            return;

        bool paused = DialogueManager.IsDialogueActive ||
                      (playerHealth != null && playerHealth.IsDead) ||
                      Mathf.Abs(player.position.x - transform.position.x) > activationRange;

        if (paused)
        {
            // Al volver a estar activa espera un ciclo completo, asi no
            // golpea apenas el jugador entra en rango.
            nextSpawnTime = Mathf.Max(nextSpawnTime, Time.time + interval * 0.5f);
            return;
        }

        if (Time.time < nextSpawnTime)
            return;

        nextSpawnTime = Time.time + interval;
        Instantiate(hazardPrefab, GetSpawnPosition(), Quaternion.identity);
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3 origin = transform.position + Vector3.up * 10f;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundRaycastDistance, groundLayer);

        float y = hit.collider != null ? hit.point.y : transform.position.y;
        return new Vector3(transform.position.x, y + spawnYOffset, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2.5f, new Vector3(1f, 5f, 0f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(activationRange * 2f, 0.2f, 0f));
    }
}
