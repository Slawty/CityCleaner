using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NpcNavMovement))]
[RequireComponent(typeof(NavMeshAgent))]
public class NpcWander : MonoBehaviour
{
    [Header("Wander")]
    [SerializeField] bool wanderEnabledOnStart = true;
    [SerializeField] float wanderRadius = 3f;
    [SerializeField] float sampleRadius = 1.5f;
    [SerializeField] float idleMin = 1f;
    [SerializeField] float idleMax = 3f;
    [SerializeField] float minMoveDistance = 0.4f;
    [SerializeField] int navSampleTries = 20;
    [SerializeField] float stuckTimeout = 2.5f;
    [SerializeField] float stuckProgressThreshold = 0.05f;
    [SerializeField] bool reanchorWanderOnMovementResume = true;

    Vector3 wanderCenter;
    bool wanderingEnabled = true;
    bool lastMovementEnabled;
    bool waitingIdle;
    bool hasWanderOrder;
    float idleEndTime;
    float wanderProgressTime;
    Vector3 wanderProgressPosition;

    NpcNavMovement movement;
    NavMeshAgent navAgent;

    void Awake()
    {
        movement = GetComponent<NpcNavMovement>();
        navAgent = GetComponent<NavMeshAgent>();
    }

    public bool InitialWanderingEnabled => wanderEnabledOnStart;

    public void Initialize()
    {
        wanderCenter = transform.position;
        lastMovementEnabled = navAgent.enabled;
        wanderingEnabled = wanderEnabledOnStart;
    }

    void Update()
    {
        if (!wanderingEnabled)
            return;

        if (movement.followPlayerOnStart)
            return;

        if (reanchorWanderOnMovementResume)
        {
            bool enabled = navAgent.enabled;
            if (enabled && !lastMovementEnabled)
            {
                wanderCenter = transform.position;
                BeginWandering();
            }
            lastMovementEnabled = enabled;
        }

        if (!navAgent.enabled || !navAgent.isOnNavMesh)
            return;

        if (waitingIdle)
        {
            if (Time.time < idleEndTime)
                return;

            BeginWandering();
            return;
        }

        if (!hasWanderOrder)
            return;

        if (movement.HasReachedDestination())
        {
            StartIdle();
            return;
        }

        if (navAgent.pathPending)
            return;

        if (IsMakingWanderProgress())
        {
            RecordWanderProgress();
            return;
        }

        if (Time.time - wanderProgressTime < stuckTimeout)
            return;

        if (!TryPickRandomWanderPoint(out Vector3 newPoint))
        {
            StartIdle();
            return;
        }

        SetWanderMove(newPoint);
    }

    public void BeginWandering()
    {
        if (!wanderingEnabled)
            return;

        if (!TryPickRandomWanderPoint(out Vector3 point))
            StartIdle();
        else
            SetWanderMove(point);
    }

    public void StopWandering()
    {
        hasWanderOrder = false;
        waitingIdle = false;
    }

    public void SetWanderCenter(Vector3 worldPosition)
    {
        wanderCenter = worldPosition;
    }

    public void SetWanderRadius(float radius)
    {
        wanderRadius = radius;
    }

    public void SetWanderingEnabled(bool value)
    {
        wanderingEnabled = value;

        if (!value)
        {
            StopWandering();
            movement.Stop();
            return;
        }

        if (navAgent.enabled)
            BeginWandering();
    }

    void StartIdle()
    {
        hasWanderOrder = false;
        waitingIdle = true;
        idleEndTime = Time.time + Random.Range(idleMin, idleMax);
    }

    void SetWanderMove(Vector3 worldPoint)
    {
        waitingIdle = false;
        hasWanderOrder = true;
        RecordWanderProgress();
        movement.MoveTo(worldPoint);
    }

    void RecordWanderProgress()
    {
        wanderProgressTime = Time.time;
        wanderProgressPosition = transform.position;
    }

    bool IsMakingWanderProgress()
    {
        float thresholdSq = stuckProgressThreshold * stuckProgressThreshold;
        if (navAgent.velocity.sqrMagnitude > thresholdSq)
            return true;
        return (transform.position - wanderProgressPosition).sqrMagnitude > thresholdSq;
    }

    bool TryPickRandomWanderPoint(out Vector3 result)
    {
        result = wanderCenter;
        for (int i = 0; i < navSampleTries; i++)
        {
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = wanderCenter + new Vector3(offset.x, 0f, offset.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            {
                if (Vector3.SqrMagnitude(hit.position - transform.position) < minMoveDistance * minMoveDistance)
                    continue;
                result = hit.position;
                return true;
            }
        }
        return false;
    }
}
