using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SettingsMenuController : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] SettingsSliderRow masterVolumeRow;
    [SerializeField] SettingsSliderRow mouseSensitivityRow;
    [SerializeField] SettingsMenuButton resumeButton;
    [SerializeField] SettingsMenuButton exitButton;
    [SerializeField] string startSceneName = "Start Scene";

    bool isOpen;
    float previousTimeScale = 1f;
    Coroutine enableGameplayInputRoutine;

    public bool IsOpen => isOpen;

    void Awake()
    {
        GameSettings.Load();

        if (panelRoot != null)
            panelRoot.SetActive(false);

        masterVolumeRow.SetLabel("Master Volume");
        masterVolumeRow.Configure(0f, 1f, GameSettings.MasterVolume, GameSettings.SetMasterVolume);

        mouseSensitivityRow.SetLabel("Mouse Sensitivity");
        mouseSensitivityRow.Configure(
            GameSettings.MinSensitivity,
            GameSettings.MaxSensitivity,
            GameSettings.MouseSensitivity,
            GameSettings.SetMouseSensitivity);

        resumeButton.SetLabel("Resume");
        resumeButton.SetClickHandler(Close);

        if (exitButton == null)
            throw new System.InvalidOperationException($"{nameof(SettingsMenuController)} on {name}: {nameof(exitButton)} is not assigned.");

        exitButton.SetLabel("Exit");
        exitButton.SetClickHandler(ExitToStartScene);
    }

    void Start()
    {
        GameSettings.ApplyAll();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            return;

        if (isOpen)
        {
            Close();
            return;
        }

        if (Managers.Input.InteractionBlocked())
            return;

        Open();
    }

    void OnDestroy()
    {
        StopEnableGameplayInputRoutine();

        if (isOpen)
            Time.timeScale = previousTimeScale;
    }

    public void Open()
    {
        if (isOpen)
            return;

        isOpen = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        panelRoot.SetActive(true);
        Managers.UI.HideInteractText();
        Managers.UI.SetHudVisible(false);
        Managers.Input.BlockInteraction(this);
        Managers.Player.SetMovementEnabled(false);
        Managers.Player.mouseLook.SetPointerMode(true);
        Managers.Tools.StopActiveShooting();
    }

    void ExitToStartScene()
    {
        StopEnableGameplayInputRoutine();
        isOpen = false;
        GameRestart.LoadStartScene(startSceneName);
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        Time.timeScale = previousTimeScale;
        panelRoot.SetActive(false);

        Managers.UI.SetHudVisible(true);
        Managers.Player.SetMovementEnabled(true);
        Managers.Player.mouseLook.SetPointerMode(false);
        enableGameplayInputRoutine = StartCoroutine(EnableGameplayInputAfterPointerReleased());
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
