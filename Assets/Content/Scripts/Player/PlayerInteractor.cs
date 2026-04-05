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
        interactAction.action.canceled += InteractButtonCanceled;
    }

    void OnDisable()
    {
        interactAction.action.Disable();
        interactAction.action.performed -= InteractButtonPressed;
        interactAction.action.canceled -= InteractButtonCanceled;
    }

    void Update()
    {
        CheckForInteractable();
    }

    void CheckForInteractable()
    {
        bool hitSomething = Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, interactDistance, ~0, QueryTriggerInteraction.Ignore);

        if (hitSomething)
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                if (interactable != currentInteractable)
                {
                    currentInteractable = interactable;
                    Managers.UI.ShowInteractText(interactable.Prompt);
                }

                return;
            }
        }

        // caching when we actually lost an interactable
        if (currentInteractable != null)
        {
            InteractButtonCanceled(ctx: default);
            currentInteractable = null;
            Managers.UI.HideInteractText();
        }
    }

    void InteractButtonPressed(InputAction.CallbackContext ctx)
    {
        Debug.Log($"Interact button pressed. Has inetractable: {currentInteractable != null}");
        if (currentInteractable == null)
            return;

        currentInteractable.Interact(transform.parent.gameObject);
    }

    void InteractButtonCanceled(InputAction.CallbackContext ctx)
    {
        if (currentInteractable == null)
            return;

        currentInteractable.InteractCanceled(transform.parent.gameObject);
    }

}
