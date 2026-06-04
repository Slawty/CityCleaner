using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SpeechPanelController : MonoBehaviour
{
    const string DefaultDialogue = "Please, can you help me?";

    [SerializeField] GameObject panelRoot;
    [SerializeField] TMP_Text dialogueText;
    [SerializeField] Button acceptButton;

    bool isOpen;
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

    public void Show(string text = null)
    {
        if (isOpen)
            return;

        StopEnableGameplayInputRoutine();
        isOpen = true;
        dialogueText.text = string.IsNullOrEmpty(text) ? DefaultDialogue : text;
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
        panelRoot.SetActive(false);

        Managers.UI.SetHudVisible(true);
        Managers.Player.SetMovementEnabled(true);
        Managers.Player.mouseLook.SetPointerMode(false);
        enableGameplayInputRoutine = StartCoroutine(EnableGameplayInputAfterPointerReleased());
    }

    void OnAcceptClicked()
    {
        Managers.Jobs.OnSpeechAccepted();
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
}
