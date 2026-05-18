using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public class WashingMachineTrigger : MonoBehaviour
{
    public UnityAction<Dirtling> OnDirtlingStored;
    public UnityAction OnDirtlingReleased;
    public Transform TargetPosition;
    public Transform machineDrum;
    Collider triggerCol;
    Dirtling storedDirtling;

    void Awake()
    {
        triggerCol = GetComponent<Collider>();
    }

    public void EnableCollider(bool b)
    {
        triggerCol.enabled = b;
    }

    public bool HasStoredDirtling => storedDirtling != null;

    public void ReleaseStoredDirtling()
    {
        if (storedDirtling == null)
            return;

        Dirtling dirtling = storedDirtling;
        storedDirtling = null;
        dirtling.ReleaseFromMachineStorage();
        triggerCol.enabled = true;
        OnDirtlingReleased?.Invoke();
    }

    void OnTriggerEnter(Collider other)
    {
        if (storedDirtling != null)
            return;

        Dirtling dirtling = other.GetComponent<Dirtling>();
        if (dirtling == null)
            return;

        storedDirtling = dirtling;
        storedDirtling.PrepareForMachineStorage();
        storedDirtling.transform.parent = machineDrum;
        storedDirtling.transform.DOMove(TargetPosition.position, 0.25f);
        storedDirtling.transform.DORotateQuaternion(TargetPosition.rotation, 0.25f);
        triggerCol.enabled = false;
        OnDirtlingStored?.Invoke(storedDirtling);
    }
}
