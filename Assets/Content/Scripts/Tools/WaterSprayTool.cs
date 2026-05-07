using System.Collections.Generic;
using UnityEngine;

public class WaterSprayTool : Tool
{
    public float MaxAmmo;
    public float AmmoPerSecond = 2f;
    [SerializeField] GPUPainterWorld painter;
    [SerializeField] List<ParticleSystem> sprayEffects;
    [SerializeField] ProgressBar ammoBar;
    float currentAmmo;
    bool isActive;

    public override void Initialize()
    {
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

        if (currentAmmo <= 0f)
            OnShootStop();
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
