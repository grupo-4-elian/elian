using System.Collections;
using UnityEngine;

// DRON DE SOLDADURA: unidad voladora de apoyo. Mantiene su altura, se
// acomoda a cierta distancia de Elian y dispara chispas de soldadura.
public class DronSoldaduraController : FactoryEnemy
{
    [Header("Dron de soldadura")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float preferredDistance = 5f;
    [SerializeField] private float moveSpeed = 2.6f;
    [SerializeField] private float shootCooldown = 1.9f;
    [SerializeField] private int shotFrame = 3;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Vector2 muzzleOffset = new Vector2(0.9f, 0f);
    [SerializeField] private float bobAmplitude = 0.15f;

    protected override bool IsFlying => true;

    private float baseY;
    private float lastShot = -99f;
    private bool shooting;

    protected override void OnEnable()
    {
        base.OnEnable();
        baseY = transform.position.y;
    }

    private void Update()
    {
        if (Paused || player == null || shooting)
            return;

        float dist = HorizontalDistanceToPlayer();
        if (dist > detectionRange)
        {
            anim.Play("idle");
            return;
        }

        Face(DirectionToPlayer());

        if (Time.time >= lastShot + shootCooldown && dist <= preferredDistance + 2f)
            StartCoroutine(Shoot());
        else
            anim.Play(Mathf.Abs(dist - preferredDistance) > 0.5f ? "move" : "idle");
    }

    private void FixedUpdate()
    {
        if (dead)
            return;

        float bob = Mathf.Sin(Time.time * 3f) * bobAmplitude;
        float vy = (baseY + bob - transform.position.y) * 4f;

        if (Paused || player == null || shooting)
        {
            rb.linearVelocity = new Vector2(0f, vy);
            return;
        }

        float dist = HorizontalDistanceToPlayer();
        float vx = 0f;
        if (dist <= detectionRange)
        {
            if (dist > preferredDistance + 0.5f) vx = DirectionToPlayer() * moveSpeed;
            else if (dist < preferredDistance - 1.5f) vx = -DirectionToPlayer() * moveSpeed;
        }
        rb.linearVelocity = new Vector2(vx, vy);
    }

    private IEnumerator Shoot()
    {
        shooting = true;
        lastShot = Time.time;
        anim.Play("attack", true);

        float len = anim.LengthOf("attack");
        float perFrame = len > 0f ? len / 6f : 0.08f;
        yield return new WaitForSeconds(perFrame * shotFrame);

        if (!dead && !Paused && bulletPrefab != null && player != null)
        {
            Vector2 muzzle = (Vector2)transform.position + (Vector2)transform.right * muzzleOffset.x + Vector2.up * muzzleOffset.y;
            Vector2 aim = ((Vector2)player.position + Vector2.up * 1.4f - muzzle).normalized;
            float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            Instantiate(bulletPrefab, muzzle, Quaternion.Euler(0f, 0f, angle));

            if (characterAudio != null && characterAudio.HasShootSound)
                characterAudio.PlayShoot();
            else
                Sfx.Play(Sfx.DisparoEnemigo, 0.7f);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, len - perFrame * shotFrame));
        shooting = false;
    }
}
