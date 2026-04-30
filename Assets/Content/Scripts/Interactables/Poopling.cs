using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class Poopling : MonoBehaviour
{
    public UnityAction OnConsumed;
    public PickupInteractable PickupInteractable { get; private set; }
    public NpcNavMovement Movement { get; private set; }

    [Header("Wander")]
    [SerializeField] bool wanderEnabled = true;
    [SerializeField] float wanderRadius = 3f;
    [SerializeField] float sampleRadius = 1.5f;
    [SerializeField] float idleMin = 1f;
    [SerializeField] float idleMax = 3f;
    [SerializeField] float minMoveDistance = 0.4f;
    [SerializeField] int navSampleTries = 20;
    [Tooltip("Set wander origin to the current position when nav movement is turned on again (e.g. after the player releases a pickup).")]
    [SerializeField] bool reanchorWanderOnMovementResume = true;
    [Tooltip("Skip wandering while this object is parented (e.g. inside a washing machine drum).")]
    [SerializeField] bool pauseWanderWhenParented = true;

    Vector3 wanderCenter;
    bool lastMovementEnabled;
    bool waitingIdle;
    bool hasWanderOrder;
    float idleEndTime;
    bool stoppedWanderForParenting;
    NavMeshAgent navAgent;

    void Awake()
    {
        PickupInteractable = GetComponent<PickupInteractable>();
        Movement = GetComponent<NpcNavMovement>();
        navAgent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        wanderCenter = transform.position;
        lastMovementEnabled = navAgent != null && navAgent.enabled;
        if (wanderEnabled)
            StartIdle();
    }

    void Update()
    {
        if (Movement == null || navAgent == null)
            return;

        if (Movement.followPlayerOnStart)
            return;

        if (!wanderEnabled)
            return;

        if (reanchorWanderOnMovementResume)
        {
            bool en = navAgent.enabled;
            if (en && !lastMovementEnabled)
            {
                wanderCenter = transform.position;
                if (!TryPickRandomWanderPoint(out Vector3 p))
                {
                    StartIdle();
                }
                else
                {
                    SetWanderMove(p);
                }
            }
            lastMovementEnabled = en;
        }

        if (!navAgent.enabled)
            return;

        if (pauseWanderWhenParented && transform.parent != null)
        {
            if (!stoppedWanderForParenting)
            {
                Movement.Stop();
                stoppedWanderForParenting = true;
            }
            return;
        }
        stoppedWanderForParenting = false;

        if (!navAgent.isOnNavMesh)
            return;

        if (waitingIdle)
        {
            if (Time.time < idleEndTime)
                return;

            if (!TryPickRandomWanderPoint(out Vector3 point))
            {
                StartIdle();
                return;
            }
            SetWanderMove(point);
            return;
        }

        if (hasWanderOrder && Movement.HasReachedDestination())
            StartIdle();
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
        Movement.MoveTo(worldPoint);
    }

    bool TryPickRandomWanderPoint(out Vector3 result)
    {
        result = wanderCenter;
        for (int i = 0; i < navSampleTries; i++)
        {
            Vector2 r = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = wanderCenter + new Vector3(r.x, 0f, r.y);
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

    public void SetWanderingEnabled(bool value)
    {
        wanderEnabled = value;
        if (!value)
        {
            if (Movement != null)
            {
                Movement.Stop();
            }
            hasWanderOrder = false;
        }
        else if (navAgent != null && navAgent.enabled)
        {
            StartIdle();
        }
    }

    public void SetWanderCenter(Vector3 worldPosition)
    {
        wanderCenter = worldPosition;
    }

    void OnDestroy()
    {
        OnConsumed?.Invoke();
    }
}
