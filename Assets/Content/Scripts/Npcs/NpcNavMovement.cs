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

    float currentAnimSpeed;

    bool waitingForGround;
    bool wasThrown;
    PickupInteractable pickupInteractable;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        pickupInteractable = GetComponent<PickupInteractable>();
    }

    void Update()
    {
        if (followPlayerOnStart)
            HandleFollow();

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
        followTarget = null;
        agent.SetDestination(position);
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