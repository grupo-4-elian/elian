using System.Collections;
using UnityEngine;

// Golpe de brazo de FABER: marca el suelo (aviso) y el brazo cae encima.
public class ArmSlam : MonoBehaviour
{
    [SerializeField] private SpriteRenderer marker;
    [SerializeField] private FrameAnimator arm;
    [SerializeField] private SpriteRenderer armRenderer;
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitHalfWidth = 0.9f;
    [SerializeField] private int impactFrame = 3;

    public void Begin(float telegraph)
    {
        StartCoroutine(Run(telegraph));
    }

    private IEnumerator Run(float telegraph)
    {
        armRenderer.enabled = false;
        marker.enabled = true;

        // Aviso: la marca parpadea.
        float t = 0f;
        while (t < telegraph)
        {
            if (!PauseMenu.IsPaused)
            {
                marker.color = new Color(1f, 1f, 1f, 0.35f + 0.65f * Mathf.PingPong(t * 6f, 1f));
                t += Time.deltaTime;
            }
            yield return null;
        }

        marker.enabled = false;
        armRenderer.enabled = true;
        arm.Play("slam", true);

        float perFrame = arm.LengthOf("slam") / 6f;
        yield return new WaitForSeconds(perFrame * impactFrame);

        Sfx.Play(Sfx.Descarga, 0.8f);
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null && Mathf.Abs(p.transform.position.x - transform.position.x) <= hitHalfWidth + 1.1f)
        {
            Health h = p.GetComponent<Health>();
            if (h != null) h.TakeDamage(damage);
        }

        yield return new WaitForSeconds(perFrame * (6 - impactFrame) + 0.1f);
        Destroy(gameObject);
    }
}
