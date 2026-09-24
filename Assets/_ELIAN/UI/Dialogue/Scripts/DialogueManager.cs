using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public enum Speaker
    {
        // El orden importa: las escenas guardan el numero, no el nombre.
        // Los nuevos hablantes se agregan siempre al final.
        Eco,
        Elian,
        Sophia,
        EcoObrero, // Nivel 2 - Limbo del Trabajo
        Optima     // Nivel 2 - jefe
    }

    [Serializable]
    public class DialogueLine
    {
        public Speaker speaker;

        [TextArea(2, 4)]
        public string text;
    }

    [Header("Interfaz")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private Image portraitImage;

    [Header("Retratos animados")]
    [Tooltip("Frames del retrato de Elian, en orden.")]
    [SerializeField] private Sprite[] elianPortraitFrames;
    [Tooltip("Frames del retrato de Eco, en orden.")]
    [SerializeField] private Sprite[] ecoPortraitFrames;
    [Tooltip("Frames del retrato de Sophia, en orden.")]
    [SerializeField] private Sprite[] sophiaPortraitFrames;
    [Tooltip("Frames del retrato del Eco Obrero (nivel 2), en orden.")]
    [SerializeField] private Sprite[] ecoObreroPortraitFrames;
    [Tooltip("Frames del retrato de Optima (nivel 2), en orden.")]
    [SerializeField] private Sprite[] optimaPortraitFrames;

    [Header("Tintes de retrato (nivel 2)")]
    [Tooltip("Mientras no haya arte propio, se reutilizan retratos del nivel 1 con este tinte.")]
    [SerializeField] private Color ecoObreroPortraitTint = new Color(0.95f, 0.8f, 0.6f);
    [SerializeField] private Color optimaPortraitTint = new Color(1f, 0.6f, 0.3f);

    [Tooltip("Cuadros por segundo de la animación de retrato (aplica a todos).")]
    [SerializeField] private float portraitFrameRate = 8f;

    private DialogueLine[] currentLines;
    private int currentLine;
    private bool dialogueActive;
    private int dialogueStartFrame = -1;

    private Action onDialogueFinished;

    private PlayerController playerController;
    private Rigidbody2D playerRb;
    private Animator playerAnimator;

    private Coroutine portraitAnimCoroutine;

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    // Otros scripts (ej. PlayerHurt) lo consultan para no devolverle
    // el control al jugador en mitad de un dialogo.
    public static bool IsDialogueActive { get; private set; }

    private void OnDisable()
    {
        // Si la escena se recarga con un dialogo abierto, que no quede trabado.
        if (dialogueActive)
            IsDialogueActive = false;
    }

    private void Update()
    {
        if (!dialogueActive || PauseMenu.IsPaused)
            return;

        // Ignoramos el frame en que arranco el dialogo: si el jugador entro
        // a la zona saltando, ese mismo Espacio se salteaba la primera linea.
        if (Time.frameCount == dialogueStartFrame)
            return;

        if (AdvancePressedThisFrame())
            NextLine();
    }

    private static bool AdvancePressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null &&
            (keyboard.spaceKey.wasPressedThisFrame ||
             keyboard.enterKey.wasPressedThisFrame ||
             keyboard.numpadEnterKey.wasPressedThisFrame))
            return true;

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
            return true;

        return false;
    }

    public void StartDialogue(
        DialogueLine[] lines,
        Action onFinished = null)
    {
        if (lines == null || lines.Length == 0)
            return;

        currentLines = lines;
        currentLine = 0;
        dialogueActive = true;
        IsDialogueActive = true;
        dialogueStartFrame = Time.frameCount;
        onDialogueFinished = onFinished;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            playerRb = player.GetComponent<Rigidbody2D>();
            playerAnimator = player.GetComponent<Animator>();

            if (playerController != null)
                playerController.enabled = false;

            if (playerRb != null)
                playerRb.linearVelocity = Vector2.zero;

            if (playerAnimator != null)
            {
                playerAnimator.SetFloat("Speed", 0f);
                playerAnimator.SetBool("IsGrounded", true);
                playerAnimator.SetBool("IsCrouching", false);
                playerAnimator.SetBool("AimUp", false);

                playerAnimator.ResetTrigger("Attack");
                playerAnimator.ResetTrigger("Hurt");

                playerAnimator.Play("elian_idle", 0, 0f);
            }
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ShowLine();
    }

    private void ShowLine()
    {
        DialogueLine line = currentLines[currentLine];

        if (dialogueText != null)
            dialogueText.text = line.text;

        UpdateSpeaker(line.speaker);
    }

    private void UpdateSpeaker(Speaker speaker)
    {
        string speakerName = "";
        Sprite[] frames = null;
        Color portraitTint = Color.white;

        switch (speaker)
        {
            case Speaker.Eco:
                speakerName = "ECO ESTUDIANTE";
                frames = ecoPortraitFrames;
                break;

            case Speaker.Elian:
                speakerName = "ELIAN";
                frames = elianPortraitFrames;
                break;

            case Speaker.Sophia:
                speakerName = "SOPHIA";
                frames = sophiaPortraitFrames;
                break;

            case Speaker.EcoObrero:
                speakerName = "ECO OBRERO";
                frames = ecoObreroPortraitFrames;
                portraitTint = ecoObreroPortraitTint;
                break;

            case Speaker.Optima:
                speakerName = "ÓPTIMA";
                frames = optimaPortraitFrames;
                portraitTint = optimaPortraitTint;
                break;
        }

        if (speakerNameText != null)
            speakerNameText.text = speakerName;

        if (portraitImage != null)
            portraitImage.color = portraitTint;

        // Frenamos cualquier animación de retrato previa antes de arrancar la nueva.
        StopPortraitAnimation();

        if (portraitImage == null)
            return;

        if (frames != null && frames.Length > 0)
        {
            portraitImage.gameObject.SetActive(true);
            portraitAnimCoroutine = StartCoroutine(AnimatePortrait(frames));
        }
        else
        {
            // No hay frames asignados para este hablante: no mostramos nada
            // en vez de arriesgarnos a mostrar un sprite viejo/incorrecto.
            portraitImage.gameObject.SetActive(false);
        }
    }

    private IEnumerator AnimatePortrait(Sprite[] frames)
    {
        int frameIndex = 0;
        float frameDuration = 1f / Mathf.Max(portraitFrameRate, 0.01f);

        while (true)
        {
            portraitImage.sprite = frames[frameIndex];
            frameIndex = (frameIndex + 1) % frames.Length;

            yield return new WaitForSeconds(frameDuration);
        }
    }

    private void StopPortraitAnimation()
    {
        if (portraitAnimCoroutine != null)
        {
            StopCoroutine(portraitAnimCoroutine);
            portraitAnimCoroutine = null;
        }
    }

    private void NextLine()
    {
        currentLine++;

        if (currentLine >= currentLines.Length)
        {
            EndDialogue();
            return;
        }

        ShowLine();
    }

    private void EndDialogue()
    {
        dialogueActive = false;
        IsDialogueActive = false;

        StopPortraitAnimation();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        StartCoroutine(ReturnControlNextFrame());

        onDialogueFinished?.Invoke();
        onDialogueFinished = null;
    }

    // La tecla que cierra el dialogo (Espacio) es la misma que la de salto.
    // Esperamos un frame para que ese mismo "press" no haga saltar a Elian.
    private IEnumerator ReturnControlNextFrame()
    {
        yield return null;

        // Si en ese frame arranco otro dialogo encadenado, el jugador sigue congelado.
        if (dialogueActive || playerController == null)
            yield break;

        // No revivir el control si Elian murio durante el dialogo.
        Health playerHealth = playerController.GetComponent<Health>();
        if (playerHealth != null && playerHealth.IsDead)
            yield break;

        playerController.enabled = true;
    }
}