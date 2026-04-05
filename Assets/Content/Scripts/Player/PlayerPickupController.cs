using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickupController : MonoBehaviour
{
    public Transform HoldPoint;
    public InputActionReference throwAction;
    public InputActionReference releaseAction;

    public float moveSpeed = 15f;
    public float throwForce = 10f;

    PickupInteractable heldObject;

    void Update()
    {
        if (heldObject == null)
            return;

        Transform obj = heldObject.transform;

        obj.position = Vector3.Lerp(obj.position, HoldPoint.position, moveSpeed * Time.deltaTime);

        obj.rotation = Quaternion.Lerp(
            obj.rotation,
            Quaternion.LookRotation(-Camera.main.transform.forward),
            moveSpeed * Time.deltaTime
        );
    }

    void BindInput()
    {
        throwAction.action.performed += OnThrow;
        releaseAction.action.performed += OnRelease;
    }

    void UnbindInput()
    {
        throwAction.action.performed -= OnThrow;
        releaseAction.action.performed -= OnRelease;
    }

    public void HoldObject(PickupInteractable obj)
    {
        heldObject = obj;

        obj.EnablePhysics(false);

        BindInput();
    }

    void OnRelease(InputAction.CallbackContext ctx)
    {
        if (heldObject == null)
            return;

        heldObject.EnablePhysics(true);
        heldObject = null;

        UnbindInput();
    }

    void OnThrow(InputAction.CallbackContext ctx)
    {
        if (heldObject == null)
            return;

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();

        heldObject.EnablePhysics(true);

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(Camera.main.transform.forward * throwForce, ForceMode.Impulse);

        heldObject = null;

        UnbindInput();
    }
}