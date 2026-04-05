using UnityEngine;
using UnityEngine.Events;

public class PickupInteractable : MonoBehaviour, IInteractable
{
    public UnityAction OnInteract;
    public string Prompt => "Pick up";

    Rigidbody rb;
    Collider col;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();
    }

    public void Interact(GameObject interactor)
    {
        PlayerPickupController playerPickup = interactor.GetComponentInChildren<PlayerPickupController>();

        if (playerPickup == null)
            return;

        OnInteract?.Invoke();
        playerPickup.HoldObject(this);
    }

    public void EnableCollider(bool b)
    {
        col.enabled = b;
    }

    public void EnablePhysics(bool b)
    {
        rb.isKinematic = !b;
        col.enabled = b;
    }

    public void InteractCanceled(GameObject interactor)
    {
    }
}