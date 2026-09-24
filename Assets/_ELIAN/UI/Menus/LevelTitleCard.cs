using UnityEngine;

// Muestra el nombre del nivel al empezar, con fundido de entrada y salida.
// Es solo visual (OnGUI): no bloquea al jugador ni necesita un Canvas.
public class LevelTitleCard : MonoBehaviour
{
    [SerializeField] private string subtitle = "NIVEL 1";
    [SerializeField] private string title = "LIMBO DE LA EDUCACIÓN";
    [SerializeField] private Color titleColor = new Color(0.4f, 0.9f, 1f);
    [Tooltip("Aviso debajo del titulo. Vacio = no se muestra.")]
    [SerializeField] private string hint = "¡COMIENZA!";

    [Header("Tiempos (segundos)")]
    [SerializeField] private float fadeIn = 0.6f;
    [SerializeField] private float hold = 2f;
    [SerializeField] private float fadeOut = 0.8f;

    private float startTime;

    private void Start()
    {
        startTime = Time.time;
    }

    private void OnGUI()
    {
        float t = Time.time - startTime;
        float total = fadeIn + hold + fadeOut;

        if (t >= total)
        {
            enabled = false;
            return;
        }

        float alpha;
        if (t < fadeIn)
            alpha = t / fadeIn;
        else if (t < fadeIn + hold)
            alpha = 1f;
        else
            alpha = 1f - (t - fadeIn - hold) / fadeOut;

        GUIStyle sub = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(Screen.height * 0.045f),
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        sub.normal.textColor = new Color(1f, 1f, 1f, alpha);

        GUIStyle main = new GUIStyle(sub)
        {
            fontSize = Mathf.RoundToInt(Screen.height * 0.09f)
        };
        main.normal.textColor = new Color(titleColor.r, titleColor.g, titleColor.b, alpha);

        float h = Screen.height;
        GUI.Label(new Rect(0, h * 0.33f, Screen.width, h * 0.08f), subtitle, sub);
        GUI.Label(new Rect(0, h * 0.40f, Screen.width, h * 0.14f), title, main);

        if (!string.IsNullOrEmpty(hint))
            GUI.Label(new Rect(0, h * 0.55f, Screen.width, h * 0.08f), hint, sub);
    }
}
