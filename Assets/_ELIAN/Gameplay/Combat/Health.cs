using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField, Min(1)] private int maxHealth = 3;

    [Header("Invulnerabilidad")]
    [SerializeField] private bool invulnerable = false;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    // Mientras sea true, TakeDamage no hace nada (frames de invencibilidad
    // de Elian, jefe antes de la pelea, etc.). Equivale a SetInvulnerable().
    public bool IsInvulnerable
    {
        get => invulnerable;
        set => invulnerable = value;
    }

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead || invulnerable)
            return;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);

        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (IsDead)
            Died?.Invoke();
    }

    public void SetInvulnerable(bool value)
    {
        invulnerable = value;
    }

    public void RestoreFullHealth()
    {
        CurrentHealth = maxHealth;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}