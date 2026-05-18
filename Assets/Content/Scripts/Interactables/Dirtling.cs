using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(DirtlingStateController))]
[RequireComponent(typeof(DirtlingWander))]
[RequireComponent(typeof(Rigidbody))]
public class Dirtling : MonoBehaviour
{
    public UnityAction OnConsumed;
    public NpcNavMovement Movement { get; private set; }
    public DirtlingStateController StateController { get; private set; }

    Rigidbody body;
    Collider bodyCollider;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<Collider>();
        StateController = GetComponent<DirtlingStateController>();
        Movement = GetComponent<NpcNavMovement>();
    }

    public void SetWanderingEnabled(bool value)
    {
        StateController.SetWanderingEnabled(value);
    }

    public void SetWanderCenter(Vector3 worldPosition)
    {
        GetComponent<DirtlingWander>().SetWanderCenter(worldPosition);
    }

    public void SetPhysicsEnabled(bool physicsEnabled)
    {
        body.isKinematic = !physicsEnabled;
    }

    public void SetBodyColliderEnabled(bool enabled)
    {
        bodyCollider.enabled = enabled;
    }

    public void PrepareForMachineStorage()
    {
        SetWanderingEnabled(false);
        Movement.EnableMovement(false);
        Movement.CancelWaitingForCollision();
        SetPhysicsEnabled(false);
        SetBodyColliderEnabled(false);
        StateController.EnterState(DirtlingState.Processed);
    }

    public void ReleaseFromMachineStorage()
    {
        transform.SetParent(null);
        Movement.EnableMovement(true);
        SetPhysicsEnabled(false);
        SetBodyColliderEnabled(true);
        SetWanderingEnabled(true);
        StateController.EnterState(DirtlingState.Wandering);
    }

    void OnDestroy()
    {
        OnConsumed?.Invoke();
    }
}
