using System.Collections;
using UnityEngine;

// ENSAMBLADOR: unidad basica de fabricacion. Detecta a Elian, se acerca,
// ataca cuerpo a cuerpo y aparece en grupos. Poca resistencia.
public class EnsambladorController : FactoryEnemy
{
    [Header("Ensamblador")]
    [SerializeField] private float detectionRange = 9f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float attackCooldown = 1.6f;
    [SerializeField] private int attackDamage = 1;
    [Tooltip("Cuadro de la animacion de ataque en el que golpea.")]
    [SerializeField] private int hitFrame = 3;

    private bool attacking;
    private float lastAttack = -99f;

    private void Update()
    {
        if (Paused || player == null)
            return;

        if (attacking)
            return;

        float dist = HorizontalDistanceToPlayer();
        float verticalGap = Mathf.Abs(player.position.y - transform.position.y);

        if (dist <= attackRange && verticalGap < 2f)
        {
            Face(DirectionToPlayer());
            if (Time.time >= lastAttack + attackCooldown)
                StartCoroutine(Attack());
            else
                anim.Play("idle");
        }
        else if (dist <= detectionRange)
        {
            Face(DirectionToPlayer());
            anim.Play("walk");
        }
        else
        {
            anim.Play("idle");
        }
    }

    private void FixedUpdate()
    {
        if (Paused || player == null || attacking)
        {
            if (!dead)
                StopMoving();
            return;
        }

        float dist = HorizontalDistanceToPlayer();
        if (dist <= detectionRange && dist > attackRange)
            rb.linearVelocity = new Vector2(DirectionToPlayer() * moveSpeed, rb.linearVelocity.y);
        else
            StopMoving();
    }

    private IEnumerator Attack()
    {
        attacking = true;
        lastAttack = Time.time;
        StopMoving();
        anim.Play("attack", true);

        if (characterAudio != null)
            characterAudio.PlayAttack();

        float fps = anim.LengthOf("attack") > 0f ? 6f / anim.LengthOf("attack") : 12f;
        yield return new WaitForSeconds(hitFrame / fps);

        if (!dead && !Paused && HorizontalDistanceToPlayer() <= attackRange + 0.3f &&
            Mathf.Abs(player.position.y - transform.position.y) < 2f)
            DamagePlayer(attackDamage);

        yield return new WaitForSeconds(Mathf.Max(0f, anim.LengthOf("attack") - hitFrame / fps));
        attacking = false;
    }
}
