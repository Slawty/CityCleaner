using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class Vacuum : MonoBehaviour
{
    public LayerMask vacuumMask;
    public float interactDistance = 3f;
    public InputActionReference interactAction;
    public Collider particleTriggerCollider;
    IVacuumable currentVacuumable;
    Camera cam;
    bool vacuumActive;

    void Start()
    {
        cam = Managers.MainCam;
    }

    void Update()
    {
        if (!vacuumActive)
            return;

        CheckForVacuumable();
    }

    void CheckForVacuumable()
    {
        bool hitSomething = Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, interactDistance, vacuumMask, QueryTriggerInteraction.Ignore);

        if (hitSomething)
        {
            IVacuumable vacuumable = hit.collider.GetComponent<IVacuumable>();

            if (vacuumable != null)
            {
                if (vacuumable != currentVacuumable)
                {
                    if (currentVacuumable != null)
                        currentVacuumable.VacuumEnd();
                    currentVacuumable = vacuumable;
                    currentVacuumable.VacuumStart();
                }

                return;
            }
        }

        // caching when we actually lost an interactable
        if (currentVacuumable != null)
        {
            currentVacuumable.VacuumEnd();
            currentVacuumable = null;

        }
    }

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
            vacuumActive = true;
            particleTriggerCollider.enabled = true;
            Managers.Input.BlockInteraction(this);
        }
    }

    void InteractButtonCanceled(InputAction.CallbackContext ctx)
    {
        Debug.Log($"Interact button released. Stopping Vacuum");
        vacuumActive = false;
        particleTriggerCollider.enabled = false;
        if (currentVacuumable != null)
            currentVacuumable.VacuumEnd();
        currentVacuumable = null;
        Managers.Input.UnblockInteraction(this);
    }

}
