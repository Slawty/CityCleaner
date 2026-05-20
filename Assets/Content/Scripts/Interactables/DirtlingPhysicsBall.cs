using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(DirtlingStateController))]
[RequireComponent(typeof(Rigidbody))]
public class DirtlingPhysicsBall : MonoBehaviour
{
    [SerializeField] float recoverAfterSeconds = 2f;
    [SerializeField] float pushForceScale = 8f;
    [SerializeField] float tumbleTorque = 3f;
    [SerializeField] float navSampleRadius = 2f;

    float recoverEndTime;
    DirtlingStateController stateController;
    Dirtling dirtling;
    Rigidbody body;
    NpcNavMovement movement;
    NavMeshAgent navAgent;

    void Awake()
    {
        stateController = GetComponent<DirtlingStateController>();
        dirtling = GetComponent<Dirtling>();
        body = GetComponent<Rigidbody>();
        movement = GetComponent<NpcNavMovement>();
        navAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (stateController.CurrentState != DirtlingState.PhysicsBall)
            return;

        if (Time.time < recoverEndTime)
            return;

        Recover();
    }

    public void BeginBall()
    {
        recoverEndTime = Time.time + recoverAfterSeconds;

        movement.Stop();
        movement.EnableMovement(false);
        navAgent.enabled = false;

        dirtling.SetBodyColliderEnabled(true);
        dirtling.SetPhysicsEnabled(true);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    public void ApplyPush(Vector3 pushDirection, float impulseStrength)
    {
        if (stateController.CurrentState != DirtlingState.PhysicsBall)
            return;

        Launch(pushDirection, impulseStrength * pushForceScale);
    }

    public void Launch(Vector3 direction, float impulse)
    {
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        Vector3 flat = direction;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f)
            flat = transform.forward;

        body.AddForce(flat.normalized * impulse, ForceMode.Impulse);
        body.AddTorque(Random.insideUnitSphere * tumbleTorque, ForceMode.Impulse);
    }

    void Recover()
    {
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        dirtling.SetPhysicsEnabled(false);

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navSampleRadius, NavMesh.AllAreas))
            transform.position = hit.position;

        navAgent.enabled = true;
        movement.EnableMovement(true);

        if (navAgent.isOnNavMesh)
            navAgent.Warp(transform.position);

        stateController.OnPhysicsBallRecovered();
    }
}
