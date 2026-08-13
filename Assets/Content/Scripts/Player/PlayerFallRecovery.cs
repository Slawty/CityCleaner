using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FMODUnity;
using UnityEngine;

public class PlayerFallRecovery : MonoBehaviour
{
    struct SafePoint
    {
        public Vector3 Position;
        public float Yaw;
    }

    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float sampleIntervalSeconds = 0.5f;
    [SerializeField] float minSampleDistance = 1f;
    [SerializeField] float fallYThreshold = -4f;
    [SerializeField] float recoveryCooldownSeconds = 1.5f;
    [SerializeField] float fadeOutDuration = 0.2f;
    [SerializeField] float fadeInDuration = 0.3f;
    [SerializeField] float groundRaycastStartOffset = 0.5f;
    [SerializeField] float groundRaycastDownDistance = 12f;
    [SerializeField] float groundRaycastStepUp = 5f;
    [SerializeField] int groundRaycastUpSteps = 3;
    [SerializeField] float landingHeightOffset = 0.05f;
    [SerializeField] float minGroundNormalY = 0.7f;
    [SerializeField] int maxSafePointCount = 10;
    [SerializeField] bool useFallYThreshold = true;
    [SerializeField] EventReference splashSoundEvent;

    readonly List<SafePoint> safePoints = new();

    SimplePlayerMovement movement;
    CharacterController controller;
    float nextSampleTime;
    Vector3 lastSamplePosition;
    bool hasSamplePosition;
    float recoveryAvailableTime;
    bool isRecovering;
    CancellationTokenSource recoveryCts;

    void Awake()
    {
        movement = GetComponent<SimplePlayerMovement>();
        controller = GetComponent<CharacterController>();

        if (movement == null)
            throw new System.InvalidOperationException($"{nameof(PlayerFallRecovery)} on {name}: {nameof(SimplePlayerMovement)} is required.");

        if (controller == null)
            throw new System.InvalidOperationException($"{nameof(PlayerFallRecovery)} on {name}: {nameof(CharacterController)} is required.");
    }

    void OnDestroy()
    {
        CancelRecovery();
    }

    void Start()
    {
        RecordSafePoint(transform.position, transform.eulerAngles.y);
    }

    void Update()
    {
        TryRecordSafePoint();

        if (useFallYThreshold && Time.time >= recoveryAvailableTime && transform.position.y < fallYThreshold)
            RecoverFromWater();
    }

    void TryRecordSafePoint()
    {
        if (movement.IsClimbing || !controller.isGrounded)
            return;

        bool movedEnough = hasSamplePosition && Vector3.Distance(transform.position, lastSamplePosition) >= minSampleDistance;
        if (Time.time < nextSampleTime && !movedEnough)
            return;

        RecordSafePoint(transform.position, transform.eulerAngles.y);
        nextSampleTime = Time.time + sampleIntervalSeconds;
        lastSamplePosition = transform.position;
        hasSamplePosition = true;
    }

    void RecordSafePoint(Vector3 position, float yaw)
    {
        if (safePoints.Count >= maxSafePointCount)
            safePoints.RemoveAt(0);

        safePoints.Add(new SafePoint { Position = position, Yaw = yaw });
    }

    public void RecoverFromWater()
    {
        if (Time.time < recoveryAvailableTime || isRecovering)
            return;

        CancelRecovery();
        recoveryCts = new CancellationTokenSource();
        RecoverFromWaterAsync(recoveryCts.Token).Forget();
    }

    async UniTaskVoid RecoverFromWaterAsync(CancellationToken cancellationToken)
    {
        isRecovering = true;
        Vector3 fallPosition = transform.position;
        SafePoint safePoint = GetLatestSafePoint();
        Vector3 groundedPosition = ProjectToGround(safePoint.Position);

        PlaySplashSound(fallPosition);

        if (Managers.Tools.IsInVacuumMode)
            Managers.Tools.EndVacuumMode();

        Managers.Tools.StopActiveShooting();
        movement.ResetMovementState();

        Managers.Input.BlockInteraction(this);
        Managers.Player.SetMovementEnabled(false);
        controller.enabled = false;

        try
        {
            ScreenFadeOverlay screenFade = Managers.UI.ScreenFade;
            if (screenFade == null)
                throw new System.InvalidOperationException($"{nameof(PlayerFallRecovery)} on {name}: Screen fade overlay is not assigned on UIManager.");

            await screenFade.FadeToAsync(1f, fadeOutDuration, cancellationToken);

            TeleportTo(safePoint, groundedPosition);

            await screenFade.FadeToAsync(0f, fadeInDuration, cancellationToken);
        }
        catch (System.OperationCanceledException)
        {
            return;
        }
        finally
        {
            controller.enabled = true;
            Managers.Player.SetMovementEnabled(true);
            Managers.Input.UnblockInteraction(this);
            recoveryAvailableTime = Time.time + recoveryCooldownSeconds;
            isRecovering = false;
            CancelRecovery();
        }
    }

    void TeleportTo(SafePoint safePoint, Vector3 groundedPosition)
    {
        transform.SetPositionAndRotation(groundedPosition, Quaternion.Euler(0f, safePoint.Yaw, 0f));

        lastSamplePosition = groundedPosition;
        hasSamplePosition = true;
    }

    void PlaySplashSound(Vector3 position)
    {
        if (splashSoundEvent.IsNull)
            throw new System.InvalidOperationException($"{nameof(PlayerFallRecovery)} on {name}: {nameof(splashSoundEvent)} is not assigned.");

        RuntimeManager.PlayOneShot(splashSoundEvent, position);
    }

    void CancelRecovery()
    {
        if (recoveryCts == null)
            return;

        recoveryCts.Cancel();
        recoveryCts.Dispose();
        recoveryCts = null;
    }

    SafePoint GetLatestSafePoint()
    {
        if (safePoints.Count == 0)
            return new SafePoint { Position = transform.position, Yaw = transform.eulerAngles.y };

        return safePoints[safePoints.Count - 1];
    }

    Vector3 ProjectToGround(Vector3 worldPoint)
    {
        float startY = worldPoint.y + groundRaycastStartOffset;
        int playerLayer = gameObject.layer;

        for (int step = 0; step <= groundRaycastUpSteps; step++)
        {
            Vector3 origin = new Vector3(worldPoint.x, startY + step * groundRaycastStepUp, worldPoint.z);
            if (TryRaycastGround(origin, groundRaycastDownDistance, playerLayer, minGroundNormalY, groundMask, out RaycastHit hit))
                return hit.point + Vector3.up * landingHeightOffset;
        }

        worldPoint.y += landingHeightOffset;
        return worldPoint;
    }

    static bool TryRaycastGround(Vector3 origin, float maxDistance, int playerLayer, float minNormalY, LayerMask mask, out RaycastHit groundHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, maxDistance, mask, QueryTriggerInteraction.Ignore);

        groundHit = default;
        float closestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.layer == playerLayer)
                continue;

            if (hit.normal.y < minNormalY)
                continue;

            if (hit.distance >= closestDistance)
                continue;

            closestDistance = hit.distance;
            groundHit = hit;
        }

        return closestDistance < float.MaxValue;
    }
}
