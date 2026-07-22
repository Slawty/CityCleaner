using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class Vacuum : MonoBehaviour
{
    public LayerMask vacuumMask;
    public float interactDistance = 3f;
    public float shootForce = 12f;
    public Collider particleTriggerCollider;
    [Tooltip("Dirtlings parent here while vacuumed.")]
    public Transform dirtlingAttachPoint;

    [Header("Audio")]
    [SerializeField] EventReference vacuumLoopEvent;

    IVacuumable currentVacuumable;
    Camera cam;
    bool suctionActive;
    bool carryMode;
    EventInstance vacuumLoopInstance;

    public bool HasCarryTarget =>
        currentVacuumable is IVacuumCarryable carryable && carryable.IsAttached;

    void Awake()
    {
        cam = Managers.MainCam;
    }

    void OnDisable()
    {
        StopVacuumLoop();
    }

    void Update()
    {
        if (!suctionActive || carryMode)
            return;

        CheckForVacuumable();
    }

    public void Begin()
    {
        suctionActive = true;
        carryMode = false;
        particleTriggerCollider.enabled = true;
        Managers.Input.BlockInteraction(this);
        StartVacuumLoop();
    }

    public void EnterCarryMode()
    {
        if (!HasCarryTarget)
            return;

        carryMode = true;
        suctionActive = false;
        particleTriggerCollider.enabled = false;
    }

    public void ReleaseCarried()
    {
        if (currentVacuumable is IVacuumCarryable carryable && carryable.IsAttached)
            carryable.ReleaseFromVacuum();
        else if (currentVacuumable != null)
            currentVacuumable.VacuumEnd();

        ClearTarget();
    }

    public void ShootCarried()
    {
        if (currentVacuumable is IVacuumCarryable carryable && carryable.IsAttached)
        {
            Vector3 direction = cam.transform.forward;
            carryable.ShootFromVacuum(direction, shootForce);
            ClearTarget();
            return;
        }

        if (currentVacuumable != null)
            currentVacuumable.VacuumEnd();
        ClearTarget();
    }

    public void End()
    {
        if (currentVacuumable != null)
            currentVacuumable.VacuumEnd();

        currentVacuumable = null;
        suctionActive = false;
        carryMode = false;
        particleTriggerCollider.enabled = false;
        Managers.Input.UnblockInteraction(this);
        StopVacuumLoop();
    }

    void StartVacuumLoop()
    {
        if (vacuumLoopEvent.IsNull)
            throw new System.InvalidOperationException("Vacuum loop FMOD event is not assigned on Vacuum.");

        StopVacuumLoop();

        vacuumLoopInstance = RuntimeManager.CreateInstance(vacuumLoopEvent);
        RuntimeManager.AttachInstanceToGameObject(vacuumLoopInstance, transform);
        vacuumLoopInstance.start();
    }

    void StopVacuumLoop()
    {
        if (!vacuumLoopInstance.isValid())
            return;

        vacuumLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        vacuumLoopInstance.release();
        vacuumLoopInstance.clearHandle();
    }

    void ClearTarget()
    {
        currentVacuumable = null;
        suctionActive = false;
        carryMode = false;
    }

    void CheckForVacuumable()
    {
        if (HasCarryTarget)
            return;

        bool hitSomething = Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, interactDistance, vacuumMask, QueryTriggerInteraction.Ignore);

        if (hitSomething)
        {
            IVacuumable vacuumable = hit.collider.GetComponentInParent<IVacuumable>();

            if (vacuumable != null && vacuumable.CanVacuum)
            {
                if (vacuumable is DirtlingVacuumCapture capture && dirtlingAttachPoint != null)
                    capture.BindVacuumAttachPoint(dirtlingAttachPoint);

                if (vacuumable != currentVacuumable)
                {
                    if (currentVacuumable != null)
                        currentVacuumable.VacuumEnd();
                    currentVacuumable = vacuumable;
                    currentVacuumable.VacuumStart();
                }

                return;
            }
        }

        if (currentVacuumable != null && !HasCarryTarget)
        {
            currentVacuumable.VacuumEnd();
            currentVacuumable = null;
        }
    }
}
