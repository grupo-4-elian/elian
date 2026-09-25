using UnityEngine;

// Tolva del area de descarte: deja caer piezas defectuosas cada cierto tiempo.
// En la fase final su seguro se puede romper a disparos y vuelca todo el
// descarte sobre la cinta.
public class Tolva : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private GameObject engranajePrefab;
    [SerializeField] private GameObject placaPrefab;
    [SerializeField] private float interval = 3.5f;
    [SerializeField] private int maxLoosePieces = 5;
    [SerializeField] private int dumpCount = 9;
    [SerializeField] private Health latch;
    [Tooltip("Velocidad horizontal con la que sale despedido el descarte al romper el seguro.")]
    [SerializeField] private float dumpSpeedMin = 4.5f;
    [SerializeField] private float dumpSpeedMax = 7.5f;

    private float nextDrop;
    private bool dropping;
    private bool dumped;
    private int count;

    public event System.Action Dumped;
    public bool IsDumped => dumped;

    private Transform player;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    private void Awake()
    {
        if (latch != null)
        {
            latch.SetInvulnerable(true);
            latch.Died += Dump;
        }
    }

    public void StartDropping()
    {
        dropping = true;
        nextDrop = Time.time + 1.5f;
    }

    public void StopDropping() { dropping = false; }

    // Fase 3: el seguro pasa a ser vulnerable.
    public void ExposeLatch()
    {
        if (latch == null)
            return;
        latch.SetInvulnerable(false);
        StartCoroutine(BlinkLatch());
    }

    // El candado parpadea para que se vea que ahora se puede romper.
    private System.Collections.IEnumerator BlinkLatch()
    {
        SpriteRenderer lsr = latch.GetComponent<SpriteRenderer>();
        Vector3 baseScale = latch.transform.localScale;
        while (!dumped && lsr != null)
        {
            float k = Mathf.PingPong(Time.time * 4f, 1f);
            lsr.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.2f), k);
            latch.transform.localScale = baseScale * (1f + 0.25f * k);
            yield return null;
        }
    }

    private void Update()
    {
        if (!dropping || dumped || PauseMenu.IsPaused || Time.time < nextDrop)
            return;

        nextDrop = Time.time + interval;

        // Nunca se queda sin piezas: si ya hay demasiadas sueltas (amontonadas,
        // empujadas lejos o atascadas), se retira la mas vieja que no este
        // junto a Elian y cae una nueva.
        int loose = 0;
        Pieza oldestFar = null;
        foreach (Pieza p in Pieza.Active)
        {
            if (p == null || p.Consumed || p.Tipo == Pieza.TipoPieza.Artefacto)
                continue;
            loose++;
            if (oldestFar == null && (player == null || Mathf.Abs(p.transform.position.x - player.position.x) > 2.5f))
                oldestFar = p;   // Active esta en orden de aparicion
        }

        if (loose >= maxLoosePieces)
        {
            if (oldestFar == null)
                return;   // todas estan junto a Elian: ya tiene con que jugar
            oldestFar.Consume();
        }

        DropOne(Vector2.zero);
    }

    private GameObject DropOne(Vector2 offset)
    {
        // Alterna tipos para que siempre se pueda combinar un par.
        GameObject prefab = count++ % 2 == 0 ? engranajePrefab : placaPrefab;
        if (prefab == null || dropPoint == null)
            return null;
        return Instantiate(prefab, (Vector2)dropPoint.position + offset, Quaternion.identity);
    }

    private void Dump()
    {
        if (dumped)
            return;
        dumped = true;
        dropping = false;

        if (body != null && openSprite != null)
            body.sprite = openSprite;

        Sfx.Play(Sfx.Explosion);
        // Elian esta justo debajo (acaba de romper el candado): el descarte
        // lo atraviesa en vez de rebotar en el y quedarse fuera de la cinta.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Collider2D[] playerCols = player != null ? player.GetComponentsInChildren<Collider2D>() : new Collider2D[0];

        // El descarte sale despedido hacia la cinta, que lo lleva hasta FABER.
        for (int i = 0; i < dumpCount; i++)
        {
            GameObject piece = DropOne(new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(0f, 1.2f)));
            if (piece != null)
                foreach (Collider2D pc in piece.GetComponentsInChildren<Collider2D>())
                    foreach (Collider2D ec in playerCols)
                        Physics2D.IgnoreCollision(pc, ec);
            Rigidbody2D rb = piece != null ? piece.GetComponent<Rigidbody2D>() : null;
            if (rb != null)
                rb.linearVelocity = new Vector2(Random.Range(dumpSpeedMin, dumpSpeedMax), Random.Range(1f, 4f));
        }

        if (latch != null)
            latch.gameObject.SetActive(false);

        Dumped?.Invoke();
    }
}
