using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Pantalla de victoria simple (OnGUI), no necesita nada armado en la escena.
// SophiaDeath la crea con VictoryScreen.Show() cuando muere el jefe.
//  - Si hay siguiente nivel: "NIVEL X SUPERADO" y, tras una cuenta regresiva,
//    carga el siguiente nivel automaticamente (ENTER lo adelanta). Las vidas
//    se conservan.
//  - Si es el ultimo nivel: "VICTORIA" y ENTER vuelve a empezar desde el
//    primer nivel con las vidas completas.
public class VictoryScreen : MonoBehaviour
{
    private const float AutoAdvanceSeconds = 4f;

    private float showAt;
    private bool visible;
    private Texture2D overlay;

    private string bossName;
    private string nextSceneName;
    private bool hasNextLevel;
    private int levelNumber;

    // Una vez ganada la pelea, Elian no puede morir por un proyectil
    // o columna que haya quedado en el aire.
    private Health playerHealth;

    public static bool IsShowing { get; private set; }

    public static void Show(float delay, string bossName = "Sophia", string nextSceneName = "")
    {
        // Evita duplicados si se llama mas de una vez.
        if (FindAnyObjectByType<VictoryScreen>() != null)
            return;

        GameObject go = new GameObject("VictoryScreen");
        VictoryScreen screen = go.AddComponent<VictoryScreen>();
        screen.showAt = Time.time + delay;
        screen.bossName = bossName;
        screen.nextSceneName = nextSceneName;
        screen.levelNumber = SceneManager.GetActiveScene().buildIndex + 1;
        screen.hasNextLevel = !string.IsNullOrEmpty(nextSceneName) &&
                              Application.CanStreamedLevelBeLoaded(nextSceneName);

        if (!string.IsNullOrEmpty(nextSceneName) && !screen.hasNextLevel)
            Debug.LogWarning($"[VictoryScreen] La escena '{nextSceneName}' no esta en Build Settings; se trata como ultimo nivel.");
    }

    private void Start()
    {
        overlay = new Texture2D(1, 1);
        overlay.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
        overlay.Apply();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerHealth = player.GetComponent<Health>();
    }

    private void OnDestroy()
    {
        IsShowing = false;

        if (overlay != null)
            Destroy(overlay);
    }

    private void Update()
    {
        // Se reafirma cada frame: el parpadeo de PlayerHurt la apaga al terminar.
        if (playerHealth != null)
            playerHealth.IsInvulnerable = true;

        if (!visible)
        {
            if (Time.time >= showAt)
            {
                visible = true;
                IsShowing = true;
                Sfx.Play(Sfx.Victoria);
            }
            return;
        }

        if (hasNextLevel)
        {
            if (ContinuePressed() || Time.time >= showAt + AutoAdvanceSeconds)
                SceneManager.LoadScene(nextSceneName);
        }
        else if (ContinuePressed())
        {
            LivesManager.ResetLives();
            SceneManager.LoadScene(0);
        }
    }

    private static bool ContinuePressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame ||
             keyboard.numpadEnterKey.wasPressedThisFrame))
            return true;

        Gamepad gamepad = Gamepad.current;
        return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
    }

    private void OnGUI()
    {
        if (!visible)
            return;

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlay);

        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(Screen.height * 0.11f),
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        title.normal.textColor = new Color(1f, 0.84f, 0.2f); // dorado

        GUIStyle sub = new GUIStyle(title)
        {
            fontSize = Mathf.RoundToInt(Screen.height * 0.05f),
            fontStyle = FontStyle.Normal
        };
        sub.normal.textColor = Color.white;

        string titleText;
        string hintText;

        if (hasNextLevel)
        {
            titleText = $"¡NIVEL {levelNumber} SUPERADO!";
            int secondsLeft = Mathf.Max(1, Mathf.CeilToInt(showAt + AutoAdvanceSeconds - Time.time));
            hintText = $"Nivel {levelNumber + 1} en {secondsLeft}...   (ENTER para continuar ya)";
        }
        else
        {
            titleText = "¡VICTORIA!";
            hintText = "Presiona ENTER para jugar de nuevo";
        }

        float h = Screen.height;
        GUI.Label(new Rect(0, h * 0.25f, Screen.width, h * 0.2f), titleText, title);
        GUI.Label(new Rect(0, h * 0.45f, Screen.width, h * 0.1f), $"Derrotaste a {bossName}", sub);
        GUI.Label(new Rect(0, h * 0.62f, Screen.width, h * 0.1f), hintText, sub);
    }
}
