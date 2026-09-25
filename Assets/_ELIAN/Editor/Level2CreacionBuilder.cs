using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using Speaker = DialogueManager.Speaker;

// Genera el Nivel 2 "CREACION" (Narrativa ELIAN) con el arte del grupo.
//
// 1. Importa y recorta los sprites (manifiesto sprites_nivel2.json).
// 2. Crea los prefabs: Ensamblador, Corrector, Dron de soldadura, piezas,
//    artefacto, replica, golpe de brazo y chispa.
// 3. Arma SCN_02_Creacion a partir del Nivel 1 (jugador, HUD, dialogos y
//    progresion) con fondo de fabrica, Tadeo, oleadas nuevas y la arena de
//    FABER; y conecta el Nivel 1 con este nivel.
//
// Uso: ELIAN > Generar Nivel 2 (Creación). Se puede volver a ejecutar.
public static class Level2CreacionBuilder
{
    private const string Level1Path = "Assets/_ELIAN/Levels/01_Education/Scenes/SCN_01_Education.unity";
    private const string L2 = "Assets/_ELIAN/Levels/02_Creacion";
    private const string SceneName = "SCN_02_Creacion";
    private const string ScenePath = L2 + "/Scenes/" + SceneName + ".unity";
    private const string PrefabDir = L2 + "/Prefabs";
    private const string ManifestPath = L2 + "/sprites_nivel2.json";
    private const string Props = L2 + "/Props";
    private const string Env = L2 + "/Environment";
    private const string Chars = "Assets/_ELIAN/Characters";
    private const string Portraits = "Assets/_ELIAN/UI/Dialogue/Portraits";
    private const string Hud = "Assets/_ELIAN/UI/HUD";
    private const string MusicPath = "Assets/_ELIAN/Audio/Music/musica_nivel2_fabrica.ogg";

    private const int PPU = 32;
    private const float BackgroundPPU = 44f;       // el fondo cubre la vista completa
    private const int BackgroundFloorPixel = 392;  // fila del suelo en el fondo (de arriba)
    private const int BackgroundHeight = 417;

    // ------------------------------------------------------------------ narrativa
    private struct Line
    {
        public Speaker S; public string T;
        public Line(Speaker s, string t) { S = s; T = t; }
    }

    private static readonly Dictionary<string, Line[]> Dialogues = new Dictionary<string, Line[]>
    {
        ["Trigger_Eco"] = new[]
        {
            new Line(Speaker.Tadeo, "¿Eres una pieza nueva? No apareces en ningún registro."),
            new Line(Speaker.Elian, "No soy una pieza."),
            new Line(Speaker.Tadeo, "Todo lo que hay aquí es una pieza. Esto es el complejo de Creación, y todo lo diseña Faber."),
            new Line(Speaker.Tadeo, "Yo solo mantengo las máquinas. Si algo falla, busco el procedimiento y lo reemplazo."),
            new Line(Speaker.Elian, "¿Y si el fallo no aparece en ningún procedimiento?"),
            new Line(Speaker.Tadeo, "...Entonces se descarta. Siempre se descarta."),
            new Line(Speaker.Tadeo, "Los Ensambladores ya te detectaron. Para ellos eres una pieza fuera de lugar."),
        },
        ["Trigger_Dialogue_02"] = new[]
        {
            new Line(Speaker.Tadeo, "Desarmaste a los Ensambladores. Eso no está en el manual."),
            new Line(Speaker.Elian, "¿Qué están construyendo?"),
            new Line(Speaker.Tadeo, "No lo sé. Nadie lo sabe. Siguen las instrucciones y la pieza sale perfecta."),
            new Line(Speaker.Elian, "Antes crear era imaginar algo que no existía... y fallar muchas veces hasta lograrlo."),
            new Line(Speaker.Tadeo, "Ahora nada falla. Faber lo diseña todo antes de que alguien llegue a pensarlo."),
            new Line(Speaker.Elian, "Entonces no dejaron de producir. Dejaron de crear."),
            new Line(Speaker.Tadeo, "Cuidado. Los Correctores eliminan todo lo que no coincide con el plano."),
        },
        ["Trigger_Dialogue_03"] = new[]
        {
            new Line(Speaker.Tadeo, "Los Correctores nunca se equivocan... y aun así pasaste."),
            new Line(Speaker.Tadeo, "Más adelante está el núcleo de Faber. No se le puede dañar: todo lo que llega a él ya está previsto."),
            new Line(Speaker.Tadeo, "Lo único que Faber no revisa es el descarte. Lo que cae de la tolva ya no le importa."),
            new Line(Speaker.Elian, "Entonces ahí está lo que no previó."),
            new Line(Speaker.Tadeo, "Si juntas piezas (agáchate junto a ellas) o las empujas a la cinta... no sé qué pasaría. No hay procedimiento para eso."),
        },
        ["Trigger_Dialogue_Sophia."] = new[]
        {
            new Line(Speaker.Faber, "Unidad no catalogada. Origen desconocido. Función: ninguna."),
            new Line(Speaker.Elian, "No necesito una función para existir."),
            new Line(Speaker.Faber, "Todo lo necesario ya fue diseñado. Tú eres un excedente."),
            new Line(Speaker.Elian, "Tal vez. Pero con los excedentes también se puede crear."),
        },
    };

