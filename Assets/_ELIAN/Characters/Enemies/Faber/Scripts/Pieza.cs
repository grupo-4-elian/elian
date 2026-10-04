using System.Collections.Generic;
using UnityEngine;

// Pieza fisica del area de descarte. Elian puede empujarla caminando contra
// ella, darle un empujon (tecla de agacharse) o moverla con disparos.
// Las piezas defectuosas (engranaje y placa) se combinan en un artefacto.
[RequireComponent(typeof(Rigidbody2D))]
public class Pieza : MonoBehaviour
{
    public enum TipoPieza { Engranaje, Placa, Artefacto }

    public static readonly List<Pieza> Active = new List<Pieza>();

    [SerializeField] private TipoPieza tipo = TipoPieza.Engranaje;
    [Tooltip("Impulso horizontal que recibe por cada disparo.")]
    [SerializeField] private float shotImpulse = 2.5f;
    [SerializeField] private GameObject breakEffect;

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    public TipoPieza Tipo => tipo;
    public bool Consumed { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable() { Active.Add(this); }
    private void OnDisable() { Active.Remove(this); }

    public void OnShot(Vector2 direction)
    {
        if (Consumed)
            return;
        rb.AddForce(new Vector2(Mathf.Sign(direction.x) * shotImpulse, 0.6f), ForceMode2D.Impulse);
    }

    public void Shove(float direction, float speed)
    {
        if (Consumed)
            return;
        rb.linearVelocity = new Vector2(direction * speed, 1.2f);
    }

    public void SetVisible(bool visible)
    {
        if (sr != null)
            sr.enabled = visible;
    }

    // La pieza entra en FABER o se usa para combinar: desaparece.
    public void Consume()
    {
        if (Consumed)
            return;
        Consumed = true;
        Destroy(gameObject);
    }

    // Un Corrector destruyo el artefacto.
    public void Romper()
    {
        if (Consumed)
            return;
        Consumed = true;
        Sfx.Play(Sfx.GolpeEnemigo);
        if (breakEffect != null)
            Instantiate(breakEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Active.Clear(); }
}
