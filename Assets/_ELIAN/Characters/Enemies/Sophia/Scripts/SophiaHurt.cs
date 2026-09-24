using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Animator))]
public class SophiaHurt : MonoBehaviour
{
    private Health health;
    private Animator animator;

    private void Awake()
    {
        health = GetComponent<Health>();
        animator = GetComponent<Animator>();
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
        if (current <= 0)
            return;

        // Sophia_AC todavia no tiene parametro "Hurt"; sin este chequeo
        // Unity tira el warning "Parameter 'Hurt' does not exist" en cada golpe.
        if (HasParameter(animator, "Hurt"))
            animator.SetTrigger("Hurt");

        Sfx.Play(Sfx.GolpeEnemigo);
    }

    public static bool HasParameter(Animator anim, string paramName)
    {
        if (anim == null || anim.runtimeAnimatorController == null)
            return false;

        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            if (p.name == paramName)
                return true;
        }

        return false;
    }
}
