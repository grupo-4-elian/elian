using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Menu de pausa: se abre y cierra con Q (o Esc / Start del mando).
// Opciones: continuar, controles, reiniciar nivel, reiniciar partida,
// ir a un nivel y salir.
//
// Se crea solo al arrancar el juego (no hace falta ponerlo en ninguna escena)
// y sobrevive a los cambios de escena.
public class PauseMenu : MonoBehaviour
{
    // Niveles a los que se puede saltar desde el menu (deben estar en Build Settings).
    private static readonly (string label, string scene)[] Levels =
    {
        ("Ir al Nivel 1 - Educación", "SCN_01_Education"),
        ("Ir al Nivel 2 - Trabajo", "SCN_02_Trabajo"),
    };

    private static bool paused;
    private static int resumeFrame = -1;

    // Tambien es true en el frame en que se cierra el menu, para que la
    // tecla que eligio "Continuar" (Espacio/Enter) no haga saltar a Elian.
    public static bool IsPaused => paused || Time.frameCount == resumeFrame;

    private enum Action { Resume, Controls, RestartLevel, RestartGame, LoadLevel, Quit }

    // Tabla de controles: (seccion, tecla, descripcion). Seccion != null inicia un grupo.
    private static readonly (string section, string key, string description)[] ControlsTable =
    {
        ("MOVIMIENTO", "A / D", "Moverse a la izquierda / derecha"),
        (null, "ESPACIO", "Saltar"),
        (null, "S", "Agacharse"),
        (null, "W", "Apuntar hacia arriba"),
        ("COMBATE", "J", "Disparar (de pie, hacia arriba o agachado)"),
        ("TÚNELES (NIVEL 2)", "A / D", "Arrastrarse agachado (no se puede saltar)"),
        (null, "J", "Disparar mientras te arrastras"),
        ("DIÁLOGOS Y MENÚS", "ESPACIO / ENTER", "Avanzar el diálogo"),
        (null, "Q / ESC", "Abrir o cerrar este menú"),
        (null, "ENTER", "Continuar (victoria o game over)"),
    };

    private bool showControls;

    private struct Option
    {
        public string Label;
        public Action Action;
        public string Scene;
    }

    private Option[] options;
    private int selected;
    private Texture2D overlay;
    private Texture2D highlight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateOnStart()
    {
        if (FindAnyObjectByType<PauseMenu>() != null)
            return;

        GameObject go = new GameObject("PauseMenu");
        DontDestroyOnLoad(go);
        go.AddComponent<PauseMenu>();
    }

    private void Awake()
    {
        overlay = MakeTexture(new Color(0f, 0f, 0f, 0.75f));
        highlight = MakeTexture(new Color(1f, 0.84f, 0.2f, 0.25f));
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetPaused(false);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SetPaused(false);
        if (overlay != null) Destroy(overlay);
        if (highlight != null) Destroy(highlight);
    }