    private static readonly Line[] ClosingLines =
    {
        new Line(Speaker.Faber, "Sin especificación... sin plano... no se puede fabricar."),
        new Line(Speaker.Elian, "No lo fabriqué. Lo creé."),
        new Line(Speaker.Tadeo, "La línea se detuvo... y no hay procedimiento para esto."),
        new Line(Speaker.Tadeo, "Pero creo que puedo repararla. A mi manera."),
        new Line(Speaker.Elian, "Eso es justo lo que Faber nunca pudo hacer."),
    };

    // ------------------------------------------------------------------ disposicion
    private enum Kind { Ensamblador, Corrector, Dron }

    private static readonly (string enc, string wave, float minX, float maxX, (Kind k, float x, float y)[] list)[] Waves =
    {
        ("Encounter_01", "Wave_01", 0f, 40f, new[] { (Kind.Ensamblador, 24f, 0f), (Kind.Ensamblador, 29f, 0f) }),
        ("Encounter_01", "Wave_02", 0f, 40f, new[] { (Kind.Ensamblador, 27f, 0f), (Kind.Ensamblador, 32f, 0f), (Kind.Ensamblador, 36f, 0f) }),
        ("Encounter_02", "Wave_01", 40f, 80f, new[] { (Kind.Ensamblador, 60f, 0f), (Kind.Ensamblador, 64f, 0f), (Kind.Corrector, 70f, 0f) }),
        ("Encounter_02", "Wave_02", 40f, 80f, new[] { (Kind.Corrector, 63f, 0f), (Kind.Corrector, 73f, 0f), (Kind.Dron, 68f, 5.8f) }),
    };

    // ==================================================================
    [MenuItem("ELIAN/Generar Nivel 2 (Creación)")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolder(L2, "Scenes");
        EnsureFolder(L2, "Prefabs");

        Dictionary<string, Sprite[]> strips = ImportStrips();
        ImportSingles();
        ImportMusic();

        Prefabs pf = BuildPrefabs(strips);

        CopyLevel1();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SetupScene(scene, strips, pf);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        LinkLevel1();
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();

        if (!Application.isBatchMode)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Debug.Log("[Level2Creacion] Nivel 2 generado en " + ScenePath);
    }

    // ================================================================== sprites
    [Serializable] private class ManifestEntry { public string path; public int cellWidth; public int cellHeight; public int frames; public float fps; public bool loop; public string pivot; }
    [Serializable] private class Manifest { public ManifestEntry[] sprites; }

    private static Dictionary<string, Sprite[]> ImportStrips()
    {
        var result = new Dictionary<string, Sprite[]>();
        Manifest m = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
        foreach (ManifestEntry e in m.sprites)
        {
            Vector2 pivot = e.pivot == "center" ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f);
            result[Path.GetFileNameWithoutExtension(e.path)] = SliceStrip(e.path, e.cellWidth, e.cellHeight, e.frames, pivot, PPU);
        }

