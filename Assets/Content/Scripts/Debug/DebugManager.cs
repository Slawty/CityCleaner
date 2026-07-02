using UnityEngine;
using UnityEngine.InputSystem;

public class DebugManager : MonoBehaviour
{
    public bool InstantCleaning;
    [SerializeField] InputActionReference interactAction;

    void Start()
    {
        if (interactAction != null)
            interactAction.action.performed += OnDebugButton01Pressed;
    }

    void OnDestroy()
    {
        if (interactAction != null)
            interactAction.action.performed -= OnDebugButton01Pressed;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
            Managers.Jobs.CompleteActiveJobDebug();

        if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame && Managers.UpgradeMenu != null)
            Managers.UpgradeMenu.Open();
    }

    void OnDebugButton01Pressed(InputAction.CallbackContext context)
    {
        Debug.Log("Debug button pressed (hook tools here if needed).");
    }
}
