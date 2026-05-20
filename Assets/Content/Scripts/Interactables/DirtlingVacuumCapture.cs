using UnityEngine;

[RequireComponent(typeof(DirtlingStateController))]
[RequireComponent(typeof(DirtlingPhysicsBall))]
public class DirtlingVacuumCapture : MonoBehaviour, IVacuumCarryable
{
    [SerializeField] Vector3 attachLocalPosition;
    [SerializeField] Vector3 attachLocalEuler;
    [SerializeField] float wriggleDegrees = 8f;
    [SerializeField] float wriggleSpeed = 12f;

    DirtlingStateController stateController;
    Dirtling dirtling;
    DirtlingPhysicsBall physicsBall;
    Transform defaultAttachPoint;
    Vector3 baseLocalEuler;
    bool isAttached;

    public bool IsAttached => isAttached;

    public bool CanVacuum => stateController.IsDizzy
        && stateController.CurrentState != DirtlingState.Vacuumed
        && stateController.CurrentState != DirtlingState.Processed;

    void Awake()
    {
        stateController = GetComponent<DirtlingStateController>();
        dirtling = GetComponent<Dirtling>();
        physicsBall = GetComponent<DirtlingPhysicsBall>();
    }

    void Update()
    {
        if (!isAttached)
            return;

        float wobble = Mathf.Sin(Time.time * wriggleSpeed) * wriggleDegrees;
        transform.localRotation = Quaternion.Euler(baseLocalEuler.x, baseLocalEuler.y + wobble, baseLocalEuler.z);
    }

    public void BindVacuumAttachPoint(Transform attachPoint)
    {
        defaultAttachPoint = attachPoint;
    }

    public void VacuumStart()
    {
        if (!CanVacuum || defaultAttachPoint == null)
            return;

        isAttached = true;
        baseLocalEuler = attachLocalEuler;
        stateController.EnterState(DirtlingState.Vacuumed);
        stateController.Movement.EnableMovement(false);

        transform.SetParent(defaultAttachPoint);
        transform.localPosition = attachLocalPosition;
        transform.localRotation = Quaternion.Euler(attachLocalEuler);
    }

    public void VacuumEnd()
    {
        ReleaseFromVacuum();
    }

    public void ReleaseFromVacuum()
    {
        if (!isAttached)
            return;

        isAttached = false;
        transform.SetParent(null);
        stateController.Movement.EnableMovement(true);
        if (stateController.IsDizzy)
            stateController.EnterState(DirtlingState.Dizzy);
        else
            stateController.EnterState(DirtlingState.Wandering);
    }

    public void ShootFromVacuum(Vector3 direction, float force)
    {
        if (!isAttached)
            return;

        isAttached = false;
        transform.SetParent(null);
        dirtling.SetBodyColliderEnabled(true);
        stateController.EnterState(DirtlingState.PhysicsBall);
        physicsBall.Launch(direction, force);
    }
}
