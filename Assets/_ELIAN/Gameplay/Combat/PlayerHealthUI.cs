using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Health health;
    [SerializeField] private Image[] pips; // Pip_1 a Pip_6, en orden de izquierda a derecha

    [Header("Colores por umbral")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color midColor = Color.yellow;
    [SerializeField] private Color lowColor = Color.red;
    [Range(0f, 1f)]
    [SerializeField] private float midThreshold = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float lowThreshold = 0.25f;

    private void OnEnable()
    {
        if (health != null)
            health.HealthChanged += UpdateBar;
    }

    private void OnDisable()
    {
        if (health != null)
            health.HealthChanged -= UpdateBar;
    }

    private void Start()
    {
        // Health.Awake ya corrio antes que este Start, asi que CurrentHealth
        // esta listo. Pintamos el estado inicial una sola vez, ya que
        // HealthChanged solo se dispara en TakeDamage/RestoreFullHealth.
        if (health != null)
            UpdateBar(health.CurrentHealth, health.MaxHealth);
    }

    private void UpdateBar(int current, int max)
    {
        int pipsToShow = Mathf.CeilToInt((float)current / max * pips.Length);
        float percent = (float)current / max;

        Color activeColor;
        if (percent <= lowThreshold)
            activeColor = lowColor;
        else if (percent <= midThreshold)
            activeColor = midColor;
        else
            activeColor = normalColor;

        for (int i = 0; i < pips.Length; i++)
        {
            pips[i].enabled = i < pipsToShow;

            if (i < pipsToShow)
                pips[i].color = activeColor;
        }
    }
}