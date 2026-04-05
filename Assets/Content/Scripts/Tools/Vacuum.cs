using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class Vacuum : MonoBehaviour
{

    public InputActionReference interactAction;
    public Collider particleTriggerCollider;

    void OnEnable()
    {
        interactAction.action.Enable();
        interactAction.action.performed += InteractButtonHold;
        interactAction.action.canceled += InteractButtonCanceled;
    }

    void OnDisable()
    {
        interactAction.action.Disable();
        interactAction.action.performed -= InteractButtonHold;
        interactAction.action.canceled -= InteractButtonCanceled;
    }

    void InteractButtonHold(InputAction.CallbackContext ctx)
    {
        if (ctx.interaction is HoldInteraction)
        {
            Debug.Log($"Interact button hold. Activate Vacuum");
            particleTriggerCollider.enabled = true;
        }
    }

    void InteractButtonCanceled(InputAction.CallbackContext ctx)
    {
        Debug.Log($"Interact button released. Stopping Vacuum");
        particleTriggerCollider.enabled = false;
    }

}
