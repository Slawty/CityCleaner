using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SpeechPanelController : MonoBehaviour
{
    const string DefaultDialogue = "Got a minute, Boss?";

    [SerializeField] GameObject panelRoot;
    [SerializeField] TMP_Text dialogueText;
    [SerializeField] Button acceptButton;
    [SerializeField] float typewriterCharsPerSecond = 45f;
    [SerializeField] EventReference typewriterSoundEvent;

    bool isOpen;
    bool lineFullyRevealed;
    bool suppressDialogueFacingRestore;
    JobClient dialogueClient;
    Action pendingOnAccept;
    string[] pendingLines;
    int pendingLineIndex;
    string currentLineText;
    CancellationTokenSource typewriterCts;
    CancellationTokenSource enableGameplayInputCts;
    EventInstance typewriterLoopInstance;

    void Awake()
    {
        acceptButton.onClick.AddListener(OnAcceptClicked);
        panelRoot.SetActive(false);
    }

    void OnDestroy()
    {
        acceptButton.onClick.RemoveListener(OnAcceptClicked);
        CancelTypewriter();
        StopTypewriterLoop();
        CancelEnableGameplayInputTask();
    }

    public void SetDialogueClient(JobClient client)
    {
        dialogueClient = client;
    }

    public void SuppressDialogueFacingRestore()
    {
        suppressDialogueFacingRestore = true;
    }

    public void Show(string text = null, Action onAccept = null)
    {
        if (isOpen)
            return;

        pendingLines = null;
        pendingLineIndex = 0;
        pendingOnAccept = onAccept;
        OpenPanel(string.IsNullOrEmpty(text) ? DefaultDialogue : text);
    }

    public void ShowDialogueSequence(string[] lines, Action onFinished = null)
    {
        if (lines == null || lines.Length == 0)
        {
            onFinished?.Invoke();
            return;
        }

        pendingLines = lines;
        pendingLineIndex = 0;
        pendingOnAccept = onFinished;

        if (isOpen)
        {
            ShowLine(lines[0]);
            return;
        }

        OpenPanel(lines[0]);
    }

    void OpenPanel(string text)
    {
        CancelEnableGameplayInputTask();
        isOpen = true;
        panelRoot.SetActive(true);
        ShowLine(text);
        ResetAcceptButtonState();

        Managers.UI.HideInteractText();
        Managers.UI.SetHudVisible(false);
        Managers.Input.BlockInteraction(this);
        Managers.Player.SetMovementEnabled(false);
        Managers.Player.mouseLook.SetPointerMode(true);
        Managers.Tools.StopActiveShooting();
    }

    void ShowLine(string text)
    {
        currentLineText = text;
        lineFullyRevealed = false;
        CancelTypewriter();
        typewriterCts = new CancellationTokenSource();
        TypewriterAsync(text, typewriterCts.Token).Forget();
    }

    async UniTaskVoid TypewriterAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            StartTypewriterLoop();

            dialogueText.text = text;
            dialogueText.ForceMeshUpdate();
            dialogueText.maxVisibleCharacters = 0;

            float charDelay = typewriterCharsPerSecond > 0f ? 1f / typewriterCharsPerSecond : 0f;
            int visibleCount = 0;

            while (visibleCount < text.Length)
            {
                visibleCount++;
                dialogueText.maxVisibleCharacters = visibleCount;

                if (charDelay > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(charDelay), ignoreTimeScale: true, cancellationToken: cancellationToken);
                else
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            lineFullyRevealed = true;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            StopTypewriterLoop();
        }
    }

    void CompleteTypewriter()
    {
        CancelTypewriter();

        if (string.IsNullOrEmpty(currentLineText))
        {
            lineFullyRevealed = true;
            return;
        }

        dialogueText.text = currentLineText;
        dialogueText.ForceMeshUpdate();
        dialogueText.maxVisibleCharacters = currentLineText.Length;
        lineFullyRevealed = true;
    }

    void StartTypewriterLoop()
    {
        if (typewriterSoundEvent.IsNull)
            return;

        StopTypewriterLoop();

        typewriterLoopInstance = RuntimeManager.CreateInstance(typewriterSoundEvent);
        RuntimeManager.AttachInstanceToGameObject(typewriterLoopInstance, gameObject);
        typewriterLoopInstance.start();
    }

    void StopTypewriterLoop()
    {
        if (!typewriterLoopInstance.isValid())
            return;

        typewriterLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        typewriterLoopInstance.release();
        typewriterLoopInstance.clearHandle();
    }

    void CancelTypewriter()
    {
        if (typewriterCts == null)
            return;

        typewriterCts.Cancel();
        typewriterCts.Dispose();
        typewriterCts = null;
    }

    public void Close()
    {
        if (!isOpen)
            return;

        CancelTypewriter();
        Managers.Jobs.ClearPendingOffer();

        if (dialogueClient != null)
        {
            dialogueClient.EndDialogueFacing(restoreRotation: !suppressDialogueFacingRestore);
            dialogueClient = null;
        }

        suppressDialogueFacingRestore = false;
        isOpen = false;
        lineFullyRevealed = false;
        currentLineText = null;
        pendingOnAccept = null;
        pendingLines = null;
        pendingLineIndex = 0;
        ResetAcceptButtonState();
        panelRoot.SetActive(false);

        Managers.UI.SetHudVisible(true);
        Managers.Player.SetMovementEnabled(true);
        Managers.Player.mouseLook.SetPointerMode(false);
        enableGameplayInputCts = new CancellationTokenSource();
        EnableGameplayInputAfterPointerReleasedAsync(enableGameplayInputCts.Token).Forget();
    }

    void OnAcceptClicked()
    {
        if (!lineFullyRevealed)
        {
            CompleteTypewriter();
            ResetAcceptButtonState();
            return;
        }

        if (pendingLines != null && pendingLineIndex < pendingLines.Length - 1)
        {
            pendingLineIndex++;
            ShowLine(pendingLines[pendingLineIndex]);
            ResetAcceptButtonState();
            return;
        }

        Action onAccept = pendingOnAccept;
        pendingOnAccept = null;
        pendingLines = null;
        pendingLineIndex = 0;

        if (onAccept != null)
            onAccept();
        else
            Managers.Jobs.OnSpeechAccepted();

        if (pendingLines != null)
            return;

        Close();
    }

    async UniTaskVoid EnableGameplayInputAfterPointerReleasedAsync(CancellationToken cancellationToken)
    {
        try
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                while (mouse.leftButton.isPressed)
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            Managers.Input.UnblockInteraction(this);
        }
        catch (OperationCanceledException)
        {
        }
    }

    void CancelEnableGameplayInputTask()
    {
        if (enableGameplayInputCts == null)
            return;

        enableGameplayInputCts.Cancel();
        enableGameplayInputCts.Dispose();
        enableGameplayInputCts = null;
    }

    void ResetAcceptButtonState()
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == acceptButton.gameObject)
            EventSystem.current.SetSelectedGameObject(null);

        Graphic graphic = acceptButton.targetGraphic;
        if (graphic != null)
            graphic.CrossFadeColor(acceptButton.colors.normalColor, 0f, true, true);
    }
}
