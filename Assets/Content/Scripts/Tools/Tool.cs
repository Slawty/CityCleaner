using UnityEngine;
using UnityEngine.InputSystem;

public abstract class Tool : MonoBehaviour
{
    public Transform Tip;
    [SerializeField] private InputActionReference shootAction;

    protected virtual void OnEnable()
    {
        shootAction.action.performed += HandleShootDown;
        shootAction.action.canceled += HandleShootUp;
    }

    protected virtual void OnDisable()
    {
        shootAction.action.performed -= HandleShootDown;
        shootAction.action.canceled -= HandleShootUp;
        OnShootStop();
    }

    private void HandleShootDown(InputAction.CallbackContext ctx)
    {
        if (Managers.Input.InteractionBlocked())
            return;

        OnShootStart();
    }

    private void HandleShootUp(InputAction.CallbackContext ctx)
    {
        if (Managers.Input.InteractionBlocked())
            return;

        OnShootStop();
    }

    public virtual void Initialize() { }

    public void StopShooting()
    {
        OnShootStop();
    }

    // These are what tools override
    protected virtual void OnShootStart() { }
    protected virtual void OnShootStop() { }
}
