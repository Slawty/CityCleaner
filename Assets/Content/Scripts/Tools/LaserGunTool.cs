using System.Collections.Generic;
using UnityEngine;

public class LaserGunTool : Tool
{
    public float MaxAmmo = 100f;
    public float AmmoDrainPerSecond = 2f;
    public float RechargePerSecond = 5f;
    public float DamagePerSecond = 10f;

    [SerializeField] List<ParticleSystem> sprayEffects;
    [SerializeField] ProgressBar ammoBar;
    [SerializeField] LayerMask hitMask;

    float currentAmmo;
    bool isActive;
    Camera cam;

    public override void Initialize()
    {
        cam = Managers.MainCam;
        RefillAmmo();
    }

    protected override void OnShootStart()
    {
        if (currentAmmo <= 0f)
            return;

        isActive = true;

        foreach (ParticleSystem effect in sprayEffects)
            effect.Play();
    }

    protected override void OnShootStop()
    {
        isActive = false;

        foreach (ParticleSystem effect in sprayEffects)
            effect.Stop();
    }

    void Update()
    {
        if (isActive)
        {
            DrainAmmo();
            HandleLaser();

            if (currentAmmo <= 0f)
                OnShootStop();
        }
        else
        {
            RechargeAmmo();
        }

        UpdateAmmoBar();
    }

    void DrainAmmo()
    {
        currentAmmo -= Time.deltaTime * AmmoDrainPerSecond;
        currentAmmo = Mathf.Clamp(currentAmmo, 0f, MaxAmmo);
    }

    void RechargeAmmo()
    {
        if (currentAmmo >= MaxAmmo)
            return;

        currentAmmo += Time.deltaTime * RechargePerSecond;
        currentAmmo = Mathf.Clamp(currentAmmo, 0f, MaxAmmo);
    }

    void UpdateAmmoBar()
    {
        ammoBar.SetPercent((currentAmmo / MaxAmmo) * 100f);
    }

    void HandleLaser()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (!Physics.Raycast(ray, out RaycastHit hit, 10f, hitMask, QueryTriggerInteraction.Ignore))
            return;

        var target = hit.collider.GetComponent<SplitableObject>();

        if (target != null)
        {
            target.UpdateLaserHit(DamagePerSecond);
            return;
        }

        var flashTarget = hit.collider.GetComponent<HitFlashObject>();

        if (flashTarget != null)
        {
            flashTarget.UpdateLaserHit(DamagePerSecond);
        }

        DirtNest dirtNest = hit.collider.GetComponentInParent<DirtNest>();

        if (dirtNest != null)
            dirtNest.ApplyLaserDamage(DamagePerSecond);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        OnShootStop();
    }

    public void RefillAmmo()
    {
        currentAmmo = MaxAmmo;
        UpdateAmmoBar();
    }

    public void FillAmmoAmount(float amount)
    {
        currentAmmo += amount;
        currentAmmo = Mathf.Clamp(currentAmmo, 0f, MaxAmmo);
        UpdateAmmoBar();
    }
}