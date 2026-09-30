using UnityEngine;

[RequireComponent(typeof(CharacterAudio))]
public class SophiaLaughOnPlayerDamage : MonoBehaviour
{
    private CharacterAudio characterAudio;
    private SophiaController sophiaController;
    private Health sophiaHealth;
    private Health playerHealth;

    private int previousPlayerHealth;
    private int accumulatedHits;
    private int hitsUntilLaugh;

    private bool initialized;
    private bool subscribed;

    private void Awake()
    {
        characterAudio = GetComponent<CharacterAudio>();
        sophiaController = GetComponent<SophiaController>();
        sophiaHealth = GetComponent<Health>();
    }

    private void Start()
    {
        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            return;

        playerHealth = player.GetComponent<Health>();

        if (playerHealth == null)
            return;

        previousPlayerHealth =
            playerHealth.CurrentHealth;

        accumulatedHits = 0;
        ChooseNextThreshold();

        initialized = true;
        Subscribe();
    }

    private void OnEnable()
    {
        if (initialized)
            Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (playerHealth == null || subscribed)
            return;

        playerHealth.HealthChanged +=
            OnPlayerHealthChanged;

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (playerHealth == null || !subscribed)
            return;

        playerHealth.HealthChanged -=
            OnPlayerHealthChanged;

        subscribed = false;
    }

    private void OnPlayerHealthChanged(
        int current,
        int max)
    {
        bool tookDamage =
            current < previousPlayerHealth;

        previousPlayerHealth = current;

        if (!tookDamage)
            return;

        if (current <= 0)
            return;

        if (sophiaController == null ||
            !sophiaController.IsBattleActive)
            return;

        if (sophiaHealth != null &&
            sophiaHealth.IsDead)
            return;

        accumulatedHits++;

        if (accumulatedHits < hitsUntilLaugh)
            return;

        if (characterAudio != null)
            characterAudio.PlayLaugh();

        accumulatedHits = 0;
        ChooseNextThreshold();
    }

    private void ChooseNextThreshold()
    {
        // Random.Range(int, int) excluye el máximo:
        // devuelve 2 o 3.
        hitsUntilLaugh = Random.Range(2, 4);
    }
}