    private void BuildOptions()
    {
        var list = new System.Collections.Generic.List<Option>
        {
            new Option { Label = "Continuar", Action = Action.Resume },
            new Option { Label = "Controles", Action = Action.Controls },
            new Option { Label = "Reiniciar nivel", Action = Action.RestartLevel },
            new Option { Label = "Reiniciar partida (desde el Nivel 1)", Action = Action.RestartGame },
        };

        string current = SceneManager.GetActiveScene().name;
        foreach (var level in Levels)
        {
            if (level.scene != current && Application.CanStreamedLevelBeLoaded(level.scene))
                list.Add(new Option { Label = level.label, Action = Action.LoadLevel, Scene = level.scene });
        }

        list.Add(new Option { Label = "Salir del juego", Action = Action.Quit });
        options = list.ToArray();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        Gamepad pad = Gamepad.current;

        bool toggle = (kb != null && (kb.qKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) ||
                      (pad != null && pad.startButton.wasPressedThisFrame);

        // En la pantalla de controles, Q / Esc / Enter vuelven a la lista.
        if (paused && showControls)
        {
            bool back = toggle || (kb != null && (kb.enterKey.wasPressedThisFrame ||
                                                  kb.spaceKey.wasPressedThisFrame ||
                                                  kb.backspaceKey.wasPressedThisFrame)) ||
                        (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame));
            if (back)
            {
                showControls = false;
                Sfx.Play(Sfx.Menu);
            }
            return;
        }

        if (toggle)
        {
            // No se pausa encima de la pantalla de victoria.
            if (!paused && VictoryScreen.IsShowing)
                return;

            SetPaused(!paused);
            Sfx.Play(Sfx.Menu);
            return;
        }

        if (!paused || options == null)
            return;

        bool up = (kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)) ||
                  (pad != null && pad.dpad.up.wasPressedThisFrame);
        bool down = (kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) ||
                    (pad != null && pad.dpad.down.wasPressedThisFrame);
        bool confirm = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame ||
                                       kb.spaceKey.wasPressedThisFrame)) ||
                       (pad != null && pad.buttonSouth.wasPressedThisFrame);

        if (up) Select((selected - 1 + options.Length) % options.Length);
        if (down) Select((selected + 1) % options.Length);
        if (confirm) Execute(options[selected]);
    }

    private void Select(int index)
    {
        if (index == selected)
            return;

        selected = index;
        Sfx.Play(Sfx.Menu, 0.6f);
    }

    private void SetPaused(bool value)
    {
        if (paused && !value)
            resumeFrame = Time.frameCount;

        paused = value;
        showControls = false;
        Time.timeScale = value ? 0f : 1f;
        AudioListener.pause = value;

        if (value)
        {
            BuildOptions();
            selected = 0;
        }
    }

    private void Execute(Option option)
    {
        Sfx.Play(Sfx.MenuOk);

        switch (option.Action)
        {
            case Action.Resume:
                SetPaused(false);
                break;

            case Action.Controls:
                showControls = true;
                break;

            case Action.RestartLevel:
                SetPaused(false);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                break;

            case Action.RestartGame:
                SetPaused(false);
                LivesManager.ResetLives();
                SceneManager.LoadScene(0);
                break;

            case Action.LoadLevel:
                SetPaused(false);
                LivesManager.ResetLives();
                SceneManager.LoadScene(option.Scene);
                break;

            case Action.Quit:
                SetPaused(false);
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }

    private void OnGUI()
    {
        if (!paused || options == null)
            return;

        // El menu se dibuja por encima de cualquier otro OnGUI (victoria, game over).
        GUI.depth = -100;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlay);

        if (showControls)
        {
            DrawControls();
            return;
        }

        float h = Screen.height;
        float w = Screen.width;

        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(h * 0.09f),
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        title.normal.textColor = new Color(0.4f, 0.9f, 1f);
        GUI.Label(new Rect(0, h * 0.08f, w, h * 0.12f), "PAUSA", title);

        GUIStyle item = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(h * 0.045f),
            alignment = TextAnchor.MiddleCenter
        };

        float rowH = h * 0.075f;
        float startY = h * 0.26f;
        float boxW = Mathf.Min(w * 0.7f, h * 1.2f);
        float boxX = (w - boxW) * 0.5f;

        for (int i = 0; i < options.Length; i++)
        {
            Rect row = new Rect(boxX, startY + i * rowH, boxW, rowH);
            bool isSelected = i == selected;

            if (isSelected)
                GUI.DrawTexture(row, highlight);

            item.normal.textColor = isSelected ? new Color(1f, 0.84f, 0.2f) : Color.white;
            item.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;

            // Tambien se puede elegir con el mouse.
            if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.Repaint)
            {
                if (row.Contains(Event.current.mousePosition))
                    selected = i;
            }

            if (GUI.Button(row, (isSelected ? "▶  " : "") + options[i].Label, item))
            {
                Execute(options[i]);
                return;
            }
        }

        GUIStyle help = new GUIStyle(item)
        {
            fontSize = Mathf.RoundToInt(h * 0.03f),
            fontStyle = FontStyle.Normal
        };
        help.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
        GUI.Label(new Rect(0, h * 0.9f, w, h * 0.06f),
            "W/S o flechas: elegir   ·   ENTER: aceptar   ·   Q: volver al juego", help);
    }

    private void DrawControls()
    {
        float h = Screen.height;
        float w = Screen.width;

        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(h * 0.075f),
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        title.normal.textColor = new Color(0.4f, 0.9f, 1f);
        GUI.Label(new Rect(0, h * 0.03f, w, h * 0.1f), "CONTROLES", title);

        GUIStyle section = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(h * 0.032f),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        section.normal.textColor = new Color(0.4f, 0.9f, 1f, 0.9f);

        GUIStyle key = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(h * 0.034f),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        key.normal.textColor = new Color(1f, 0.84f, 0.2f);

        GUIStyle desc = new GUIStyle(key)
        {
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleLeft
        };
        desc.normal.textColor = Color.white;

        float colW = Mathf.Min(w * 0.9f, h * 1.6f);
        float left = (w - colW) * 0.5f;
        float keyW = colW * 0.34f;
        float gap = colW * 0.04f;
        float rowH = h * 0.048f;
        float y = h * 0.14f;

        foreach (var c in ControlsTable)
        {
            if (c.section != null)
            {
                y += rowH * 0.25f;
                GUI.Label(new Rect(left, y, colW, rowH), c.section, section);
                y += rowH;
            }

            GUI.Label(new Rect(left, y, keyW, rowH), c.key, key);
            GUI.Label(new Rect(left + keyW + gap, y, colW - keyW - gap, rowH), c.description, desc);
            y += rowH;
        }

        GUIStyle help = new GUIStyle(desc)
        {
            fontSize = Mathf.RoundToInt(h * 0.03f),
            alignment = TextAnchor.MiddleCenter
        };
        help.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
        GUI.Label(new Rect(0, h * 0.92f, w, h * 0.06f), "ENTER / Q: volver al menú", help);
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, color);
        t.Apply();
        return t;
    }
}
