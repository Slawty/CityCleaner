using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class WaterSprayTool : Tool
{
    public float MaxAmmo;
    public float AmmoPerSecond = 2f;
    public float DamagePerSecond = 100f;
    [SerializeField] GPUPainterWorld painter;
    public GPUPainterWorld Painter => painter;
    [SerializeField] List<ParticleSystem> sprayEffects;
    [SerializeField] GameObject sprayEffectsObject;
    [SerializeField] List<ParticleSystem> impactEffects;
    [SerializeField] float impactSurfaceOffset = 0.02f;
    [SerializeField] float impactRayDistance = 10f;
    [SerializeField] ProgressBar ammoBar;
    [SerializeField] LayerMask dirtlingHitMask = ~0;
    [SerializeField] float dirtlingRayDistance = 12f;
    [SerializeField] float dirtlingPushForcePerSecond = 10f;

    [Header("Audio")]
    [SerializeField] EventReference washerStartEvent;
    [SerializeField] EventReference washerLoopEvent;
    [SerializeField] EventReference washerHitLoopEvent;

    float currentAmmo;
    bool isActive;
    bool ammoDepletedFired;
    bool impactEffectsPlaying;
    Quaternion hitEffectsShapeOffset;
    Camera cam;
    EventInstance washerLoopInstance;
    EventInstance washerHitLoopInstance;

    public float NormalizedAmmo => MaxAmmo > 0f ? currentAmmo / MaxAmmo : 0f;
    public bool IsEmpty => currentAmmo <= 0f;

    public event System.Action OnAmmoDepleted;
    public event System.Action OnAmmoRestored;

    public override void Initialize()
    {
        cam = Managers.MainCam;
        painter.Bind(this);
        ResolveImpactEffects();
        ResolveSprayEffectsObject();
        RefillWater();
        StopImpactEffects();
        SetSprayEffectsObjectActive(false);
    }

    void ResolveImpactEffects()
    {
        if (impactEffects != null && impactEffects.Count > 0 && impactEffects[0] != null)
        {
            hitEffectsShapeOffset = impactEffects[0].transform.localRotation;
            return;
        }

        Transform searchRoot = Tip != null ? Tip : transform;
        Transform hitEffectsTransform = searchRoot.Find("WaterSpray Hit Effect");
        if (hitEffectsTransform == null)
            return;

        ParticleSystem hitEffect = hitEffectsTransform.GetComponent<ParticleSystem>();
        if (hitEffect == null)
            return;

        impactEffects = new List<ParticleSystem> { hitEffect };
        hitEffectsShapeOffset = hitEffect.transform.localRotation;
    }

    void ResolveSprayEffectsObject()
    {
        if (sprayEffectsObject != null)
            return;

        Transform searchRoot = Tip != null ? Tip : transform;
        Transform effectsTransform = searchRoot.Find("SprayEffectsObject");
        if (effectsTransform == null)
            return;

        sprayEffectsObject = effectsTransform.gameObject;
    }

    void SetSprayEffectsObjectActive(bool active)
    {
        if (sprayEffectsObject == null)
            return;

        sprayEffectsObject.SetActive(active);
    }

    protected override void OnShootStart()
    {
        if (currentAmmo <= 0f)
            return;

        isActive = true;

        foreach (ParticleSystem effect in sprayEffects)
            effect.Play();

        SetSprayEffectsObjectActive(true);
        painter.StartPainting();
        PlayWasherStart();
        StartWasherLoop();
    }

    void PlayWasherStart()
    {
        if (washerStartEvent.IsNull)
            throw new System.InvalidOperationException("Washer start FMOD event is not assigned on WaterSprayTool.");

        RuntimeManager.PlayOneShotAttached(washerStartEvent, GetAudioAttachTarget());
    }

    void StartWasherLoop()
    {
        if (washerLoopEvent.IsNull)
            throw new System.InvalidOperationException("Washer loop FMOD event is not assigned on WaterSprayTool.");

        StopWasherLoop();

        washerLoopInstance = RuntimeManager.CreateInstance(washerLoopEvent);
        RuntimeManager.AttachInstanceToGameObject(washerLoopInstance, GetAudioAttachTarget());
        washerLoopInstance.start();
    }

    void StopWasherLoop()
    {
        if (!washerLoopInstance.isValid())
            return;

        washerLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        washerLoopInstance.release();
        washerLoopInstance.clearHandle();
    }

    void StartWasherHitLoop()
    {
        if (washerHitLoopEvent.IsNull)
            throw new System.InvalidOperationException("Washer hit loop FMOD event is not assigned on WaterSprayTool.");

        if (washerHitLoopInstance.isValid())
            return;

        washerHitLoopInstance = RuntimeManager.CreateInstance(washerHitLoopEvent);
        washerHitLoopInstance.start();
    }

    void UpdateWasherHitLoopPosition(Vector3 hitPoint)
    {
        if (!washerHitLoopInstance.isValid())
            return;

        washerHitLoopInstance.set3DAttributes(hitPoint.To3DAttributes());
    }

    void StopWasherHitLoop()
    {
        if (!washerHitLoopInstance.isValid())
            return;

        washerHitLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        washerHitLoopInstance.release();
        washerHitLoopInstance.clearHandle();
    }

    GameObject GetAudioAttachTarget()
    {
        return Tip != null ? Tip.gameObject : gameObject;
    }

    protected override void OnShootStop()
    {
        isActive = false;

        foreach (ParticleSystem effect in sprayEffects)
            effect.Stop();

        SetSprayEffectsObjectActive(false);
        painter.StopPainting();
        StopImpactEffects();
        StopWasherLoop();
        StopWasherHitLoop();
    }

    void Update()
    {
        if (!isActive)
            return;

        currentAmmo -= Time.deltaTime * AmmoPerSecond;
        currentAmmo = Mathf.Clamp(currentAmmo, 0f, MaxAmmo);
        ammoBar.SetPercent((currentAmmo / MaxAmmo) * 100f);

        HandleImpactEffects();
        HandleWaterRay();

        if (currentAmmo <= 0f)
        {
            if (!ammoDepletedFired)
            {
                ammoDepletedFired = true;
                OnAmmoDepleted?.Invoke();
            }

            OnShootStop();
        }
    }

    void HandleImpactEffects()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, impactRayDistance, painter.PaintMask, QueryTriggerInteraction.Ignore);

        UpdateImpactEffects(hitSomething, hit, ray);

        if (!hitSomething)
        {
            StopWasherHitLoop();
            return;
        }

        StartWasherHitLoop();
        UpdateWasherHitLoopPosition(hit.point);
    }

    void UpdateImpactEffects(bool hitSomething, RaycastHit hit, Ray ray)
    {
        if (impactEffects == null || impactEffects.Count == 0)
            return;

        if (!hitSomething)
        {
            StopImpactEffects();
            return;
        }

        Vector3 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.up;
        Vector3 position = hit.point + normal * impactSurfaceOffset;
        Vector3 cameraDirection = ray.direction;
        Quaternion rotation = cameraDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(cameraDirection.normalized, normal) * hitEffectsShapeOffset
            : Quaternion.LookRotation(normal);

        foreach (ParticleSystem impactEffect in impactEffects)
        {
            if (impactEffect == null)
                continue;

            impactEffect.transform.SetPositionAndRotation(position, rotation);
        }

        if (impactEffectsPlaying)
            return;

        foreach (ParticleSystem impactEffect in impactEffects)
        {
            if (impactEffect == null)
                continue;

            impactEffect.Play(true);
        }

        impactEffectsPlaying = true;
    }

    void StopImpactEffects()
    {
        if (impactEffects == null || !impactEffectsPlaying)
            return;

        foreach (ParticleSystem impactEffect in impactEffects)
        {
            if (impactEffect == null)
                continue;

            impactEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        impactEffectsPlaying = false;
    }

    void HandleWaterRay()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (!Physics.Raycast(ray, out RaycastHit hit, dirtlingRayDistance, dirtlingHitMask, QueryTriggerInteraction.Ignore))
            return;

        Dirtling dirtling = hit.collider.GetComponentInParent<Dirtling>();
        if (dirtling == null)
            return;

        Vector3 pushDirection = hit.point - cam.transform.position;
        dirtling.StateController.ApplyWater(pushDirection, dirtlingPushForcePerSecond);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        painter.StopPainting();
        StopImpactEffects();
        StopWasherLoop();
        StopWasherHitLoop();
    }

    public void RefillWater()
    {
        currentAmmo = MaxAmmo;
        ammoBar.SetPercent(100f);
        ammoDepletedFired = false;
        OnAmmoRestored?.Invoke();
    }

    public void FillWaterAmount(float amount)
    {
        bool wasEmpty = currentAmmo <= 0f;
        currentAmmo += amount;
        currentAmmo = Mathf.Clamp(currentAmmo, 0f, MaxAmmo);
        ammoBar.SetPercent((currentAmmo / MaxAmmo) * 100f);

        if (currentAmmo > 0f)
            ammoDepletedFired = false;

        if (wasEmpty && currentAmmo > 0f)
            OnAmmoRestored?.Invoke();
    }
}
