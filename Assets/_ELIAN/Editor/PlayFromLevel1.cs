using UnityEditor;
using UnityEditor.SceneManagement;

// Hace que el boton Play arranque siempre desde el Nivel 1, sin importar
// que escena este abierta en el editor (como en el juego final).
// Se puede desactivar desde el menu ELIAN para probar un nivel suelto.
[InitializeOnLoad]
public static class PlayFromLevel1
{
    private const string Level1Path = "Assets/_ELIAN/Levels/01_Education/Scenes/SCN_01_Education.unity";
    private const string MenuPath = "ELIAN/Jugar siempre desde el Nivel 1";
    private const string PrefKey = "ELIAN.PlayFromLevel1";

    static PlayFromLevel1()
    {
        // Esperamos a que el editor termine de cargar para leer la escena.
        EditorApplication.delayCall += Apply;
    }

    private static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    [MenuItem(MenuPath, priority = 0)]
    private static void Toggle()
    {
        Enabled = !Enabled;
        Apply();
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    private static void Apply()
    {
        EditorSceneManager.playModeStartScene = Enabled
            ? AssetDatabase.LoadAssetAtPath<SceneAsset>(Level1Path)
            : null;
    }
}