        result["CINTA"] = SliceStrip(Props + "/cinta_transportadora.png", 64, 16, 4, new Vector2(0.5f, 0.5f), PPU);
        result["TOLVA"] = SliceStrip(Props + "/tolva_descarte.png", 96, 128, 2, new Vector2(0.5f, 0.5f), PPU);
        result["RETRATO_TADEO"] = SliceStrip(Portraits + "/retrato_tadeo.png", 96, 96, 4, new Vector2(0.5f, 0.5f), PPU);
        result["RETRATO_FABER"] = SliceStrip(Portraits + "/retrato_faber.png", 96, 96, 4, new Vector2(0.5f, 0.5f), PPU);
        return result;
    }

    private static Sprite[] SliceStrip(string path, int cw, int ch, int frames, Vector2 pivot, float ppu)
    {
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if (ti == null)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            ti = (TextureImporter)AssetImporter.GetAtPath(path);
        }

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();

        string baseName = Path.GetFileNameWithoutExtension(path);
        SpriteRect[] existing = dp.GetSpriteRects();
        var rects = new List<SpriteRect>();
        for (int i = 0; i < frames; i++)
        {
            string name = baseName + "_" + i;
            SpriteRect prev = existing.FirstOrDefault(r => r.name == name);
            rects.Add(new SpriteRect
            {
                name = name,
                rect = new Rect(i * cw, 0, cw, ch),
                alignment = SpriteAlignment.Custom,
                pivot = pivot,
                spriteID = prev != null ? prev.spriteID : GUID.Generate(),
            });
        }
        dp.SetSpriteRects(rects.ToArray());

        var ids = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (ids != null)
            ids.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));

        dp.Apply();
        ti.SaveAndReimport();

        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
            .ToArray();
    }

    private static void ImportSingles()
    {
        foreach (string f in new[] { "pieza_engranaje", "pieza_placa", "artefacto_improvisado", "seguro_tolva", "chispa_soldadura" })
            SetSingle(Props + "/" + f + ".png", PPU, new Vector2(0.5f, 0.5f), false);
        SetSingle(Props + "/marca_golpe.png", PPU, new Vector2(0.5f, 0f), true);
        // compuertas: misma densidad de pixeles que el fondo para integrarse
        SetSingle(Props + "/compuerta_marco.png", BackgroundPPU, new Vector2(0.5f, 0f), false);
        SetSingle(Props + "/compuerta_persiana.png", BackgroundPPU, new Vector2(0.5f, 0f), false);
        SetSingle(Props + "/compuerta_luz_roja.png", BackgroundPPU, new Vector2(0.5f, 0.5f), false);
        SetSingle(Props + "/compuerta_luz_verde.png", BackgroundPPU, new Vector2(0.5f, 0.5f), false);
        SetSingle(Env + "/BG_Fabrica.png", BackgroundPPU,
                  new Vector2(0.5f, (BackgroundHeight - BackgroundFloorPixel) / (float)BackgroundHeight), false);
        SetSingle(Env + "/Suelo_Fabrica.png", BackgroundPPU, new Vector2(0.5f, 1f), true);
    }

    private static void ImportMusic()
    {
        AssetDatabase.ImportAsset(MusicPath, ImportAssetOptions.ForceSynchronousImport);
        var ai = AssetImporter.GetAtPath(MusicPath) as AudioImporter;
        if (ai == null) { Debug.LogWarning("[Level2Creacion] No se encontro la musica " + MusicPath); return; }
        AudioImporterSampleSettings st = ai.defaultSampleSettings;
        st.loadType = AudioClipLoadType.Streaming;
        st.compressionFormat = AudioCompressionFormat.Vorbis;
        st.quality = 0.7f;
        ai.defaultSampleSettings = st;
        ai.loadInBackground = true;
        ai.SaveAndReimport();
    }

    private static void SetSingle(string path, float ppu, Vector2 pivot, bool tiled)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.Custom;
        st.spritePivot = pivot;
        st.spriteMeshType = tiled ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
    }

    private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    // ================================================================== prefabs
    private class Prefabs
    {
        public GameObject Ensamblador, Corrector, Dron, Chispa, Engranaje, Placa, Artefacto, Replica, ArmSlam;
    }

    private static Prefabs BuildPrefabs(Dictionary<string, Sprite[]> s)
    {
        var p = new Prefabs();
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int enemyShotLayer = LayerMask.NameToLayer("EnemyProjectile");

        // --- chispa del dron (proyectil) ---
        {
            GameObject go = new GameObject("ChispaSoldadura");
            if (enemyShotLayer >= 0) go.layer = enemyShotLayer;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = LoadSprite(Props + "/chispa_soldadura.png"); sr.sortingOrder = 32;
            var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.15f;
            var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic;
            var eb = go.AddComponent<EnemyBullet>();
            SetField(eb, "speed", 7f); SetField(eb, "lifeTime", 4f);
            p.Chispa = SavePrefab(go, "ChispaSoldadura");
        }

        p.Ensamblador = MakeEnemy("Ensamblador", typeof(EnsambladorController), 3, false, new Vector2(1.5f, 2.2f), enemyLayer,
            Clip("idle", s["ENSAMBLADOR_IDLE"], 8, true), Clip("walk", s["ENSAMBLADOR_WALK"], 10, true),
            Clip("attack", s["ENSAMBLADOR_ATTACK"], 12, false), Clip("spawn", s["ENSAMBLADOR_SPAWN"], 8, false),
            Clip("dead", s["ENSAMBLADOR_DEAD"], 10, false));

        p.Corrector = MakeEnemy("Corrector", typeof(CorrectorController), 5, false, new Vector2(1.2f, 3f), enemyLayer,
            Clip("idle", s["CORRECTOR_IDLE"], 8, true), Clip("run", s["CORRECTOR_RUN"], 14, true),
            Clip("scan", s["CORRECTOR_SCAN"], 12, false), Clip("attack", s["CORRECTOR_ATTACK"], 14, false),
            Clip("dead", s["CORRECTOR_DEAD"], 10, false));

        {
            GameObject dron = MakeEnemyObject("DronSoldadura", typeof(DronSoldaduraController), 2, true, new Vector2(1.5f, 1.2f), enemyLayer,
                Clip("idle", s["DRON_IDLE"], 10, true), Clip("move", s["DRON_MOVE"], 10, true),
                Clip("attack", s["DRON_ATTACK"], 12, false), Clip("dead", s["DRON_DEAD"], 10, false));
            SetField(dron.GetComponent<DronSoldaduraController>(), "bulletPrefab", p.Chispa);
            p.Dron = SavePrefab(dron, "DronSoldadura");
        }

        // --- piezas ---
        p.Engranaje = MakePieza("Pieza_Engranaje", Props + "/pieza_engranaje.png", Pieza.TipoPieza.Engranaje);
        p.Placa = MakePieza("Pieza_Placa", Props + "/pieza_placa.png", Pieza.TipoPieza.Placa);
        p.Artefacto = MakePieza("Artefacto_Improvisado", Props + "/artefacto_improvisado.png", Pieza.TipoPieza.Artefacto);

        // --- replica rodante de FABER ---
        {
            GameObject go = new GameObject("Replica_Faber");
            if (enemyLayer >= 0) go.layer = enemyLayer;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sortingOrder = 31;
            var fa = go.AddComponent<FrameAnimator>();
            fa.SetClips(new List<FrameAnimator.Clip> { Clip("idle", s["FABER_REPLICA"], 12, true) }, sr);
            // del tamano del cuerpo visible (48x72 px = 1,5 x 2,25 u)
            var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(1.3f, 2f);
            var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true;   // que tambien la detecten las balas (cinematicas)
            var h = go.AddComponent<Health>(); SetField(h, "maxHealth", 1);
            go.AddComponent<ReplicaRodante>();
            p.Replica = SavePrefab(go, "Replica_Faber");
        }

        // --- golpe de brazo ---
        {
            GameObject root = new GameObject("GolpeBrazo_Faber");
            GameObject marker = new GameObject("Marca");
            marker.transform.SetParent(root.transform, false);
            marker.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            var msr = marker.AddComponent<SpriteRenderer>();
            msr.sprite = LoadSprite(Props + "/marca_golpe.png"); msr.drawMode = SpriteDrawMode.Tiled;
            msr.size = new Vector2(2.2f, 0.25f); msr.sortingOrder = 29;

            GameObject arm = new GameObject("Brazo");
            arm.transform.SetParent(root.transform, false);
            var asr = arm.AddComponent<SpriteRenderer>(); asr.sortingOrder = 36; asr.sprite = s["FABER_ARM_SLAM"][0];
            var afa = arm.AddComponent<FrameAnimator>();
            afa.SetClips(new List<FrameAnimator.Clip> { Clip("slam", s["FABER_ARM_SLAM"], 12, false) }, asr);
            SetField(afa, "playOnStart", "");

            var slam = root.AddComponent<ArmSlam>();
            SetField(slam, "marker", msr); SetField(slam, "arm", afa); SetField(slam, "armRenderer", asr);
            p.ArmSlam = SavePrefab(root, "GolpeBrazo_Faber");
        }

        return p;
    }

    private static FrameAnimator.Clip Clip(string name, Sprite[] frames, float fps, bool loop, int from = 0, int count = -1)
    {
        Sprite[] f = count < 0 ? frames.Skip(from).ToArray() : frames.Skip(from).Take(count).ToArray();
        return new FrameAnimator.Clip { name = name, frames = f, fps = fps, loop = loop };
    }

    private static GameObject MakeEnemy(string name, Type controller, int hp, bool flying, Vector2 size, int layer, params FrameAnimator.Clip[] clips)
    {
        return SavePrefab(MakeEnemyObject(name, controller, hp, flying, size, layer, clips), name);
    }

    private static GameObject MakeEnemyObject(string name, Type controller, int hp, bool flying, Vector2 size, int layer, params FrameAnimator.Clip[] clips)
    {
        GameObject go = new GameObject(name);
        if (layer >= 0) go.layer = layer;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sortingOrder = 30; sr.sprite = clips[0].frames[0];
        var fa = go.AddComponent<FrameAnimator>(); fa.SetClips(clips.ToList(), sr);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = flying ? 0f : 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.offset = flying ? Vector2.zero : new Vector2(0f, size.y / 2f);
        var h = go.AddComponent<Health>(); SetField(h, "maxHealth", hp);
        go.AddComponent(controller);
        return go;
    }

    private static GameObject MakePieza(string name, string spritePath, Pieza.TipoPieza tipo)
    {
        GameObject go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = LoadSprite(spritePath); sr.sortingOrder = 28;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f; rb.mass = 0.6f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = sr.sprite.bounds.size * 0.9f;
        var mat = new PhysicsMaterial2D(name + "_Mat") { friction = 0.35f, bounciness = 0f };
        string matPath = PrefabDir + "/" + name + "_Mat.physicsMaterial2D";
        AssetDatabase.DeleteAsset(matPath);
        AssetDatabase.CreateAsset(mat, matPath);
        col.sharedMaterial = mat;
        // Las piezas son bajas: una zona de impacto invisible (trigger) mas alta
        // hace que tambien las empuje el disparo de pie.
        var hit = new GameObject("ZonaDeImpacto");
        hit.transform.SetParent(go.transform, false);
        var hcol = hit.AddComponent<BoxCollider2D>(); hcol.isTrigger = true;
        float bottom = -col.size.y / 2f;
        hcol.size = new Vector2(col.size.x, 2.5f);
        hcol.offset = new Vector2(0f, bottom + 1.25f);
        var pz = go.AddComponent<Pieza>();
        SetEnum(pz, "tipo", (int)tipo);
        return SavePrefab(go, name);
    }

    private static GameObject SavePrefab(GameObject go, string name)
    {
        string path = PrefabDir + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    // ================================================================== escena
    private static void CopyLevel1()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            // Se sobrescribe para conservar el GUID de la escena.
            File.Copy(FullPath(Level1Path), FullPath(ScenePath), true);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
        }
        else
        {
            AssetDatabase.CopyAsset(Level1Path, ScenePath);
        }
    }

    private static void SetupScene(Scene scene, Dictionary<string, Sprite[]> s, Prefabs pf)
    {
        // --- fondo de fabrica con paralaje y suelo ---
        GameObject env = Find(scene, "Environment");
        if (env != null)
        {
            foreach (SpriteRenderer sr in env.GetComponentsInChildren<SpriteRenderer>(true).ToList())
                UnityEngine.Object.DestroyImmediate(sr.gameObject);
        }
        Camera cam = All<Camera>(scene).FirstOrDefault();
        if (cam != null) cam.backgroundColor = new Color(0.04f, 0.03f, 0.03f);

        GameObject bg = NewObject(scene, "BG_Fabrica", env != null ? env.transform : null, new Vector3(60f, 0f, 0f));
        var bgsr = bg.AddComponent<SpriteRenderer>(); bgsr.sprite = LoadSprite(Env + "/BG_Fabrica.png"); bgsr.sortingOrder = -30;
        var par = bg.AddComponent<ParallaxBackground>(); SetField(par, "cam", cam);

        GameObject floor = NewObject(scene, "Suelo_Fabrica", env != null ? env.transform : null, new Vector3(60f, 0.02f, 0f));
        var fsr = floor.AddComponent<SpriteRenderer>(); fsr.sprite = LoadSprite(Env + "/Suelo_Fabrica.png");
        fsr.drawMode = SpriteDrawMode.Tiled; fsr.size = new Vector2(130f, 120f / BackgroundPPU); fsr.sortingOrder = -10;

        // Sin ambientacion sonora de la Educacion en la fabrica.
        foreach (EducationAmbience amb in All<EducationAmbience>(scene).ToList())
            UnityEngine.Object.DestroyImmediate(amb);

        // --- Tadeo reemplaza a los Eco del nivel 1 ---
        foreach (Transform t in All<Transform>(scene).Where(t => t.name.StartsWith("EcoEstudiante")).ToList())
        {
            string suffix = t.name.Substring("EcoEstudiante".Length);
            Vector3 pos = t.position;
            Transform parent = t.parent;
            UnityEngine.Object.DestroyImmediate(t.gameObject);

            GameObject tadeo = NewObject(scene, "Tadeo" + suffix, parent, pos);
            var tsr = tadeo.AddComponent<SpriteRenderer>(); tsr.sortingOrder = 29; tsr.sprite = s["TADEO_IDLE"][0];
            var tfa = tadeo.AddComponent<FrameAnimator>();
            tfa.SetClips(new List<FrameAnimator.Clip> { Clip("idle", s["TADEO_IDLE"], 4, true) }, tsr);
        }

        // --- el tutorial ya se vio en el nivel 1 ---
        GameObject tutorial = Find(scene, "TutorialText");
        if (tutorial != null)
        {
            tutorial.SetActive(false);
            foreach (DialogueTrigger tr in All<DialogueTrigger>(scene))
                RemoveFromArray(tr, "activateOnEnd", tutorial);
        }

        // --- dialogos ---
        foreach (DialogueTrigger tr in All<DialogueTrigger>(scene))
        {
            if (!Dialogues.TryGetValue(tr.gameObject.name, out Line[] lines))
            {
                Debug.LogWarning("[Level2Creacion] Dialogo sin texto: " + tr.gameObject.name);
                continue;
            }
            SetLines(tr, "lines", lines);
        }

        foreach (DialogueManager dm in All<DialogueManager>(scene))
        {
            SetSpriteArray(dm, "tadeoPortraitFrames", s["RETRATO_TADEO"]);
            SetSpriteArray(dm, "faberPortraitFrames", s["RETRATO_FABER"]);
        }

        // --- oleadas con los enemigos de la fabrica ---
        foreach (var w in Waves)
        {
            GameObject enc = Find(scene, w.enc);
            Transform wave = enc != null ? enc.transform.Find(w.wave) : null;
            if (wave == null) { Debug.LogWarning($"[Level2Creacion] No se encontro {w.enc}/{w.wave}"); continue; }

            foreach (Transform child in wave.Cast<Transform>().ToList())
                if (child.GetComponent<EnemyController>() || child.GetComponent<CodiceController>() || child.GetComponent<FactoryEnemy>())
                    UnityEngine.Object.DestroyImmediate(child.gameObject);

            int n = 1;
            foreach (var e in w.list)
            {
                GameObject prefab = e.k == Kind.Ensamblador ? pf.Ensamblador : e.k == Kind.Corrector ? pf.Corrector : pf.Dron;
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, wave);
                go.name = $"{prefab.name}_{n++:00}";
                go.transform.position = new Vector3(e.x, e.y + 0.02f, 0f);
                FactoryEnemy fe = go.GetComponent<FactoryEnemy>();
                SetField(fe, "sectionMinX", w.minX);
                SetField(fe, "sectionMaxX", w.maxX);
            }
        }

        // --- el jugador improvisa ---
        GameObject player = All<PlayerController>(scene).First().gameObject;
        var imp = player.GetComponent<ElianImprovisacion>() ?? player.AddComponent<ElianImprovisacion>();
        SetSpriteArray(imp, "craftFrames", s["ELIAN_CRAFT"]);
        SetSpriteArray(imp, "pushFrames", s["ELIAN_PUSH"]);
        SetSpriteArray(imp, "victoryFrames", s["ELIAN_ACTIVATE"]);
        SetField(imp, "artefactoPrefab", pf.Artefacto);

        BuildArena(scene, s, pf);
        BuildGates(scene);

        // --- titulo del nivel ---
        foreach (LevelTitleCard card in All<LevelTitleCard>(scene))
        {
            SetField(card, "subtitle", "NIVEL 2");
            SetField(card, "title", "CREACIÓN");
            SetColor(card, "titleColor", new Color(1f, 0.62f, 0.2f));
        }
    }

    private static void BuildArena(Scene scene, Dictionary<string, Sprite[]> s, Prefabs pf)
    {
        // Se retira a Sophia y su disparador de combate del nivel 1.
        foreach (SophiaController sc in All<SophiaController>(scene).ToList())
            UnityEngine.Object.DestroyImmediate(sc.gameObject);
        GameObject oldStart = Find(scene, "SophiaBattleStart");
        Transform enc3 = oldStart != null ? oldStart.transform.parent : Find(scene, "Encounter_03")?.transform;

        GameObject arena = NewObject(scene, "Arena_Faber", null, Vector3.zero);

        // --- FABER ---
        GameObject faber = NewObject(scene, "FABER", arena.transform, new Vector3(107.5f, 0.3f, 0f));
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) faber.layer = enemyLayer;
        var fsr = faber.AddComponent<SpriteRenderer>(); fsr.sortingOrder = 20; fsr.sprite = s["FABER_IDLE"][0];
        var ffa = faber.AddComponent<FrameAnimator>();
        ffa.SetClips(new List<FrameAnimator.Clip>
        {
            Clip("idle", s["FABER_IDLE"], 8, true),
            Clip("open", s["FABER_OPEN"], 10, false, 0, 6),
            Clip("close", s["FABER_OPEN"], 8, false, 6, 2),
            Clip("dead", s["FABER_DEAD"], 7, false),
        }, fsr);
        var fcol = faber.AddComponent<BoxCollider2D>(); fcol.isTrigger = true;
        fcol.size = new Vector2(3.6f, 6.3f); fcol.offset = new Vector2(0f, 3.05f); // de y 0,2 a 6,5: le dan disparos de pie y agachado
        var fh = faber.AddComponent<Health>(); SetField(fh, "maxHealth", 36); SetField(fh, "invulnerable", true);
        var fc = faber.AddComponent<FaberController>();

        // --- muro de la boca: Elian no pasa, las piezas si (por debajo) ---
        GameObject wall = NewObject(scene, "Boca_Muro", arena.transform, new Vector3(102.8f, 0f, 0f));
        wall.AddComponent<BulletPassThrough>(); // las balas de Elian pasan; Elian no
        var wcol = wall.AddComponent<BoxCollider2D>(); wcol.size = new Vector2(0.6f, 10f); wcol.offset = new Vector2(0f, 2.1f + 5f); // hueco de 2 u: pasa el artefacto, Elian (3,2) no

        GameObject intake = NewObject(scene, "Boca_Ensamblaje", arena.transform, new Vector3(102.2f, 1.2f, 0f));
        var icol = intake.AddComponent<BoxCollider2D>(); icol.isTrigger = true; icol.size = new Vector2(1.6f, 2.4f); // antes del muro: recoge tambien piezas apiladas
        var fi = intake.AddComponent<FaberIntake>(); SetField(fi, "faber", fc);

        // --- cinta ---
        GameObject belt = NewObject(scene, "Cinta_Transportadora", arena.transform, new Vector3(99.5f, 0f, 0f));
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) belt.layer = groundLayer;
        var bcol = belt.AddComponent<BoxCollider2D>(); bcol.size = new Vector2(11f, 0.2f); bcol.usedByEffector = true;
        var eff = belt.AddComponent<SurfaceEffector2D>(); eff.speed = 2f; eff.useFriction = false;
        GameObject beltVis = NewObject(scene, "Visual", belt.transform, new Vector3(99.5f, -0.1f, 0f));
        var bsr = beltVis.AddComponent<SpriteRenderer>(); bsr.sprite = s["CINTA"][0];
        bsr.drawMode = SpriteDrawMode.Tiled; bsr.size = new Vector2(11f, 0.5f); bsr.sortingOrder = 27;
        var cinta = belt.AddComponent<CintaTransportadora>();
        SetField(cinta, "beltRenderer", bsr); SetSpriteArray(cinta, "beltFrames", s["CINTA"]);

        // --- tolva y seguro ---
        GameObject tolvaGo = NewObject(scene, "Tolva_Descarte", arena.transform, new Vector3(91.8f, 6.4f, 0f));
        var tsr = tolvaGo.AddComponent<SpriteRenderer>(); tsr.sprite = s["TOLVA"][0]; tsr.sortingOrder = 22;
        GameObject drop = NewObject(scene, "PuntoDeCaida", tolvaGo.transform, new Vector3(91.8f, 4.2f, 0f));
        GameObject latch = NewObject(scene, "Seguro", tolvaGo.transform, new Vector3(92.9f, 5.1f, 0f));
        if (enemyLayer >= 0) latch.layer = enemyLayer;
        var lsr = latch.AddComponent<SpriteRenderer>(); lsr.sprite = LoadSprite(Props + "/seguro_tolva.png"); lsr.sortingOrder = 24;
        var lcol = latch.AddComponent<BoxCollider2D>(); lcol.isTrigger = true; lcol.size = new Vector2(0.9f, 0.9f);
        var lh = latch.AddComponent<Health>(); SetField(lh, "maxHealth", 3); SetField(lh, "invulnerable", true);
        var tolva = tolvaGo.AddComponent<Tolva>();
        SetField(tolva, "body", tsr); SetField(tolva, "closedSprite", s["TOLVA"][0]); SetField(tolva, "openSprite", s["TOLVA"][1]);
        SetField(tolva, "dropPoint", drop.transform); SetField(tolva, "engranajePrefab", pf.Engranaje);
        SetField(tolva, "placaPrefab", pf.Placa); SetField(tolva, "latch", lh);

        // --- capsulas de fabricacion ---
        FabricationPod MakePod(string name, float x, Sprite[] frames, GameObject enemy)
        {
            GameObject pod = NewObject(scene, name, arena.transform, new Vector3(x, 0f, 0f));
            var psr = pod.AddComponent<SpriteRenderer>(); psr.sprite = frames[0]; psr.sortingOrder = 18;
            var pfa = pod.AddComponent<FrameAnimator>();
            pfa.SetClips(new List<FrameAnimator.Clip>
            {
                Clip("idle", frames, 1, true, 0, 1),
                Clip("build", frames, 7, false, 0, 5),
            }, psr);
            GameObject sp = NewObject(scene, "Salida", pod.transform, new Vector3(x, 0.3f, 0f));
            var fp = pod.AddComponent<FabricationPod>();
            SetField(fp, "anim", pfa); SetField(fp, "enemyPrefab", enemy); SetField(fp, "spawnPoint", sp.transform);
            SetField(fp, "sectionMinX", 86f); SetField(fp, "sectionMaxX", 102.5f);
            return fp;
        }
        FabricationPod podE = MakePod("Capsula_Ensamblador", 96.5f, s["FABER_POD_ENSAMBLADOR"], pf.Ensamblador);
        FabricationPod podC = MakePod("Capsula_Corrector", 100f, s["FABER_POD_CORRECTOR"], pf.Corrector);

        GameObject replicaSpawn = NewObject(scene, "SalidaReplicas", arena.transform, new Vector3(102f, 1.15f, 0f));

        // --- referencias de FABER ---
        SetField(fc, "body", ffa); SetField(fc, "bodyRenderer", fsr); SetField(fc, "cinta", cinta);
        SetField(fc, "tolva", tolva); SetField(fc, "podEnsamblador", podE); SetField(fc, "podCorrector", podC);
        SetField(fc, "armSlamPrefab", pf.ArmSlam); SetField(fc, "replicaPrefab", pf.Replica);
        SetField(fc, "replicaSpawn", replicaSpawn.transform);
        SetField(fc, "arenaMinX", 88f); SetField(fc, "arenaMaxX", 102f);
        SetField(fc, "cameraLockFromX", 89f); SetField(fc, "cameraLockX", 101f);
        SetLines(fc, "closingLines", ClosingLines);

        // --- inicio del combate al terminar el dialogo ---
        GameObject starter = NewObject(scene, "FaberBattleStart", enc3 != null ? enc3 : arena.transform, new Vector3(100f, 0f, 0f));
        var fbs = starter.AddComponent<FaberBattleStarter>(); SetField(fbs, "faber", fc);
        starter.SetActive(false);

        GameObject trig = Find(scene, "Trigger_Dialogue_Sophia.");
        if (trig != null)
        {
            trig.name = "Trigger_Dialogue_Faber";
            // antes de la cinta, para que Elian no se mueva durante el dialogo
            trig.transform.position = new Vector3(93.8f, trig.transform.position.y, 0f);
            var box = trig.GetComponent<BoxCollider2D>();
            if (box != null) box.size = new Vector2(1f, 4f);
            ReplaceInArray(trig.GetComponent<DialogueTrigger>(), "activateOnEnd", oldStart, starter);
        }
        if (oldStart != null)
            UnityEngine.Object.DestroyImmediate(oldStart);

        foreach (BossHealthUI ui in All<BossHealthUI>(scene))
        {
            SetField(ui, "health", fh);
            // la barra del nivel 1 tiene a Sophia dibujada: version con FABER
            foreach (UnityEngine.UI.Image img in ui.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (img.name == "BossHealthFill") img.sprite = LoadSprite(Hud + "/barra_boss_faber.png");
                if (img.name == "BossHealthFrame") img.sprite = LoadSprite(Hud + "/barra_boss_faber_vacia.png");
            }
        }

        // --- musica de la fabrica ---
        GameObject music = NewObject(scene, "Musica_Nivel2", null, Vector3.zero);
        music.AddComponent<AudioSource>();
        var lm = music.AddComponent<LevelMusic>();
        SetField(lm, "clip", AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath));
    }

    // ================================================================== nivel 1 y build
    private static void LinkLevel1()
    {
        Scene l1 = EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Single);
        foreach (SophiaDeath d in All<SophiaDeath>(l1))
        {
            SetField(d, "bossDisplayName", "Sophia");
            SetField(d, "nextSceneName", SceneName);
        }
        EditorSceneManager.MarkSceneDirty(l1);
        EditorSceneManager.SaveScene(l1);
    }

    private static void UpdateBuildSettings()
    {
        var list = EditorBuildSettings.scenes.Where(x => !x.path.Contains("/02_")).ToList();
        if (list.All(x => x.path != Level1Path))
            list.Insert(0, new EditorBuildSettingsScene(Level1Path, true));
        int i = list.FindIndex(x => x.path == Level1Path);
        list.Insert(i + 1, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ================================================================== utilidades
    private static IEnumerable<T> All<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));

    private static GameObject Find(Scene scene, string name) =>
        All<Transform>(scene).FirstOrDefault(t => t.name == name)?.gameObject;

    // Compuertas entre secciones: el collider "Gate_xx" del nivel 1 bloquea el
    // paso hasta limpiar las oleadas; encima va una compuerta industrial dibujada
    // con la paleta del fondo, que sube su persiana al quedar libre.
    private static void BuildGates(Scene scene)
    {
        Sprite marco = LoadSprite(Props + "/compuerta_marco.png");
        Sprite persiana = LoadSprite(Props + "/compuerta_persiana.png");
        Sprite luzRoja = LoadSprite(Props + "/compuerta_luz_roja.png");
        Sprite luzVerde = LoadSprite(Props + "/compuerta_luz_verde.png");

        foreach (string gateName in new[] { "Gate_01_02", "Gate_02_03" })
        {
            GameObject gate = Find(scene, gateName);
            if (gate == null) { Debug.LogWarning($"[Level2Creacion] No se encontro {gateName}"); continue; }

            // El bloqueo ocupa el ancho de la persiana.
            var box = gate.GetComponent<BoxCollider2D>();
            if (box != null) box.size = new Vector2(persiana.bounds.size.x, box.size.y);

            float x = gate.transform.position.x;
            GameObject root = NewObject(scene, "Compuerta_" + gateName.Substring(5), null, new Vector3(x, 0f, 0f));

            var fsr = root.AddComponent<SpriteRenderer>(); fsr.sprite = marco; fsr.sortingOrder = 14;

            // la persiana arranca 10 px sobre el suelo (base del marco)
            GameObject shutter = NewObject(scene, "Persiana", root.transform, new Vector3(x, 10f / BackgroundPPU, 0f));
            var ssr = shutter.AddComponent<SpriteRenderer>(); ssr.sprite = persiana; ssr.sortingOrder = 13;

            // luz en la viga izquierda, a 4 unidades del suelo
            GameObject lamp = NewObject(scene, "Luz", root.transform,
                new Vector3(x - marco.bounds.extents.x + 5.5f / BackgroundPPU, 4f, 0f));
            var lsr = lamp.AddComponent<SpriteRenderer>(); lsr.sprite = luzRoja; lsr.sortingOrder = 15;

            var fg = root.AddComponent<FactoryGate>();
            SetField(fg, "blocker", gate);
            SetField(fg, "shutter", shutter.transform);
            SetField(fg, "lamp", lsr);
            SetField(fg, "lampOpen", luzVerde);
        }
    }

    private static GameObject NewObject(Scene scene, string name, Transform parent, Vector3 worldPos)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        else SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = worldPos;
        return go;
    }

    private static string FullPath(string assetPath) =>
        Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static SerializedProperty Prop(UnityEngine.Object o, string name, out SerializedObject so)
    {
        so = new SerializedObject(o);
        SerializedProperty p = so.FindProperty(name);
        if (p == null)
            Debug.LogError($"[Level2Creacion] Campo '{name}' no encontrado en {o.GetType().Name}");
        return p;
    }

    private static void SetField(UnityEngine.Object o, string name, object value)
    {
        SerializedProperty p = Prop(o, name, out SerializedObject so);
        if (p == null) return;
        switch (value)
        {
            case int i: p.intValue = i; break;
            case float f: p.floatValue = f; break;
            case bool b: p.boolValue = b; break;
            case string str: p.stringValue = str; break;
            case UnityEngine.Object obj: p.objectReferenceValue = obj; break;
            case null: p.objectReferenceValue = null; break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(UnityEngine.Object o, string name, int value)
    {
        SerializedProperty p = Prop(o, name, out SerializedObject so);
        if (p == null) return;
        p.enumValueIndex = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetColor(UnityEngine.Object o, string name, Color c)
    {
        SerializedProperty p = Prop(o, name, out SerializedObject so);
        if (p == null) return;
        p.colorValue = c;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSpriteArray(UnityEngine.Object o, string name, Sprite[] sprites)
    {
        SerializedProperty p = Prop(o, name, out SerializedObject so);
        if (p == null) return;
        p.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetLines(UnityEngine.Object o, string name, Line[] lines)
    {
        SerializedProperty p = Prop(o, name, out SerializedObject so);
        if (p == null) return;
        p.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++)
        {
            SerializedProperty e = p.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("speaker").intValue = (int)lines[i].S;
            e.FindPropertyRelative("text").stringValue = lines[i].T;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveFromArray(UnityEngine.Object o, string name, UnityEngine.Object value)
    {
        SerializedProperty p = Prop(o, name, out SerializedObject so);
        if (p == null) return;
        for (int i = p.arraySize - 1; i >= 0; i--)
        {
            if (p.GetArrayElementAtIndex(i).objectReferenceValue == value)
            {
                p.GetArrayElementAtIndex(i).objectReferenceValue = null;
                p.DeleteArrayElementAtIndex(i);
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ReplaceInArray(UnityEngine.Object o, string name, UnityEngine.Object oldValue, UnityEngine.Object newValue)
    {
        if (o == null) return;
        SerializedProperty p = Prop(o, name, out SerializedObject so);
        if (p == null) return;
        bool replaced = false;
        for (int i = 0; i < p.arraySize; i++)
        {
            if (oldValue != null && p.GetArrayElementAtIndex(i).objectReferenceValue == oldValue)
            {
                p.GetArrayElementAtIndex(i).objectReferenceValue = newValue;
                replaced = true;
            }
        }
        if (!replaced)
        {
            p.arraySize++;
            p.GetArrayElementAtIndex(p.arraySize - 1).objectReferenceValue = newValue;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
