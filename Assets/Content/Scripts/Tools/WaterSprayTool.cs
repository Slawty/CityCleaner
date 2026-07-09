using System.Collections.Generic;
using UnityEngine;

public class WaterSprayTool : Tool
{
    public float MaxAmmo;
    public float AmmoPerSecond = 2f;
    public float DamagePerSecond = 100f;
    [SerializeField] GPUPainterWorld painter;
    public GPUPainterWorld Painter => painter;
    [SerializeField] List<ParticleSystem> sprayEffects;
    [SerializeField] List<ParticleSystem> impactEffects;
    [SerializeField] float impactSurfaceOffset = 0.02f;
    [SerializeField] float impactRayDistance = 10f;
    [SerializeField] ProgressBar ammoBar;
    [SerializeField] LayerMask dirtlingHitMask = ~0;
    [SerializeField] float dirtlingRayDistance = 12f;
    [SerializeField] float dirtlingPushForcePerSecond = 10f;

    float currentAmmo;
    bool isActive;
    bool ammoDepletedFired;
    Camera cam;

    public float NormalizedAmmo => MaxAmmo > 0f ? currentAmmo / MaxAmmo : 0f;
    public bool IsEmpty => currentAmmo <= 0f;

    public event System.Action OnAmmoDepleted;
    public event System.Action OnAmmoRestored;

    public override void Initialize()
    {
        cam = Managers.MainCam;
        painter.Bind(this);
        RefillWater();
        StopImpactEffects();
    }

    protected override void OnShootStart()
    {
        if (currentAmmo <= 0f)
            return;

        isActive = true;

        foreach (ParticleSystem effect in sprayEffects)
            effect.Play();

        painter.StartPainting();
    }

    protected override void OnShootStop()
    {
        isActive = false;

        foreach (ParticleSystem effect in sprayEffects)
            effect.Stop();

        painter.StopPainting();
        StopImpactEffects();
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
        if (impactEffects == null || impactEffects.Count == 0)
            return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (!Physics.Raycast(ray, out RaycastHit hit, impactRayDistance, painter.PaintMask, QueryTriggerInteraction.Ignore))
        {
            StopImpactEffects();
            return;
        }

        Vector3 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.up;
        Vector3 position = hit.point + normal * impactSurfaceOffset;
        Quaternion rotation = Quaternion.LookRotation(normal);

        foreach (ParticleSystem impactEffect in impactEffects)
        {
            if (impactEffect == null)
                continue;

            impactEffect.transform.SetPositionAndRotation(position, rotation);

            if (!impactEffect.isPlaying)
                impactEffect.Play();
        }
    }

    void StopImpactEffects()
    {
        if (impactEffects == null)
            return;

        foreach (ParticleSystem impactEffect in impactEffects)
        {
            if (impactEffect == null || !impactEffect.isPlaying)
                continue;

            impactEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
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
