using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactDistance = 3f;
    public InputActionReference interactAction;
    IInteractable currentInteractable;
    IVacuumable currentVacuumPrompt;

    Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void OnEnable()
    {
        interactAction.action.Enable();
        interactAction.action.performed += InteractButtonPressed;
        interactAction.action.canceled += InteractButtonReleased;
    }

    void OnDisable()
    {
        interactAction.action.Disable();
        interactAction.action.performed -= InteractButtonPressed;
        interactAction.action.canceled -= InteractButtonReleased;
    }

    void Update()
    {
        CheckForInteractable();
    }

    void CheckForInteractable()
    {
        if (Managers.Input.InteractionBlocked())
        {
            ClearPrompts();
            return;
        }

        if (Managers.Tools.IsInVacuumMode)
            return;

        bool hitSomething = Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, interactDistance, ~0, QueryTriggerInteraction.Ignore);

        if (hitSomething)
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null && !string.IsNullOrEmpty(interactable.Prompt))
            {
                if (interactable != currentInteractable)
                {
                    currentVacuumPrompt = null;
                    currentInteractable = interactable;
                    Managers.UI.ShowInteractText(interactable.Prompt);
                }

                return;
            }

            IVacuumable vacuumable = hit.collider.GetComponentInParent<IVacuumable>();
            if (vacuumable != null && vacuumable.CanVacuum)
            {
                if (vacuumable != currentVacuumPrompt)
                {
                    currentInteractable = null;
                    currentVacuumPrompt = vacuumable;
                    Managers.UI.ShowVacuumPrompt(vacuumable.VacuumPrompt);
                }

                return;
            }
        }

        ClearPrompts();
    }

    void ClearPrompts()
    {
        if (currentInteractable == null && currentVacuumPrompt == null)
            return;

        currentInteractable = null;
        currentVacuumPrompt = null;
        Managers.UI.HideInteractText();
    }

    void InteractButtonPressed(InputAction.CallbackContext ctx)
    {
        if (Managers.Input.InteractionBlocked())
            return;

        if (currentInteractable == null)
            return;

        currentInteractable.Interact(transform.parent.gameObject);
    }

    void InteractButtonReleased(InputAction.CallbackContext ctx)
    {
        if (Managers.Input.InteractionBlocked())
            return;

        if (currentInteractable == null)
            return;

        currentInteractable.InteractReleased(transform.parent.gameObject);
    }
}
