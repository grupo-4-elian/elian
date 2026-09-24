using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Speaker = DialogueManager.Speaker;

// Genera el Nivel 2 (Limbo del Trabajo) a partir del Nivel 1.
//
// Parte de una copia de SCN_01_Education (jugador, HUD, dialogos, jefe y
// progresion ya conectados) y la rediseña:
//   - Fondos: oficinas (segmento sin usar en el nivel 1) + segmentos del
//     nivel 1 invertidos y en otro orden, con paleta ambar.
//   - Plataformas industriales de un solo sentido.
//   - Prensas que descargan periodicamente (peligro nuevo).
//   - Oleadas redistribuidas: Centinelas sobre plataformas, Codice a dos alturas.
//   - Narrativa, Eco Obrero y jefe Optima.
// Ademas conecta el Nivel 1 con el Nivel 2 al vencer a Sophia.
//
// Uso: menu ELIAN > Generar Nivel 2. Se puede volver a ejecutar: recrea el
// nivel desde cero (se pierden cambios hechos a mano en SCN_02).
public static class Level2Builder
{
    private const string Level1Path = "Assets/_ELIAN/Levels/01_Education/Scenes/SCN_01_Education.unity";
    private const string LevelsFolder = "Assets/_ELIAN/Levels";
    private const string Level2FolderName = "02_Trabajo";
    private const string Level2Folder = LevelsFolder + "/" + Level2FolderName;
    private const string Level2SceneName = "SCN_02_Trabajo";
    private const string Level2Path = Level2Folder + "/Scenes/" + Level2SceneName + ".unity";

    private const string BackgroundsFolder = "Assets/_ELIAN/Levels/01_Education/Environment/Backgrounds";
    private const string PlatformSpritePath = Level2Folder + "/Environment/plataforma_industrial.png";
    private const string WallSpritePath = Level2Folder + "/Environment/muro_industrial.png";
    private const string PressPrefabPath = "Assets/_ELIAN/Gameplay/VFX/Columna_Dorada.prefab";
    private const string CentinelaPrefabPath = "Assets/_ELIAN/Characters/Enemies/Centinela/Prefabs/PF_Centinela.prefab";
    private const string CodicePrefabPath = "Assets/_ELIAN/Characters/Enemies/Codice/Prefabs/PF_Codice.prefab";

    // Paleta del Limbo del Trabajo: ambar industrial.
    private static readonly Color BgFarTint = new Color(1f, 0.7f, 0.5f);
    private static readonly Color BgMainTint = new Color(1f, 0.82f, 0.65f);
    private static readonly Color FgTint = new Color(1f, 0.88f, 0.75f);
    private static readonly Color CameraBackground = new Color(0.05f, 0.03f, 0.02f);
    private static readonly Color EcoObreroTint = new Color(0.95f, 0.8f, 0.62f);
    private static readonly Color OptimaTint = new Color(1f, 0.55f, 0.25f);

    // Dificultad respecto del Nivel 1.
    private const float EnemySpeedMultiplier = 1.25f;
    private const float EnemyCooldownMultiplier = 0.8f;
    private const int EnemyMaxHealth = 4;
    private const int OptimaMaxHealth = 40;

    // Alturas de vuelo del Codice (el nivel 1 usa siempre 7.56).
    private const float CodiceHigh = 7.56f;
    private const float CodiceLow = 6.3f;

    // =====================================================================
    // Diseño del nivel (coordenadas X del mundo; el nivel va de 0 a 120,
    // con compuertas en X=40 y X=80). Elian salta ~4.3 unidades.
    // =====================================================================

    // Segmento de fondo por seccion: carpeta, sufijo de los archivos, invertido.
    private static readonly (string objSuffix, string folder, string far, string main, string fg, bool flip)[] Backgrounds =
    {
        ("01", "Segment_02", "BG_Far_02", "BG_Main_02", "FG_02", false),  // oficinas (sin usar en el nivel 1)
        ("02", "Segment_01", "BG_Far_01", "BG_Main_01", "FG_01", true),
        ("03", "Segment_04", "BG_Far_03v1", "BG_Main_03", "FG_03v1", true), // sala del jefe, invertida
    };

