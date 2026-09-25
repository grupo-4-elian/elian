using System.Collections;
using UnityEngine;

// CORRECTOR: unidad avanzada que mantiene el orden de la fabrica.
// Persigue rapido, escanea (aviso rojo) y embiste. Resiste mas que el
// Ensamblador. En el combate contra FABER tambien busca destruir los
// artefactos improvisados de Elian ("no coinciden con el plano").
public class CorrectorController : FactoryEnemy
{
    [Header("Corrector")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float moveSpeed = 3.8f;
    [Tooltip("Distancia a la que se detiene a escanear antes de embestir.")]
    [SerializeField] private float lungeTriggerRange = 4f;
    [SerializeField] private float lungeSpeed = 11f;
    [SerializeField] private float lungeDuration = 0.28f;
    [SerializeField] private float recoverTime = 0.5f;
    [SerializeField] private float attackCooldown = 1.4f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float hitRadius = 1.3f;

    private bool busy;
    private float lastAttack = -99f;
    private Transform target;

    private void Update()
    {
        if (Paused || player == null)
            return;

        if (busy)
            return;

        target = ChooseTarget();
        float dx = target.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);

        if (dist <= lungeTriggerRange && Time.time >= lastAttack + attackCooldown)
        {
            StartCoroutine(ScanAndLunge(Mathf.Sign(dx)));
        }
        else if (dist <= detectionRange)
        {
            Face(dx);
            anim.Play(dist <= lungeTriggerRange ? "idle" : "run");
        }
        else
        {
            anim.Play("idle");
        }
    }

    private void FixedUpdate()
    {
        if (Paused || player == null || busy || target == null)
        {
            if (!dead && !busy)
                StopMoving();
            return;
        }

        float dx = target.position.x - transform.position.x;
        if (Mathf.Abs(dx) <= detectionRange && Mathf.Abs(dx) > lungeTriggerRange)
            rb.linearVelocity = new Vector2(Mathf.Sign(dx) * moveSpeed, rb.linearVelocity.y);
        else
            StopMoving();
    }

    // Prioriza un artefacto cercano sobre Elian: el sistema elimina desviaciones.
    private Transform ChooseTarget()
    {
        Pieza best = null;
        float bestDist = detectionRange;
        foreach (Pieza p in Pieza.Active)
        {
            if (p == null || p.Tipo != Pieza.TipoPieza.Artefacto)
                continue;
            float d = Mathf.Abs(p.transform.position.x - transform.position.x);
            if (d < bestDist)
            {
                best = p;
                bestDist = d;
            }
        }
        return best != null ? best.transform : player;
    }

    private IEnumerator ScanAndLunge(float direction)
    {
        busy = true;
        lastAttack = Time.time;
        Face(direction);
        StopMoving();

        // Aviso: luz de escaneo roja.
        anim.Play("scan", true);
        yield return new WaitForSeconds(anim.LengthOf("scan"));
        if (dead || Paused) { busy = false; yield break; }

        anim.Play("attack", true);
        if (characterAudio != null)
            characterAudio.PlayAttack();

        bool hitDone = false;
        float t = 0f;
        while (t < lungeDuration && !dead)
        {
            rb.linearVelocity = new Vector2(direction * lungeSpeed, rb.linearVelocity.y);
            if (!hitDone)
                hitDone = TryHit();
            t += Time.deltaTime;
            yield return null;
        }

        StopMoving();
        yield return new WaitForSeconds(recoverTime);
        busy = false;
    }

    private bool TryHit()
    {
        Vector2 center = (Vector2)transform.position + Vector2.up * 1.4f + (Vector2)transform.right * 0.8f;

        if (player != null && Vector2.Distance(center, (Vector2)player.position + Vector2.up * 1.4f) <= hitRadius + 0.6f)
        {
            DamagePlayer(attackDamage);
            return true;
        }

        foreach (Pieza p in Pieza.Active)
        {
            if (p != null && p.Tipo == Pieza.TipoPieza.Artefacto &&
                Vector2.Distance(center, p.transform.position) <= hitRadius + 0.4f)
            {
                p.Romper();
                return true;
            }
        }
        return false;
    }
}
