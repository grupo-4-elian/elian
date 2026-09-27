
using System.Collections;
using UnityEngine;

public class HurtFlash : MonoBehaviour
{
    [SerializeField] private Color hurtColor = new Color(1f, 0.45f, 0.35f);

    private SpriteRenderer sprite;
    private Health health;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.HealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        health.HealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int current, int max)
    {
        StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        if (sprite == null)
            yield break;

        sprite.color = hurtColor;
        yield return new WaitForSeconds(0.08f);
        sprite.color = Color.white;
    }
}