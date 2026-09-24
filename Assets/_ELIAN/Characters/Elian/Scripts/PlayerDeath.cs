using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Animator))]
public class PlayerDeath : MonoBehaviour
{
    [Tooltip("Segundos entre la muerte y el reinicio del nivel (si quedan vidas).")]
    [SerializeField] private float restartDelay = 2f;
    [Tooltip("Segundos entre la muerte y la pantalla de GAME OVER.")]
    [SerializeField] private float gameOverDelay = 1.2f;

    private enum State { Alive, LifeLost, GameOver }

    private Health health;
    private Animator animator;
    private State state = State.Alive;
    private Texture2D overlay;

    private void Awake()
    {
        health = GetComponent<Health>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        health.Died += OnDeath;
    }

    private void OnDisable()
    {
        health.Died -= OnDeath;
    }

    private void OnDestroy()
    {
        if (overlay != null)
            Destroy(overlay);
    }

    private void OnDeath()
    {
        animator.ResetTrigger("Hurt");
        animator.SetTrigger("Die");

        PlayerHurt ph = GetComponent<PlayerHurt>();
        if (ph != null) ph.enabled = false;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Restamos la vida apenas muere (no despues del delay), asi el
        // HUD se actualiza al instante aunque el reinicio tarde un poco.
        bool hasLivesLeft = LivesManager.LoseLife();
        Sfx.Play(Sfx.Muerte);

        if (hasLivesLeft)
        {
            state = State.LifeLost;
            Invoke(nameof(RestartScene), restartDelay);
        }
        else
        {
            Invoke(nameof(ShowGameOver), gameOverDelay);
        }
    }

    private void ShowGameOver()
    {
        state = State.GameOver;
        Sfx.Play(Sfx.GameOver);
    }

    private void Update()
    {
        if (state != State.GameOver || PauseMenu.IsPaused)
            return;

        // En GAME OVER se espera a que el jugador elija reintentar.
        if (RetryPressed())
        {
            LivesManager.ResetLives();
            RestartScene();
        }
    }

    private static bool RetryPressed()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            return true;

        Gamepad pad = Gamepad.current;
        return pad != null && pad.buttonSouth.wasPressedThisFrame;
    }

    private void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnGUI()
    {
        if (state == State.Alive)
            return;

        if (overlay == null)
        {
            overlay = new Texture2D(1, 1);
            overlay.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.7f));
            overlay.Apply();
        }

        float w = Screen.width;
        float h = Screen.height;

        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        GUIStyle sub = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(h * 0.05f)
        };
        sub.normal.textColor = Color.white;

        if (state == State.LifeLost)
        {
            title.fontSize = Mathf.RoundToInt(h * 0.09f);
            title.normal.textColor = new Color(1f, 0.55f, 0.2f);

            int lives = LivesManager.CurrentLives;
            GUI.Label(new Rect(0, h * 0.30f, w, h * 0.15f), "¡CAÍSTE!", title);
            GUI.Label(new Rect(0, h * 0.46f, w, h * 0.1f),
                lives == 1 ? "Te queda 1 vida" : $"Te quedan {lives} vidas", sub);
            return;
        }

        // GAME OVER
        GUI.DrawTexture(new Rect(0, 0, w, h), overlay);

        title.fontSize = Mathf.RoundToInt(h * 0.15f);
        title.normal.textColor = new Color(0.9f, 0.1f, 0.1f);
        GUI.Label(new Rect(0, h * 0.20f, w, h * 0.22f), "GAME OVER", title);

        GUI.Label(new Rect(0, h * 0.45f, w, h * 0.08f), "Te quedaste sin vidas", sub);

        GUIStyle hint = new GUIStyle(sub) { fontSize = Mathf.RoundToInt(h * 0.045f) };
        hint.normal.textColor = new Color(1f, 0.84f, 0.2f);
        GUI.Label(new Rect(0, h * 0.62f, w, h * 0.08f), "ENTER: reintentar el nivel", hint);

        GUIStyle small = new GUIStyle(sub) { fontSize = Mathf.RoundToInt(h * 0.035f) };
        small.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
        GUI.Label(new Rect(0, h * 0.71f, w, h * 0.07f), "Q: menú (reiniciar partida, elegir nivel, salir)", small);
    }
}
