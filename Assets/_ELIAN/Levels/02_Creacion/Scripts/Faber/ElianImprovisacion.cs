using System.Collections;
using UnityEngine;

// Acciones de Elian en el Nivel 2 (se agrega al Player solo en esa escena).
// Con la tecla de agacharse (S), junto a piezas del descarte:
//  - Engranaje + placa cerca: las COMBINA en un artefacto improvisado.
//  - Una sola pieza delante: le da un EMPUJON que la manda deslizando.
// Mientras dura la accion se muestran las animaciones propias de Elian.
[RequireComponent(typeof(PlayerController))]
public class ElianImprovisacion : MonoBehaviour
{
    [Header("Animaciones (cuadros)")]
    [SerializeField] private Sprite[] craftFrames;
    [SerializeField] private float craftFps = 8f;
    [SerializeField] private Sprite[] pushFrames;
    [SerializeField] private float pushFps = 12f;
    [SerializeField] private Sprite[] victoryFrames;
    [SerializeField] private float victoryFps = 8f;

    [Header("Interaccion")]
    [SerializeField] private float combineRadius = 3f;
    [SerializeField] private float shoveReach = 1.8f;
    [SerializeField] private float shoveSpeed = 7f;
    [SerializeField] private GameObject artefactoPrefab;

    private PlayerController controller;
    private Animator animator;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Health health;
    private PlayerControls controls;
    private bool busy;

    public bool Busy => busy;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        controls = new PlayerControls();
    }

    private void OnEnable() { controls.Player.Enable(); }
    private void OnDisable() { controls.Player.Disable(); }

    private void Update()
    {
        if (busy || PauseMenu.IsPaused || DialogueManager.IsDialogueActive || health.IsDead)
            return;

        if (!controller.enabled)
            return;

        // Se detecta el momento de pulsar "abajo" (S o flecha), aunque ya se
        // este manteniendo otra direccion.
        var kb = UnityEngine.InputSystem.Keyboard.current;
        var pad = UnityEngine.InputSystem.Gamepad.current;
        bool downPressed =
            (kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) ||
            (pad != null && pad.dpad.down.wasPressedThisFrame);
        if (!downPressed || Mathf.Abs(rb.linearVelocity.y) > 0.2f)
            return;

        if (TryFindCombinable(out Pieza a, out Pieza b))
            StartCoroutine(Combine(a, b));
        else if (TryFindShoveTarget(out Pieza target))
            StartCoroutine(Shove(target));
    }

    // Si un golpe (PlayerHurt) devuelve el control en mitad de una accion,
    // se vuelve a bloquear hasta que la accion termine.
    private void LateUpdate()
    {
        if (busy && controller.enabled)
            controller.enabled = false;
    }

    private float Facing => transform.right.x >= 0f ? 1f : -1f;

    private bool TryFindCombinable(out Pieza gear, out Pieza plate)
    {
        gear = null; plate = null;
        float bestG = combineRadius, bestP = combineRadius;
        foreach (Pieza p in Pieza.Active)
        {
            if (p == null || p.Consumed) continue;
            float d = Vector2.Distance(p.transform.position, transform.position + Vector3.up * 0.5f);
            if (p.Tipo == Pieza.TipoPieza.Engranaje && d < bestG) { gear = p; bestG = d; }
            if (p.Tipo == Pieza.TipoPieza.Placa && d < bestP) { plate = p; bestP = d; }
        }
        return gear != null && plate != null;
    }

    private bool TryFindShoveTarget(out Pieza target)
    {
        target = null;
        float best = shoveReach;
        foreach (Pieza p in Pieza.Active)
        {
            if (p == null || p.Consumed) continue;
            float dx = (p.transform.position.x - transform.position.x) * Facing;
            float dy = Mathf.Abs(p.transform.position.y - transform.position.y);
            if (dx > -0.2f && dx < best && dy < 1.5f)
            {
                target = p;
                best = dx;
            }
        }
        return target != null;
    }

    private IEnumerator Combine(Pieza gear, Pieza plate)
    {
        yield return BeginAction();

        bool tookBoth = false;
        yield return PlayFrames(craftFrames, craftFps, 2, () =>
        {
            // Si mientras tanto una pieza entro en FABER o la rompieron,
            // no hay artefacto.
            tookBoth = gear != null && !gear.Consumed && plate != null && !plate.Consumed;
            if (!tookBoth)
                return;

            // Al tomar las piezas, desaparecen del suelo.
            gear.Consume();
            plate.Consume();
        });

        if (tookBoth && artefactoPrefab != null)
        {
            Vector3 pos = transform.position + new Vector3(Facing * 1.1f, 0.6f, 0f);
            Instantiate(artefactoPrefab, pos, Quaternion.identity);
        }
        Sfx.Play(Sfx.MenuOk);

        EndAction();
    }

    private IEnumerator Shove(Pieza target)
    {
        yield return BeginAction();
        float dir = Facing;

        // El dibujo del empujon ya incluye una pieza: la real se oculta
        // mientras tanto y reaparece al salir despedida.
        target.SetVisible(false);
        int[] order = { 0, 1, 2, 3, 5 };
        Sprite[] seq = new Sprite[order.Length];
        for (int i = 0; i < order.Length; i++)
            seq[i] = pushFrames != null && order[i] < pushFrames.Length ? pushFrames[order[i]] : null;

        yield return PlayFrames(seq, pushFps, 3, () =>
        {
            if (target != null)
            {
                target.transform.position = new Vector3(transform.position.x + dir * 1.2f, target.transform.position.y, 0f);
                target.SetVisible(true);
                target.Shove(dir, shoveSpeed);
            }
        });

        if (target != null)
            target.SetVisible(true);

        EndAction();
    }

    // Pose final al vencer a FABER.
    public IEnumerator VictoryPose()
    {
        yield return BeginAction();
        yield return PlayFrames(victoryFrames, victoryFps, -1, null);
        yield return new WaitForSeconds(0.4f);
        EndAction();
    }

    private IEnumerator BeginAction()
    {
        busy = true;
        controller.enabled = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (animator != null)
            animator.enabled = false;
        yield return null;
    }

    private void EndAction()
    {
        if (animator != null)
            animator.enabled = true;
        if (!health.IsDead && !DialogueManager.IsDialogueActive)
            controller.enabled = true;
        busy = false;
    }

    private IEnumerator PlayFrames(Sprite[] frames, float fps, int eventFrame, System.Action onEvent)
    {
        if (frames == null || frames.Length == 0)
        {
            onEvent?.Invoke();
            yield break;
        }

        float wait = 1f / Mathf.Max(fps, 1f);
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
                sr.sprite = frames[i];
            if (i == eventFrame)
                onEvent?.Invoke();
            yield return new WaitForSeconds(wait);
        }
    }
}
