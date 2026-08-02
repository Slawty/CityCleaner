using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DebugManager : MonoBehaviour
{
    public bool InstantCleaning;
    [SerializeField] InputActionReference pauseAction;

    void OnEnable()
    {
        if (pauseAction == null)
            return;

        pauseAction.action.Enable();
        pauseAction.action.performed += OnPausePressed;
    }

    void OnDisable()
    {
        if (pauseAction == null)
            return;

        pauseAction.action.performed -= OnPausePressed;
        pauseAction.action.Disable();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
            Managers.Jobs.CompleteActiveJobDebug();

        if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame && Managers.UpgradeMenu != null)
            Managers.UpgradeMenu.Open();
    }

    void OnPausePressed(InputAction.CallbackContext context)
    {
        ToggleEditorPause();
    }

    void ToggleEditorPause()
    {
#if UNITY_EDITOR
        EditorApplication.isPaused = !EditorApplication.isPaused;
#else
        Debug.LogWarning($"{nameof(DebugManager)} pause only works in the Unity Editor.");
#endif
    }
}
