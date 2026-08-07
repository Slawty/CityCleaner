using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactDistance = 3f;
    public InputActionReference interactAction;
    private IInteractable currentInteractable;

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
            if (currentInteractable != null)
            {
                currentInteractable = null;
                Managers.UI.HideInteractText();
            }

            return;
        }

        bool hitSomething = Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, interactDistance, ~0, QueryTriggerInteraction.Ignore);

        if (hitSomething)
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null && !string.IsNullOrEmpty(interactable.Prompt))
            {
                if (interactable != currentInteractable)
                {
                    currentInteractable = interactable;
                    Debug.Log($"Found interactable: {hit.collider.name}");

                    if (string.IsNullOrEmpty(interactable.Prompt))
                        Managers.UI.HideInteractText();
                    else
                        Managers.UI.ShowInteractText(interactable.Prompt);
                }

                return;
            }
        }

        // caching when we actually lost an interactable
        if (currentInteractable != null && !Managers.Input.InteractionBlocked())
        {
            currentInteractable = null;
            Managers.UI.HideInteractText();
        }
    }

    void InteractButtonPressed(InputAction.CallbackContext ctx)
    {
        if (Managers.Input.InteractionBlocked())
            return;

        Debug.Log($"Interact button pressed. Has inetractable: {currentInteractable != null}");
        if (currentInteractable == null)
            return;

        currentInteractable.Interact(transform.parent.gameObject);
    }

    void InteractButtonReleased(InputAction.CallbackContext ctx)
    {
        if (Managers.Input.InteractionBlocked())
            return;

        Debug.Log($"Interact button released. Has inetractable: {currentInteractable != null}");
        if (currentInteractable == null)
            return;

        currentInteractable.InteractReleased(transform.parent.gameObject);
    }

}
