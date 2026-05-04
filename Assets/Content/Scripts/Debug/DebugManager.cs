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

    void OnDebugButton01Pressed(InputAction.CallbackContext context)
    {
        Debug.Log("Debug button pressed (hook tools here if needed).");
    }
}
