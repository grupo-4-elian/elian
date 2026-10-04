using System.Collections;
using UnityEngine;

// FABER, el Artifice: jefe del Nivel 2 ("Creacion").
//
// No se le vence disparando: todo lo que le llega ya esta previsto.
//  Fase 1 (100 % -> 50 %): una pieza defectuosa que llega por la cinta lo
//          atasca ("error de ensamblaje") y abre su carcasa unos segundos.
//  Fase 2 (50 % -> 15 %): rechaza las piezas sueltas; solo un ARTEFACTO
//          combinado por Elian (sin plano) lo sobrecarga y lo abre.
//  Fase 3 (15 % -> 0 %): invierte la cinta y lanza replicas. Romper el
//          seguro de la tolva vuelca todo el descarte sobre el y lo derriba.
[RequireComponent(typeof(Health))]
public class FaberController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private FrameAnimator body;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private CintaTransportadora cinta;
    [SerializeField] private Tolva tolva;
    [SerializeField] private FabricationPod podEnsamblador;
    [SerializeField] private FabricationPod podCorrector;
    [SerializeField] private GameObject armSlamPrefab;
    [SerializeField] private GameObject replicaPrefab;
    [SerializeField] private Transform replicaSpawn;

    [Header("Arena")]
    [SerializeField] private float arenaMinX = 88f;
    [SerializeField] private float arenaMaxX = 106f;
    [Tooltip("La camara se fija en la arena cuando Elian pasa esta X.")]
    [SerializeField] private float cameraLockFromX = 89f;
    [SerializeField] private float cameraLockX = 101f;

    [Header("Fases")]
    [SerializeField, Range(0f, 1f)] private float phase2Threshold = 0.5f;
    [SerializeField, Range(0f, 1f)] private float phase3Threshold = 0.15f;
    [SerializeField] private float jamDuration = 6f;
    [SerializeField] private float overloadDuration = 7f;
    [SerializeField] private int collapseItems = 3;

    [Header("Ritmo de ataques (segundos)")]
    [SerializeField] private float armIntervalPhase1 = 5.5f;
    [SerializeField] private float armIntervalLate = 4f;
    [SerializeField] private float armTelegraphPhase1 = 1.4f;
    [SerializeField] private float armTelegraphLate = 1.1f;
    [SerializeField] private float fabricateInterval = 18f;
    [SerializeField] private float replicaInterval = 3.5f;

    [Header("Final")]
    [SerializeField] private DialogueManager.DialogueLine[] closingLines;

    private Health health;
    private Transform player;
    private Health playerHealth;

    private int phase;
    private bool battleActive;
    private bool vulnerable;
    private bool finished;
    private bool cameraLocked;
    private int itemsFedPhase3;
    private float nextArm, nextFabricate, nextReplica;
    private Coroutine openRoutine;

    private string bark;
    private float barkUntil;

    public bool IsBattleActive => battleActive;
    public int Phase => phase;
    public bool IsVulnerable => vulnerable;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.SetInvulnerable(true);
        health.HealthChanged += OnHealthChanged;
        health.Died += OnDied;

        if (tolva != null)
            tolva.Dumped += OnDumped;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<Health>();
        }
        if (body != null)
            body.Play("idle", true);
    }

    public void StartBattle()
    {
        if (battleActive || finished)
            return;

        battleActive = true;
        phase = 1;
        nextArm = Time.time + 2f;
        nextFabricate = Time.time + 5f;
        if (tolva != null)
            tolva.StartDropping();
    }

    private bool Paused =>
        DialogueManager.IsDialogueActive || PauseMenu.IsPaused ||
        (playerHealth != null && playerHealth.IsDead);

    private void Update()
    {
        if (!cameraLocked && player != null && player.position.x >= cameraLockFromX)
        {
            CameraFollowPlayer follow = Camera.main != null ? Camera.main.GetComponent<CameraFollowPlayer>() : null;
            if (follow != null)
                follow.LockTo(cameraLockX);
            cameraLocked = true;
        }

        if (!battleActive || finished || Paused || player == null)
            return;

        // Atascado o sobrecargado no ataca ni fabrica: es el momento de dispararle.
        if (vulnerable)
        {
            nextArm = Mathf.Max(nextArm, Time.time + 1.5f);
            nextFabricate = Mathf.Max(nextFabricate, Time.time + 3f);
            return;
        }

        if (Time.time >= nextArm && armSlamPrefab != null)
        {
            nextArm = Time.time + (phase == 1 ? armIntervalPhase1 : armIntervalLate);
            float x = Mathf.Clamp(player.position.x, arenaMinX, arenaMaxX);
            GameObject slam = Instantiate(armSlamPrefab, new Vector3(x, 0f, 0f), Quaternion.identity);
            slam.GetComponent<ArmSlam>().Begin(phase == 1 ? armTelegraphPhase1 : armTelegraphLate);
        }

        if (phase < 3 && Time.time >= nextFabricate)
        {
            nextFabricate = Time.time + fabricateInterval;
            FabricationPod pod = phase == 1 ? podEnsamblador : podCorrector;
            if (pod != null && pod.CanFabricate)
                pod.Fabricate();
        }

        if (phase == 3 && Time.time >= nextReplica && replicaPrefab != null && replicaSpawn != null)
        {
            nextReplica = Time.time + replicaInterval;
            Instantiate(replicaPrefab, replicaSpawn.position, Quaternion.identity);
        }
    }

    // Llamado por la boca de ensamblaje cuando la cinta le entrega algo.
    public void OnItemReceived(Pieza pieza)
    {
        if (!battleActive || finished)
        {
            pieza.Consume();
            return;
        }

        switch (phase)
        {
            case 1:
                pieza.Consume();
                Open(pieza.Tipo == Pieza.TipoPieza.Artefacto ? overloadDuration : jamDuration,
                     "Error de ensamblaje. Pieza sin instrucción.");
                break;

            case 2:
                pieza.Consume();
                if (pieza.Tipo == Pieza.TipoPieza.Artefacto)
                    Open(overloadDuration, "Objeto sin plano. Imposible de clasificar.");
                else
                    Bark("Pieza defectuosa detectada. Descartando.");
                break;

            default:
                pieza.Consume();
                itemsFedPhase3++;
                if (itemsFedPhase3 >= collapseItems)
                    Collapse();
                break;
        }
    }

    private void Open(float duration, string line)
    {
        if (vulnerable)
            return;

        Bark(line);
        Sfx.Play(Sfx.Descarga);
        if (openRoutine != null)
            StopCoroutine(openRoutine);
        openRoutine = StartCoroutine(OpenRoutine(duration));
    }

    private IEnumerator OpenRoutine(float duration)
    {
        vulnerable = true;
        health.SetInvulnerable(false);
        body.Play("open", true);

        yield return new WaitForSeconds(duration);

        EndVulnerable();
    }

    private void EndVulnerable()
    {
        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        bool wasOpen = vulnerable;
        vulnerable = false;
        health.SetInvulnerable(true);

        if (wasOpen && !finished)
            StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        body.Play("close", true);
        yield return new WaitForSeconds(body.LengthOf("close"));
        if (!finished)
            body.Play("idle", true);
    }

    private void OnHealthChanged(int current, int max)
    {
        if (current <= 0)
            return;

        StartCoroutine(Flash());
        Sfx.PlayHurtFallback(this, Sfx.GolpeEnemigo);

        if (phase == 1 && current <= Mathf.CeilToInt(max * phase2Threshold))
            EnterPhase2();
        else if (phase == 2 && current <= Mathf.CeilToInt(max * phase3Threshold))
            EnterPhase3();
    }

    private IEnumerator Flash()
    {
        if (bodyRenderer == null)
            yield break;
        bodyRenderer.color = new Color(1f, 0.6f, 0.5f);
        yield return new WaitForSeconds(0.07f);
        bodyRenderer.color = Color.white;
    }

    private void EnterPhase2()
    {
        phase = 2;
        EndVulnerable();
        nextFabricate = Time.time + 3f;
        Bark("Pieza defectuosa registrada. Iniciando control de calidad.");
    }

    private void EnterPhase3()
    {
        phase = 3;
        EndVulnerable();
        nextReplica = Time.time + 1.5f;
        if (cinta != null)
            cinta.SetDirection(-1);
        if (tolva != null)
        {
            tolva.StopDropping();
            tolva.ExposeLatch();
        }
        Bark("Iniciando réplica del objeto.");
    }

    // El seguro se rompio: el descarte cae y la cinta vuelve a llevarlo a FABER.
    private void OnDumped()
    {
        if (cinta != null)
            cinta.SetDirection(1);
        Bark("Carga no prevista... ¡sobrecarga!");
    }

    private void Collapse()
    {
        if (finished)
            return;
        health.SetInvulnerable(false);
        health.TakeDamage(health.CurrentHealth);
    }

    private void OnDied()
    {
        finished = true;
        battleActive = false;
        vulnerable = false;
        StopAllCoroutines();
        if (bodyRenderer != null)
            bodyRenderer.color = Color.white;

        if (tolva != null)
            tolva.StopDropping();

        // Se detiene la produccion: los robots y replicas en pie se apagan.
        foreach (FactoryEnemy e in FindObjectsByType<FactoryEnemy>(FindObjectsSortMode.None))
        {
            Health h = e.GetComponent<Health>();
            if (h != null && !h.IsDead) { h.SetInvulnerable(false); h.TakeDamage(h.CurrentHealth); }
        }
        foreach (ReplicaRodante r in FindObjectsByType<ReplicaRodante>(FindObjectsSortMode.None))
            Destroy(r.gameObject);
        foreach (ArmSlam a in FindObjectsByType<ArmSlam>(FindObjectsSortMode.None))
            Destroy(a.gameObject);
        if (podEnsamblador != null) podEnsamblador.Stop();
        if (podCorrector != null) podCorrector.Stop();

        Sfx.PlayDeathFallback(this, Sfx.ExplosionJefe);
        LevelMusic.FadeOut(2.5f);
        body.Play("dead", true);
        StartCoroutine(Ending());
    }

    private IEnumerator Ending()
    {
        yield return new WaitForSeconds(body.LengthOf("dead") + 0.8f);

        DialogueManager dm = FindAnyObjectByType<DialogueManager>();
        if (dm != null && closingLines != null && closingLines.Length > 0)
        {
            bool done = false;
            dm.StartDialogue(closingLines, () => done = true);
            while (!done)
                yield return null;
        }

        ElianImprovisacion elian = player != null ? player.GetComponent<ElianImprovisacion>() : null;
        if (elian != null)
        {
            yield return new WaitForSeconds(0.2f);
            yield return elian.VictoryPose();
        }

        VictoryScreen.Show(0.3f, "FABER", "");
    }

    private void Bark(string line)
    {
        bark = line;
        barkUntil = Time.time + 2.8f;
    }

    // Frases de FABER durante el combate, sin pausar el juego.
    private void OnGUI()
    {
        if (DialogueManager.IsDialogueActive || PauseMenu.IsPaused)
            return;

        if (string.IsNullOrEmpty(bark) || Time.time > barkUntil)
            return;

        float h = Screen.height;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(h * 0.035f),
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        Rect r = new Rect(0, h * 0.84f, Screen.width, h * 0.07f);
        style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), "FABER: «" + bark + "»", style);
        style.normal.textColor = new Color(1f, 0.62f, 0.25f);
        GUI.Label(r, "FABER: «" + bark + "»", style);
    }
}
