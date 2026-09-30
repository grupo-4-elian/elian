using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Health health;
    [Tooltip("Image con Image Type = Filled (Fill Method Horizontal, Fill Origin Left).")]
    [SerializeField] private Image fillImage;

    [Header("Al morir")]
    [Tooltip("Segundos que espera antes de ocultar la barra, para que coincida con la animación de muerte del boss.")]
    [SerializeField] private float deathHideDelay = 1.5f;

    private void OnEnable()
    {
        if (health != null)
        {
            health.HealthChanged += UpdateBar;
            health.Died += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.HealthChanged -= UpdateBar;
            health.Died -= HandleDeath;
        }
    }

    private void Start()
    {
        // Igual que en PlayerHealthUI: pintamos el estado inicial una vez,
        // porque HealthChanged solo se dispara en TakeDamage/RestoreFullHealth.
        if (health != null)
            UpdateBar(health.CurrentHealth, health.MaxHealth);
    }

    private void UpdateBar(int current, int max)
    {
        if (fillImage == null || max <= 0)
            return;

        fillImage.fillAmount = (float)current / max;
    }

    private void HandleDeath()
    {
        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(deathHideDelay);
        gameObject.SetActive(false);
    }
}