using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
    PickupInteractable pickupInteractable;
    Quaternion? pendingArrivalRotation;
    CancellationTokenSource facingCts;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        pickupInteractable = GetComponent<PickupInteractable>();
    }

    void OnDestroy()
    {
        CancelFacing();
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
        CancelFacing();
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

        CancelFacing();

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
        waitingForGround = true;
    }

    public void CancelWaitingForCollision()
    {
        waitingForGround = false;
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
        CancelFacing();
        agent.ResetPath();
    }

    public bool HasReachedDestination()
    {
        if (agent.pathPending) return false;
        if (agent.remainingDistance > agent.stoppingDistance) return false;
        if (agent.hasPath && agent.velocity.sqrMagnitude > 0f) return false;

        return true;
    }

    public UniTask WaitUntilArrivedAsync(CancellationToken cancellationToken = default)
    {
        return UniTask.WaitUntil(HasFullyArrived, cancellationToken: cancellationToken);
    }

    bool HasFullyArrived()
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return true;

        return HasReachedDestination() && !pendingArrivalRotation.HasValue;
    }

    public void CancelFacing()
    {
        if (facingCts == null)
            return;

        facingCts.Cancel();
        facingCts.Dispose();
        facingCts = null;
    }

    public async UniTask FacePointAsync(Vector3 worldPoint, CancellationToken cancellationToken = default)
    {
        pendingArrivalRotation = null;

        Vector3 direction = worldPoint - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        await FaceRotationAsync(targetRotation, cancellationToken);
    }

    public async UniTask FaceRotationAsync(Quaternion targetRotation, CancellationToken cancellationToken = default)
    {
        pendingArrivalRotation = null;

        CancellationTokenSource rotationCts = CreateFacingCts(cancellationToken);
        try
        {
            await RotateTowardsAsync(targetRotation, rotationCts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (facingCts == rotationCts)
            {
                facingCts.Dispose();
                facingCts = null;
            }
        }
    }

    CancellationTokenSource CreateFacingCts(CancellationToken externalToken)
    {
        CancelFacing();
        return facingCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken, destroyCancellationToken);
    }

    async UniTask RotateTowardsAsync(Quaternion targetRotation, CancellationToken cancellationToken)
    {
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.5f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, arrivalRotationSpeed * Time.deltaTime);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        transform.rotation = targetRotation;
    }
}