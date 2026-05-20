using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[RequireComponent(typeof(DirtlingStateController))]
public class DirtlingFlee : MonoBehaviour
{
    [Tooltip("Seconds after the last water hit before leaving flee and returning to wander or dizzy.")]
    [FormerlySerializedAs("fleeDuration")]
    [SerializeField] float calmDownAfterWater = 2f;
    [SerializeField] float fleeMoveDistance = 4f;
    [SerializeField] float sampleRadius = 2f;

    float calmDownEndTime;
    bool pendingFleeMove;
    DirtlingStateController stateController;
    NpcNavMovement movement;
    NavMeshAgent navAgent;

    void Awake()
    {
        stateController = GetComponent<DirtlingStateController>();
        movement = GetComponent<NpcNavMovement>();
        navAgent = GetComponent<NavMeshAgent>();
    }

    void OnEnable()
    {
        pendingFleeMove = true;
        NotifyWaterHit();
    }

    public void NotifyWaterHit()
    {
        calmDownEndTime = Time.time + calmDownAfterWater;
    }

    void Update()
    {
        if (stateController.CurrentState != DirtlingState.Fleeing)
            return;

        if (Time.time >= calmDownEndTime)
        {
            stateController.OnFleeEnded();
            return;
        }

        if (pendingFleeMove || movement.HasReachedDestination())
            TryMoveAwayFromPlayer();
    }

    void TryMoveAwayFromPlayer()
    {
        if (Managers.Player == null)
            return;

        pendingFleeMove = false;
        MoveAwayFromPlayer();
    }

    void MoveAwayFromPlayer()
    {
        Transform player = Managers.Player.transform;
        Vector3 away = transform.position - player.position;
        away.y = 0f;

        if (away.sqrMagnitude < 0.01f)
            away = new Vector3(Random.value - 0.5f, 0f, Random.value - 0.5f);

        Vector3 candidate = transform.position + away.normalized * fleeMoveDistance;
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            movement.MoveTo(hit.position);
    }
}
