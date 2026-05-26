using System.Collections.Generic;
using UnityEngine;

public class WaterSprayTool : Tool
{
    public float MaxAmmo;
    public float AmmoPerSecond = 2f;
    public float DamagePerSecond = 100f;
    [SerializeField] GPUPainterWorld painter;
    [SerializeField] List<ParticleSystem> sprayEffects;
    [SerializeField] ProgressBar ammoBar;
    [SerializeField] LayerMask dirtlingHitMask = ~0;
    [SerializeField] float dirtlingRayDistance = 12f;
    [SerializeField] float dirtlingPushForcePerSecond = 10f;

    float currentAmmo;
    bool isActive;
    Camera cam;

    public override void Initialize()
    {
        cam = Managers.MainCam;
        painter.Bind(this);
        RefillWater();
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
    }

    void Update()
    {
        if (!isActive)
            return;

        currentAmmo -= Time.deltaTime * AmmoPerSecond;
        currentAmmo = Mathf.Clamp(currentAmmo, 0f, MaxAmmo);
        ammoBar.SetPercent((currentAmmo / MaxAmmo) * 100f);

        HandleWaterRay();

        if (currentAmmo <= 0f)
            OnShootStop();
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
    }

    public void RefillWater()
    {
        currentAmmo = MaxAmmo;
        ammoBar.SetPercent(100f);
    }

    public void FillWaterAmount(float amount)
    {
        currentAmmo += amount;
        currentAmmo = Mathf.Clamp(currentAmmo, 0f, MaxAmmo);
        ammoBar.SetPercent((currentAmmo / MaxAmmo) * 100f);
    }
}
