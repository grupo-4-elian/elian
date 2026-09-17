using System.Collections;
using UnityEngine;

// Vive en el prefab de la columna (Column_Dorada / Column_Roja).
// Secuencia: muestra un sprite de aviso en el piso (sin dañar todavía),
// y después de telegraphDuration lo apaga y activa la columna real + el daño.
public class ColumnHazard : MonoBehaviour
{
    [Header("Aviso (telegraph)")]
    [Tooltip("SpriteRenderer del hijo que marca el piso como aviso (ej: efecto_especial).")]
    [SerializeField] private SpriteRenderer warningSprite;
    [SerializeField] private float telegraphDuration = 0.9f;

    [Header("Columna activa")]
    [SerializeField] private float activeDuration = 0.4f;
    [SerializeField] private int damage = 1;

    private SpriteRenderer columnSprite;
    private Collider2D columnCollider;
    private bool damageDealt;

    private void Awake()
    {
        columnSprite = GetComponent<SpriteRenderer>();
        columnCollider = GetComponent<Collider2D>();

        if (columnSprite != null)
            columnSprite.enabled = false;

        if (columnCollider != null)
            columnCollider.enabled = false;

        if (warningSprite != null)
            warningSprite.enabled = false;
    }

    private void Start()
    {
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        if (warningSprite != null)
            warningSprite.enabled = true;

        yield return new WaitForSeconds(telegraphDuration);

        if (warningSprite != null)
            warningSprite.enabled = false;

        if (columnSprite != null)
            columnSprite.enabled = true;

        if (columnCollider != null)
            columnCollider.enabled = true;

        yield return new WaitForSeconds(activeDuration);

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (damageDealt)
            return;

        Health health = other.GetComponentInParent<Health>();

        if (health == null)
            return;

        damageDealt = true;
        health.TakeDamage(damage);
    }
}