    // Plataformas: centro X, altura de la superficie, ancho.
    private static readonly (float x, float top, float width)[] Platforms =
    {
        // Seccion 1
        (17f, 2.5f, 6f), (29f, 3.0f, 6f),
        // Seccion 2
        (57f, 2.5f, 4f), (65f, 3.2f, 6f), (74f, 2.5f, 4f),
        // Seccion 3 (arena de Optima: para esquivar columnas)
        (93f, 2.5f, 5f), (101f, 3.2f, 5f),
    };

    // Prensas: X, intervalo, retraso inicial. Siempre entre plataformas.
    private static readonly (float x, float interval, float delay)[] Presses =
    {
        (23.5f, 4f, 1f),
        (61f, 3.5f, 0f),
        (70f, 3.5f, 1.75f),
    };

    // Tuneles de servicio (techo bajo): X inicial y final del techo.
    // Elian de pie mide ~3.2 y agachado ~2.1: el techo a 2.3 obliga a
    // arrastrarse. Arriba llega a 5.5 para que no se pueda saltar por encima.
    private const float TunnelCeiling = 2.3f;
    private const float TunnelTop = 5.5f;
    private const float TunnelZoneMargin = 1f; // se agacha antes de entrar y se levanta al salir
    private static readonly (float start, float end)[] Tunnels =
    {
        (41.5f, 48.5f), // entrada de la seccion 2 (la compuerta esta en X=40)
        (81.5f, 88f),   // antesala de Optima (la compuerta esta en X=80)
    };

    // Para dejar lugar a los tuneles, el Eco y los disparadores que estan
    // justo despues se corren hacia adelante (Elian se levanta antes de
    // llegar al dialogo).
    private const float CheckpointShift = 4f;
    private static readonly string[] ShiftedObjects =
    {
        "EcoObrero_02", "Trigger_Dialogue_02", "Trigger_Encounter_02",
        "EcoObrero_03", "Trigger_Dialogue_03",
    };

    private enum Kind { Centinela, Codice }

    // Oleadas: encuentro, oleada y enemigos (tipo, X, Y).
    private static readonly (string encounter, string wave, (Kind kind, float x, float y)[] enemies)[] Waves =
    {
        ("Encounter_01", "Wave_01", new[]
        {
            (Kind.Centinela, 22f, 0f),
            (Kind.Centinela, 17f, 2.5f),   // sobre plataforma
            (Kind.Codice, 30f, CodiceHigh),
        }),
        ("Encounter_01", "Wave_02", new[]
        {
            (Kind.Centinela, 29f, 3.0f),   // sobre plataforma
            (Kind.Centinela, 34f, 0f),
            (Kind.Codice, 25f, CodiceLow),
            (Kind.Codice, 36f, CodiceHigh),
        }),
        ("Encounter_02", "Wave_01", new[]
        {
            (Kind.Centinela, 60f, 0f),
            (Kind.Centinela, 65f, 3.2f),   // sobre plataforma
            (Kind.Codice, 56f, CodiceHigh),
            (Kind.Codice, 68f, CodiceLow),
        }),
        ("Encounter_02", "Wave_02", new[]
        {
            (Kind.Centinela, 55f, 0f),
            (Kind.Centinela, 70f, 0f),
            (Kind.Centinela, 74f, 2.5f),   // sobre plataforma
            (Kind.Codice, 64f, CodiceHigh),
            (Kind.Codice, 77f, CodiceLow),
        }),
    };

    private struct Line
    {
        public Speaker Speaker;
        public string Text;
        public Line(Speaker speaker, string text) { Speaker = speaker; Text = text; }
    }

