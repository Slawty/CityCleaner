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

    [Header("Effects")]
    [SerializeField] ParticleSystem[] vacuumEffects;
    [SerializeField] ParticleSystem waterRefillEffect;

    IVacuumable currentVacuumable;
    IVacuumable promptVacuumable;
    Camera cam;
    bool suctionActive;
    bool carryMode;
    bool waterRefillEffectPlaying;
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
        StopVacuumEffects();
        StopWaterRefillEffect();
        ClearVacuumPrompt();
    }

    void Update()
    {
        if (carryMode)
            return;

        if (suctionActive)
            CheckForVacuumable();
    }

    public void Begin()
    {
        suctionActive = true;
        carryMode = false;
        particleTriggerCollider.enabled = true;
        Managers.Input.BlockInteraction(this);
        StartVacuumLoop();
        StartVacuumEffects();
    }

    public void EnterCarryMode()
    {
        if (!HasCarryTarget)
            return;

        carryMode = true;
        suctionActive = false;
        particleTriggerCollider.enabled = false;
        ClearVacuumPrompt();
    }

    public void ReleaseCarried()
    {
        if (currentVacuumable is IVacuumCarryable carryable && carryable.IsAttached)
            carryable.ReleaseFromVacuum();
        else
            StopVacuumingTarget();

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

        StopVacuumingTarget();
        ClearTarget();
    }

    public void End()
    {
        StopVacuumingTarget();
        suctionActive = false;
        carryMode = false;
        particleTriggerCollider.enabled = false;
        Managers.Input.UnblockInteraction(this);
        StopVacuumLoop();
        StopVacuumEffects();
        ClearVacuumPrompt();
    }

    void StartVacuumLoop()
    {
        if (vacuumLoopEvent.IsNull)
            throw new System.InvalidOperationException("Vacuum loop FMOD event is not assigned on Vacuum.");

        StopVacuumLoop();

        vacuumLoopInstance = RuntimeManager.CreateInstance(vacuumLoopEvent);
        RuntimeManager.AttachInstanceToGameObject(vacuumLoopInstance, gameObject);
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

    void StartVacuumEffects()
    {
        if (vacuumEffects == null)
            return;

        foreach (ParticleSystem effect in vacuumEffects)
        {
            if (effect != null)
                effect.Play();
        }
    }

    void StopVacuumEffects()
    {
        if (vacuumEffects == null)
            return;

        foreach (ParticleSystem effect in vacuumEffects)
        {
            if (effect != null)
                effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    void ClearTarget()
    {
        StopWaterRefillEffect();
        currentVacuumable = null;
        suctionActive = false;
        carryMode = false;
        ClearVacuumPrompt();
    }

    void CheckForVacuumable()
    {
        if (HasCarryTarget)
            return;

        bool hitSomething = Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, interactDistance, vacuumMask, QueryTriggerInteraction.Ignore);

        if (hitSomething)
        {
            IVacuumable vacuumable = hit.collider.GetComponentInParent<IVacuumable>();

            if (vacuumable != null)
            {
                if (vacuumable.CanVacuum)
                {
                    if (vacuumable is DirtlingVacuumCapture capture && dirtlingAttachPoint != null)
                        capture.BindVacuumAttachPoint(dirtlingAttachPoint);

                    ShowVacuumPrompt(vacuumable);

                    if (vacuumable != currentVacuumable)
                    {
                        StartVacuumingTarget(vacuumable);
                    }

                    return;
                }

                if (vacuumable == currentVacuumable)
                    StopVacuumingTarget();

                ClearVacuumPrompt();
                return;
            }
        }

        if (currentVacuumable != null && !HasCarryTarget)
            StopVacuumingTarget();

        ClearVacuumPrompt();
    }

    void StartVacuumingTarget(IVacuumable vacuumable)
    {
        StopVacuumingTarget();

        currentVacuumable = vacuumable;
        currentVacuumable.VacuumStart();

        if (IsWaterRefillSource(vacuumable))
            StartWaterRefillEffect();
    }

    void StopVacuumingTarget()
    {
        if (currentVacuumable == null)
            return;

        if (IsWaterRefillSource(currentVacuumable))
            StopWaterRefillEffect();

        currentVacuumable.VacuumEnd();
        currentVacuumable = null;
    }

    static bool IsWaterRefillSource(IVacuumable vacuumable)
    {
        if (vacuumable is WaterRefill)
            return true;

        return vacuumable is Dripling dripling && dripling.Type == Dripling.ConsumableType.Water;
    }

    void StartWaterRefillEffect()
    {
        if (waterRefillEffect == null || waterRefillEffectPlaying)
            return;

        waterRefillEffectPlaying = true;
        waterRefillEffect.Play();
    }

    void StopWaterRefillEffect()
    {
        if (waterRefillEffect == null || !waterRefillEffectPlaying)
            return;

        waterRefillEffectPlaying = false;
        waterRefillEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    void ShowVacuumPrompt(IVacuumable vacuumable)
    {
        if (vacuumable == promptVacuumable)
            return;

        promptVacuumable = vacuumable;
        Managers.UI.ShowVacuumPrompt(vacuumable.VacuumPrompt);
    }

    void ClearVacuumPrompt()
    {
        if (promptVacuumable == null)
            return;

        promptVacuumable = null;
        Managers.UI.HideInteractText();
    }
}
