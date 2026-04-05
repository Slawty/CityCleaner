using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Events;

public class WashingMachineTrigger : MonoBehaviour
{
    public UnityAction<Poopling> OnPooplingStored;
    public UnityAction OnPooplingPickedUp;
    public Transform TargetPosition;
    public Transform machineDrum;
    Collider triggerCol;
    Poopling storedPoopling;

    void Awake()
    {
        triggerCol = GetComponent<Collider>();
    }

    public void EnableCollider(bool b)
    {
        triggerCol.enabled = b;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (storedPoopling != null)
            return;

        Debug.Log($"Thing entered washing machine: {other.name}");

        Poopling checkPoopling = other.GetComponent<Poopling>();

        if (checkPoopling == null)
            return;

        storedPoopling = checkPoopling;
        storedPoopling.PickupInteractable.EnablePhysics(false);
        storedPoopling.PickupInteractable.EnableCollider(true);
        storedPoopling.PickupInteractable.OnInteract += OnPooplingPickUp;
        storedPoopling.transform.parent = machineDrum;
        storedPoopling.transform.DOMove(TargetPosition.position, 0.25f);
        storedPoopling.transform.DORotateQuaternion(TargetPosition.rotation, 0.25f);
        triggerCol.enabled = false;
        OnPooplingStored?.Invoke(storedPoopling);
    }

    void OnPooplingPickUp()
    {
        Debug.Log($"Poopling picked up");
        storedPoopling.PickupInteractable.OnInteract -= OnPooplingPickUp;
        storedPoopling.transform.parent = null;
        triggerCol.enabled = true;
        storedPoopling = null;
        OnPooplingPickedUp?.Invoke();
    }
}
