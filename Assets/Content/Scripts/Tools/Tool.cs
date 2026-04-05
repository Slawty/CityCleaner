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
    }

    private void HandleShootDown(InputAction.CallbackContext ctx)
    {
        OnShootStart();
    }

    private void HandleShootUp(InputAction.CallbackContext ctx)
    {
        OnShootStop();
    }

    // These are what tools override
    protected virtual void OnShootStart() { }
    protected virtual void OnShootStop() { }
}
