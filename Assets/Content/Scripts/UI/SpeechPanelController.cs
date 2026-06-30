using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SpeechPanelController : MonoBehaviour
{
    const string DefaultDialogue = "Please, can you help me?";

    [SerializeField] GameObject panelRoot;
    [SerializeField] TMP_Text dialogueText;
    [SerializeField] Button acceptButton;

    bool isOpen;
    Action pendingOnAccept;
    string[] pendingLines;
    int pendingLineIndex;
    Coroutine enableGameplayInputRoutine;

    void Awake()
    {
        acceptButton.onClick.AddListener(OnAcceptClicked);
        panelRoot.SetActive(false);
    }

    void OnDestroy()
    {
        acceptButton.onClick.RemoveListener(OnAcceptClicked);
        StopEnableGameplayInputRoutine();
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
            dialogueText.text = lines[0];
            ResetAcceptButtonState();
            return;
        }

        OpenPanel(lines[0]);
    }

    void OpenPanel(string text)
    {
        StopEnableGameplayInputRoutine();
        isOpen = true;
        dialogueText.text = text;
        ResetAcceptButtonState();
        panelRoot.SetActive(true);

        Managers.UI.HideInteractText();
        Managers.UI.SetHudVisible(false);
        Managers.Input.BlockInteraction(this);
        Managers.Player.SetMovementEnabled(false);
        Managers.Player.mouseLook.SetPointerMode(true);
        Managers.Tools.StopActiveShooting();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        Managers.Jobs.ClearPendingOffer();
        isOpen = false;
        pendingOnAccept = null;
        pendingLines = null;
        pendingLineIndex = 0;
        ResetAcceptButtonState();
        panelRoot.SetActive(false);

        Managers.UI.SetHudVisible(true);
        Managers.Player.SetMovementEnabled(true);
        Managers.Player.mouseLook.SetPointerMode(false);
        enableGameplayInputRoutine = StartCoroutine(EnableGameplayInputAfterPointerReleased());
    }

    void OnAcceptClicked()
    {
        if (pendingLines != null && pendingLineIndex < pendingLines.Length - 1)
        {
            pendingLineIndex++;
            dialogueText.text = pendingLines[pendingLineIndex];
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

    IEnumerator EnableGameplayInputAfterPointerReleased()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            while (mouse.leftButton.isPressed)
                yield return null;
        }

        yield return null;

        enableGameplayInputRoutine = null;
        Managers.Input.UnblockInteraction(this);
    }

    void StopEnableGameplayInputRoutine()
    {
        if (enableGameplayInputRoutine == null)
            return;

        StopCoroutine(enableGameplayInputRoutine);
        enableGameplayInputRoutine = null;
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
