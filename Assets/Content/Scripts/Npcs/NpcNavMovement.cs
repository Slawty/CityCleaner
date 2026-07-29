using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NpcNavMovement : MonoBehaviour
{
    NavMeshAgent agent;
    public Animator animator;
    public bool followPlayerOnStart;
    Transform followTarget;

    [Header("Animation")]
    public string speedParameter = "Speed";
    public float animationSmooth = 10f;
    [Header("Arrival")]
    [SerializeField] float arrivalRotationSpeed = 360f;

    float currentAnimSpeed;

    bool waitingForGround;
    bool wasThrown;
    PickupInteractable pickupInteractable;
    Quaternion? pendingArrivalRotation;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        pickupInteractable = GetComponent<PickupInteractable>();
    }

    void Update()
    {
        if (followPlayerOnStart)
            HandleFollow();

        UpdateArrivalRotation();
        UpdateAnimation();
    }

    void HandleFollow()
    {
        if (followTarget != null && agent.isOnNavMesh)
            agent.SetDestination(followTarget.position);
    }

    void UpdateAnimation()
    {
        float targetSpeed = agent.velocity.magnitude / (agent.speed * 0.5f);

        currentAnimSpeed = Mathf.Lerp(currentAnimSpeed, targetSpeed, Time.deltaTime * animationSmooth);

        animator.SetFloat(speedParameter, currentAnimSpeed);
    }

    public void MoveTo(Vector3 position)
    {
        MoveTo(position, null);
    }

    public void MoveTo(Vector3 position, Transform faceTarget)
    {
        followTarget = null;
        agent.SetDestination(position);

        if (faceTarget == null)
        {
            pendingArrivalRotation = null;
            return;
        }

        pendingArrivalRotation = Quaternion.LookRotation(faceTarget.forward, Vector3.up);
    }

    void UpdateArrivalRotation()
    {
        if (!pendingArrivalRotation.HasValue)
            return;

        if (!HasReachedDestination())
            return;

        Quaternion targetRotation = pendingArrivalRotation.Value;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, arrivalRotationSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRotation) > 0.5f)
            return;

        transform.rotation = targetRotation;
        pendingArrivalRotation = null;
    }

    public void Follow(Transform target)
    {
        followTarget = target;
    }

    public void EnableMovement(bool b)
    {
        agent.enabled = b;
    }

    public void MarkThrown()
    {
        wasThrown = true;
        waitingForGround = true;
    }

    public void CancelWaitingForCollision()
    {
        waitingForGround = false;
        wasThrown = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!waitingForGround)
            return;
        Debug.Log($"Collision with {collision.gameObject.name}");
        waitingForGround = false;

        if (pickupInteractable != null)
        {
            pickupInteractable.EnablePhysics(false);
            pickupInteractable.EnableCollider(true);
        }

        EnableMovement(true);
    }

    public void Stop()
    {
        followTarget = null;
        pendingArrivalRotation = null;
        agent.ResetPath();
    }

    public bool HasReachedDestination()
    {
        if (agent.pathPending) return false;
        if (agent.remainingDistance > agent.stoppingDistance) return false;
        if (agent.hasPath && agent.velocity.sqrMagnitude > 0f) return false;

        return true;
    }
}