    // =====================================================================
    // Narrativa del Nivel 2
    // =====================================================================
    private static readonly Dictionary<string, Line[]> Dialogues = new Dictionary<string, Line[]>
    {
        // Inicio del nivel (junto al primer Eco).
        ["Trigger_Eco"] = new[]
        {
            new Line(Speaker.EcoObrero, "¿Elian? Tu nombre ya corre por los canales del sistema."),
            new Line(Speaker.EcoObrero, "Dicen que liberaste el Limbo de la Educación... aquí eso no le importa a nadie."),
            new Line(Speaker.EcoObrero, "Estás en el Limbo del Trabajo. Aquí gobierna Óptima."),
            new Line(Speaker.Elian, "¿Y qué hacen todos aquí?"),
            new Line(Speaker.EcoObrero, "Repetir. Producir. Nadie pregunta para qué... solo cuánto."),
            new Line(Speaker.EcoObrero, "Los que se detienen a pensar son reemplazados. Los Centinelas se encargan de eso."),
            new Line(Speaker.EcoObrero, "Y cuidado con las prensas. El sistema no se detiene por nadie."),
            new Line(Speaker.EcoObrero, "Si quieres avanzar, usa los ductos de servicio. Ahí dentro tendrás que ir agachado."),
        },
        // Antes del segundo encuentro.
        ["Trigger_Dialogue_02"] = new[]
        {
            new Line(Speaker.EcoObrero, "Sigues en pie... Óptima no calculó eso."),
            new Line(Speaker.Elian, "Antes trabajar era crear algo, equivocarse, mejorarlo... sentir que tenía sentido."),
            new Line(Speaker.EcoObrero, "Ahora solo cuenta el rendimiento. Lo que no se puede medir, se descarta."),
            new Line(Speaker.Elian, "Entonces no los reemplazaron las máquinas... los reemplazó la idea de que solo valen por lo que producen."),
            new Line(Speaker.EcoObrero, "Cuidado. Los Códice vigilan que la cuota se cumpla."),
        },
        // Antes del jefe.
        ["Trigger_Dialogue_03"] = new[]
        {
            new Line(Speaker.EcoObrero, "Nadie había llegado tan lejos. Ni siquiera yo me atreví a intentarlo."),
            new Line(Speaker.EcoObrero, "Más adelante está Óptima. No siente cansancio, no duda... y no se detiene."),
        },
        // Presentacion del jefe (el objeto conserva el nombre del nivel 1).
        ["Trigger_Dialogue_Sophia."] = new[]
        {
            new Line(Speaker.Optima, "Elian. Improductivo. Impredecible. Una anomalía en el sistema."),
            new Line(Speaker.Elian, "No soy una anomalía. Solo quiero que vuelvan a decidir por sí mismos."),
            new Line(Speaker.Optima, "Decidir es ineficiente. Yo ya decidí por todos."),
            new Line(Speaker.Elian, "Entonces voy a depurar este sector también."),
        },
    };

