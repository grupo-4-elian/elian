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
        Eco,
        Elian,
        Sophia
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
    [Tooltip("Cuadros por segundo de la animación de retrato (aplica a los tres).")]
    [SerializeField] private float portraitFrameRate = 8f;

    private DialogueLine[] currentLines;
    private int currentLine;
    private bool dialogueActive;

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

    private void Update()
    {
        if (!dialogueActive)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            NextLine();
        }
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
        }

        if (speakerNameText != null)
            speakerNameText.text = speakerName;

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

        StopPortraitAnimation();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (playerController != null)
            playerController.enabled = true;

        onDialogueFinished?.Invoke();
        onDialogueFinished = null;
    }
}