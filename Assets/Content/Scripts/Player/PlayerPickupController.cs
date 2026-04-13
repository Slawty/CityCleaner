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
        Transform objHold = heldObject.HoldPoint;

        Quaternion targetRot = Quaternion.LookRotation(-Camera.main.transform.forward);

        Vector3 holdOffset = obj.position - objHold.position;
        Vector3 targetPos = HoldPoint.position + holdOffset;

        obj.position = Vector3.Lerp(obj.position, targetPos, moveSpeed * Time.deltaTime);

        obj.rotation = Quaternion.Lerp(obj.rotation, targetRot, moveSpeed * Time.deltaTime);
    }

    void BindInput()
    {
        throwAction.action.performed += OnThrow;
        releaseAction.action.canceled += OnRelease;
    }

    void UnbindInput()
    {
        throwAction.action.performed -= OnThrow;
        releaseAction.action.canceled -= OnRelease;
    }

    public void HoldObject(PickupInteractable obj)
    {
        heldObject = obj;

        obj.EnablePhysics(false);

        if (obj.TryGetComponent(out NpcNavMovement npc))
        {
            npc.EnableMovement(false);
        }

        BindInput();
    }

    void OnRelease(InputAction.CallbackContext ctx)
    {
        if (heldObject == null)
            return;

        if (heldObject.TryGetComponent(out NpcNavMovement npc))
        {
            npc.EnableMovement(true);
        }

        heldObject.EnablePhysics(false);
        heldObject.EnableCollider(true);
        heldObject = null;

        UnbindInput();
    }

    void OnThrow(InputAction.CallbackContext ctx)
    {
        if (heldObject == null)
            return;

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();

        heldObject.EnablePhysics(true);
        heldObject.EnableCollider(true);

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(Camera.main.transform.forward * throwForce, ForceMode.Impulse);

        if (heldObject.TryGetComponent(out NpcNavMovement npc))
        {
            npc.MarkThrown();
        }

        heldObject = null;

        UnbindInput();
    }
}