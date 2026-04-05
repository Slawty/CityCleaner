using System.Collections.Generic;
using UnityEngine;

public class LaserGunTool : Tool
{
    public float MaxAmmo;
    public float AmmoPerSecond = 2f;
    public float DamagePerSecond = 10f;
    [SerializeField] List<ParticleSystem> sprayEffects;
    [SerializeField] ProgressBar ammoBar;
    [SerializeField] LayerMask hitMask;
    float currentAmmo;
    bool isActive;
    Camera cam;

    void Start()
    {
        cam = Managers.MainCam;
        RefillWater();
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
        if (!isActive)
            return;

        currentAmmo -= Time.deltaTime * AmmoPerSecond;
        currentAmmo = Mathf.Clamp(currentAmmo, 0f, MaxAmmo);
        ammoBar.SetPercent((currentAmmo / MaxAmmo) * 100f);

        HandleLaser();

        if (currentAmmo <= 0f)
            OnShootStop();

    }

    void HandleLaser()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (!Physics.Raycast(ray, out RaycastHit hit, 10f, hitMask, QueryTriggerInteraction.Ignore))
            return;

        var paintable = hit.collider.GetComponent<SplitableObject>();

        if (paintable == null)
            return;

        paintable.UpdateLaserHit(DamagePerSecond);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
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