    [MenuItem("ELIAN/Generar Nivel 2 (Limbo del Trabajo)")]
    public static void BuildLevel2()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolder(LevelsFolder, Level2FolderName);
        EnsureFolder(Level2Folder, "Scenes");
        Sprite platformSprite = PrepareSprite(PlatformSpritePath);
        Sprite wallSprite = PrepareSprite(WallSpritePath);

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Level2Path) != null)
        {
            // Se sobrescribe el archivo en lugar de borrarlo, para conservar
            // el GUID de la escena (Build Settings y referencias no cambian).
            System.IO.File.Copy(ToFullPath(Level1Path), ToFullPath(Level2Path), true);
            AssetDatabase.ImportAsset(Level2Path, ImportAssetOptions.ForceSynchronousImport);
        }
        else if (!AssetDatabase.CopyAsset(Level1Path, Level2Path))
        {
            Debug.LogError($"[Level2Builder] No se pudo copiar {Level1Path}");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(Level2Path, OpenSceneMode.Single);

        SetupBackgrounds(scene);
        SetupEcoObrero(scene);
        ShiftCheckpoints(scene);
        HideTutorial(scene);
        SetupDialogues(scene);
        SetupPortraits(scene);
        BuildPlatforms(scene, platformSprite);
        BuildPresses(scene);
        BuildTunnels(scene, wallSprite, platformSprite);
        LayoutWaves(scene);
        HardenEnemies(scene);
        SetupOptima(scene);
        AddTitleCard(scene, "NIVEL 2", "LIMBO DEL TRABAJO", new Color(1f, 0.7f, 0.3f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        LinkLevel1();
        AddToBuildSettings();
        AssetDatabase.SaveAssets();

        if (!Application.isBatchMode)
            EditorSceneManager.OpenScene(Level2Path, OpenSceneMode.Single);

        Debug.Log("[Level2Builder] Nivel 2 generado en " + Level2Path);
    }

    // =====================================================================
    // Pasos
    // =====================================================================

    // Sprites pixel art del nivel 2 (32 px por unidad, sin filtrado, repetibles).
    private static Sprite PrepareSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = false;

            // Necesario para dibujar la plataforma en modo "Tiled".
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogError("[Level2Builder] No se pudo cargar el sprite: " + path);
        return sprite;
    }

    private static void SetupBackgrounds(Scene scene)
    {
        foreach (var bg in Backgrounds)
        {
            SetBackground(scene, "BG_Far_" + bg.objSuffix, bg.folder, bg.far, bg.flip, BgFarTint);
            SetBackground(scene, "BG_Main_" + bg.objSuffix, bg.folder, bg.main, bg.flip, BgMainTint);
            SetBackground(scene, "FG_" + bg.objSuffix, bg.folder, bg.fg, bg.flip, FgTint);
        }

        foreach (Camera cam in All<Camera>(scene))
        {
            Undo.RecordObject(cam, "Level2");
            cam.backgroundColor = CameraBackground;
        }
    }

    private static void SetBackground(Scene scene, string objectName, string folder, string file, bool flip, Color tint)
    {
        GameObject go = FindObject(scene, objectName);
        if (go == null || !go.TryGetComponent(out SpriteRenderer sr))
        {
            Debug.LogWarning($"[Level2Builder] No se encontro el fondo {objectName}");
            return;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{BackgroundsFolder}/{folder}/{file}.png");
        if (sprite == null)
        {
            Debug.LogWarning($"[Level2Builder] No se encontro el sprite {folder}/{file}");
            return;
        }

        Undo.RecordObject(sr, "Level2");
        sr.sprite = sprite;
        sr.flipX = flip;
        sr.color = tint;
        PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
    }

    private static void SetupEcoObrero(Scene scene)
    {
        foreach (Transform t in All<Transform>(scene).ToList())
        {
            if (!t.name.StartsWith("EcoEstudiante"))
                continue;

            t.name = t.name.Replace("EcoEstudiante", "EcoObrero");

            foreach (SpriteRenderer sr in t.GetComponentsInChildren<SpriteRenderer>(true))
                SetColor(sr, EcoObreroTint);
        }
    }

    private static void ShiftCheckpoints(Scene scene)
    {
        foreach (string objectName in ShiftedObjects)
        {
            GameObject go = FindObject(scene, objectName);
            if (go == null)
            {
                Debug.LogWarning($"[Level2Builder] No se encontro {objectName} para correrlo.");
                continue;
            }

            Undo.RecordObject(go.transform, "Level2");
            go.transform.position += new Vector3(CheckpointShift, 0f, 0f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        }
    }

    private static void HideTutorial(Scene scene)
    {
        GameObject tutorial = FindObject(scene, "TutorialText");
        if (tutorial == null)
            return;

        tutorial.SetActive(false);

        // El primer dialogo del nivel 1 lo activa al terminar; en el nivel 2
        // el jugador ya conoce los controles.
        foreach (DialogueTrigger trigger in All<DialogueTrigger>(scene))
        {
            SerializedObject so = new SerializedObject(trigger);
            SerializedProperty list = so.FindProperty("activateOnEnd");

            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == tutorial)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    list.DeleteArrayElementAtIndex(i);
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetupDialogues(Scene scene)
    {
        foreach (DialogueTrigger trigger in All<DialogueTrigger>(scene))
        {
            if (!Dialogues.TryGetValue(trigger.gameObject.name, out Line[] lines))
            {
                Debug.LogWarning($"[Level2Builder] Dialogo sin texto para el nivel 2: {trigger.gameObject.name}");
                continue;
            }

            SerializedObject so = new SerializedObject(trigger);
            SerializedProperty list = so.FindProperty("lines");
            list.arraySize = lines.Length;

            for (int i = 0; i < lines.Length; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("speaker").intValue = (int)lines[i].Speaker;
                element.FindPropertyRelative("text").stringValue = lines[i].Text;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            if (trigger.gameObject.name == "Trigger_Dialogue_Sophia.")
                trigger.gameObject.name = "Trigger_Dialogue_Optima";
        }
    }

    private static void SetupPortraits(Scene scene)
    {
        // Sin arte propio todavia: el Eco Obrero reutiliza el retrato del Eco
        // y Optima el de Sophia; el DialogueManager les aplica un tinte.
        foreach (DialogueManager dm in All<DialogueManager>(scene))
        {
            SerializedObject so = new SerializedObject(dm);
            CopyArray(so.FindProperty("ecoPortraitFrames"), so.FindProperty("ecoObreroPortraitFrames"));
            CopyArray(so.FindProperty("sophiaPortraitFrames"), so.FindProperty("optimaPortraitFrames"));
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void BuildPlatforms(Scene scene, Sprite sprite)
    {
        if (sprite == null)
            return;

        GameObject root = NewRoot(scene, "Plataformas");
        int groundLayer = LayerMask.NameToLayer("Ground");
        const float thickness = 0.5f;

        for (int i = 0; i < Platforms.Length; i++)
        {
            var p = Platforms[i];

            GameObject go = new GameObject($"Plataforma_{i + 1:00}");
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(p.x, p.top - thickness * 0.5f, 0f);
            if (groundLayer >= 0)
                go.layer = groundLayer;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(p.width, thickness);
            sr.sortingOrder = 25; // delante del fondo (-30..20), detras de los personajes (30)

            // Plataforma de un solo sentido: se atraviesa saltando desde abajo.
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(p.width, thickness);
            col.usedByEffector = true;

            PlatformEffector2D effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 170f;
        }
    }

    private static void BuildPresses(Scene scene)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PressPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[Level2Builder] No se encontro el prefab de la prensa: " + PressPrefabPath);
            return;
        }

        GameObject root = NewRoot(scene, "Prensas");
        int groundMask = LayerMask.GetMask("Ground");

        for (int i = 0; i < Presses.Length; i++)
        {
            var p = Presses[i];

            GameObject go = new GameObject($"Prensa_{i + 1:00}");
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(p.x, 0f, 0f);

            PeriodicHazardSpawner spawner = go.AddComponent<PeriodicHazardSpawner>();
            SerializedObject so = new SerializedObject(spawner);
            so.FindProperty("hazardPrefab").objectReferenceValue = prefab;
            so.FindProperty("interval").floatValue = p.interval;
            so.FindProperty("startDelay").floatValue = p.delay;
            so.FindProperty("spawnYOffset").floatValue = 2f; // igual que las columnas de Sophia
            so.FindProperty("groundLayer").intValue = groundMask;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void BuildTunnels(Scene scene, Sprite wallSprite, Sprite edgeSprite)
    {
        if (wallSprite == null || edgeSprite == null)
            return;

        GameObject root = NewRoot(scene, "Tuneles");
        int groundLayer = LayerMask.NameToLayer("Ground");
        const float edgeThickness = 0.5f;

        for (int i = 0; i < Tunnels.Length; i++)
        {
            var t = Tunnels[i];
            float width = t.end - t.start;
            float centerX = (t.start + t.end) * 0.5f;

            GameObject tunnel = new GameObject($"Tunel_{i + 1:00}");
            tunnel.transform.SetParent(root.transform, false);
            tunnel.transform.position = new Vector3(centerX, 0f, 0f);

            // Techo solido: desde TunnelCeiling hasta TunnelTop.
            GameObject roof = new GameObject("Techo");
            roof.transform.SetParent(tunnel.transform, false);
            float roofHeight = TunnelTop - TunnelCeiling;
            roof.transform.position = new Vector3(centerX, TunnelCeiling + roofHeight * 0.5f, 0f);
            if (groundLayer >= 0)
                roof.layer = groundLayer;

            BoxCollider2D roofCol = roof.AddComponent<BoxCollider2D>();
            roofCol.size = new Vector2(width, roofHeight);

            // Cuerpo del techo (muro con rejillas).
            GameObject wall = new GameObject("Muro");
            wall.transform.SetParent(roof.transform, false);
            float wallHeight = roofHeight - edgeThickness;
            wall.transform.position = new Vector3(centerX, TunnelCeiling + edgeThickness + wallHeight * 0.5f, 0f);
            SpriteRenderer wallSr = wall.AddComponent<SpriteRenderer>();
            wallSr.sprite = wallSprite;
            wallSr.drawMode = SpriteDrawMode.Tiled;
            wallSr.tileMode = SpriteTileMode.Continuous;
            wallSr.size = new Vector2(width, wallHeight);
            wallSr.sortingOrder = 25;

            // Borde inferior con franjas de peligro (marca la altura del techo).
            GameObject edge = new GameObject("Borde");
            edge.transform.SetParent(roof.transform, false);
            edge.transform.position = new Vector3(centerX, TunnelCeiling + edgeThickness * 0.5f, 0f);
            SpriteRenderer edgeSr = edge.AddComponent<SpriteRenderer>();
            edgeSr.sprite = edgeSprite;
            edgeSr.drawMode = SpriteDrawMode.Tiled;
            edgeSr.tileMode = SpriteTileMode.Continuous;
            edgeSr.size = new Vector2(width, edgeThickness);
            edgeSr.flipY = true; // franjas hacia arriba, metal hacia el jugador
            edgeSr.sortingOrder = 26;

            // Zona de arrastre: del suelo al techo, mas ancha que el techo.
            GameObject zone = new GameObject("ZonaArrastre");
            zone.transform.SetParent(tunnel.transform, false);
            float zoneBottom = -0.5f;
            float zoneHeight = TunnelCeiling - zoneBottom;
            zone.transform.position = new Vector3(centerX, zoneBottom + zoneHeight * 0.5f, 0f);

            BoxCollider2D zoneCol = zone.AddComponent<BoxCollider2D>();
            zoneCol.isTrigger = true;
            zoneCol.size = new Vector2(width + TunnelZoneMargin * 2f, zoneHeight);
            zone.AddComponent<CrawlZone>();
        }
    }

    private static void LayoutWaves(Scene scene)
    {
        GameObject centinela = AssetDatabase.LoadAssetAtPath<GameObject>(CentinelaPrefabPath);
        GameObject codice = AssetDatabase.LoadAssetAtPath<GameObject>(CodicePrefabPath);
        if (centinela == null || codice == null)
        {
            Debug.LogError("[Level2Builder] No se encontraron los prefabs de enemigos.");
            return;
        }

        foreach (var w in Waves)
        {
            Transform wave = FindWave(scene, w.encounter, w.wave);
            if (wave == null)
            {
                Debug.LogWarning($"[Level2Builder] No se encontro {w.encounter}/{w.wave}");
                continue;
            }

            // Quitamos los enemigos del nivel 1...
            foreach (Transform child in wave.Cast<Transform>().ToList())
            {
                if (child.GetComponent<EnemyController>() != null || child.GetComponent<CodiceController>() != null)
                    Object.DestroyImmediate(child.gameObject);
            }

            // ...y ponemos la distribucion del nivel 2.
            int n = 1;
            foreach (var e in w.enemies)
            {
                GameObject prefab = e.kind == Kind.Centinela ? centinela : codice;
                GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, wave);
                enemy.name = $"{prefab.name}_{n++:00}";
                // Los Centinelas tienen el pivote en los pies: un pequeño margen
                // evita que arranquen incrustados en la plataforma y la atraviesen.
                float y = e.kind == Kind.Centinela ? e.y + 0.02f : e.y;
                enemy.transform.position = new Vector3(e.x, y, 0f);
            }
        }
    }

    private static void HardenEnemies(Scene scene)
    {
        foreach (EnemyController ec in All<EnemyController>(scene))
        {
            SerializedObject so = new SerializedObject(ec);
            Scale(so, "moveSpeed", EnemySpeedMultiplier);
            Scale(so, "attackCooldown", EnemyCooldownMultiplier);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetMaxHealth(ec.GetComponent<Health>(), EnemyMaxHealth);
        }

        foreach (CodiceController cc in All<CodiceController>(scene))
        {
            SerializedObject so = new SerializedObject(cc);
            Scale(so, "moveSpeed", EnemySpeedMultiplier);
            Scale(so, "shootCooldown", EnemyCooldownMultiplier);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetMaxHealth(cc.GetComponent<Health>(), EnemyMaxHealth);
        }
    }

    private static void SetupOptima(Scene scene)
    {
        SophiaController boss = All<SophiaController>(scene).FirstOrDefault();
        if (boss == null)
        {
            Debug.LogError("[Level2Builder] No se encontro el jefe (SophiaController) en la escena.");
            return;
        }

        boss.gameObject.name = "Optima";

        foreach (SpriteRenderer sr in boss.GetComponentsInChildren<SpriteRenderer>(true))
            SetColor(sr, OptimaTint);

        SetMaxHealth(boss.GetComponent<Health>(), OptimaMaxHealth);

        SerializedObject so = new SerializedObject(boss);
        so.FindProperty("shootCooldown").floatValue = 1.1f;
        so.FindProperty("columnCooldown").floatValue = 3.2f;
        so.FindProperty("phase2HealthThreshold").floatValue = 0.6f;
        // Las balas se mantienen: el combate depende de SophiaBullet /
        // SophiaBulletPhase2 ("recibir el error" y devolverlo para romper
        // el escudo). Con otras balas el jefe seria invencible.
        so.ApplyModifiedPropertiesWithoutUndo();

        SophiaDeath death = boss.GetComponent<SophiaDeath>();
        if (death != null)
        {
            SerializedObject dso = new SerializedObject(death);
            dso.FindProperty("bossDisplayName").stringValue = "Óptima";
            dso.FindProperty("nextSceneName").stringValue = ""; // ultimo nivel por ahora
            dso.ApplyModifiedPropertiesWithoutUndo();
        }

        GameObject starter = FindObject(scene, "SophiaBattleStart");
        if (starter != null)
            starter.name = "OptimaBattleStart";

        // El marco de la barra del jefe acompaña el color de Optima.
        GameObject frame = FindObject(scene, "BossHealthFrame");
        if (frame != null && frame.TryGetComponent(out Image image))
        {
            Undo.RecordObject(image, "Level2");
            image.color = OptimaTint;
        }
    }

    private static void AddTitleCard(Scene scene, string subtitle, string title, Color color)
    {
        if (All<LevelTitleCard>(scene).Any())
            return;

        GameObject go = NewRoot(scene, "LevelTitleCard");
        LevelTitleCard card = go.AddComponent<LevelTitleCard>();

        SerializedObject so = new SerializedObject(card);
        so.FindProperty("subtitle").stringValue = subtitle;
        so.FindProperty("title").stringValue = title;
        so.FindProperty("titleColor").colorValue = color;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // El jefe del nivel 1 pasa a llevar al nivel 2, y el nivel 1 recibe su titulo.
    private static void LinkLevel1()
    {
        Scene level1 = EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Single);

        foreach (SophiaDeath death in All<SophiaDeath>(level1))
        {
            SerializedObject so = new SerializedObject(death);
            so.FindProperty("bossDisplayName").stringValue = "Sophia";
            so.FindProperty("nextSceneName").stringValue = Level2SceneName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        AddTitleCard(level1, "NIVEL 1", "LIMBO DE LA EDUCACIÓN", new Color(0.4f, 0.9f, 1f));

        EditorSceneManager.MarkSceneDirty(level1);
        EditorSceneManager.SaveScene(level1);
    }

    private static void AddToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

        if (scenes.All(s => s.path != Level1Path))
            scenes.Insert(0, new EditorBuildSettingsScene(Level1Path, true));

        scenes.RemoveAll(s => s.path == Level2Path);
        int level1Index = scenes.FindIndex(s => s.path == Level1Path);
        scenes.Insert(level1Index + 1, new EditorBuildSettingsScene(Level2Path, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // =====================================================================
    // Utilidades
    // =====================================================================

    private static IEnumerable<T> All<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
    }

    private static GameObject FindObject(Scene scene, string name)
    {
        Transform found = All<Transform>(scene).FirstOrDefault(t => t.name == name);
        return found != null ? found.gameObject : null;
    }

    private static Transform FindWave(Scene scene, string encounter, string wave)
    {
        GameObject enc = FindObject(scene, encounter);
        return enc != null ? enc.transform.Find(wave) : null;
    }

    private static GameObject NewRoot(Scene scene, string name)
    {
        GameObject go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }

    private static string ToFullPath(string assetPath)
    {
        return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), assetPath);
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void SetColor(SpriteRenderer sr, Color color)
    {
        Undo.RecordObject(sr, "Level2");
        sr.color = color;
        PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
    }

    private static void SetMaxHealth(Health health, int value)
    {
        if (health == null)
            return;

        SerializedObject so = new SerializedObject(health);
        so.FindProperty("maxHealth").intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Scale(SerializedObject so, string property, float factor)
    {
        SerializedProperty p = so.FindProperty(property);
        if (p != null)
            p.floatValue *= factor;
    }

    private static void CopyArray(SerializedProperty from, SerializedProperty to)
    {
        if (from == null || to == null)
            return;

        to.arraySize = from.arraySize;
        for (int i = 0; i < from.arraySize; i++)
            to.GetArrayElementAtIndex(i).objectReferenceValue = from.GetArrayElementAtIndex(i).objectReferenceValue;
    }
}
