using System.Collections.Generic;
using UnityEngine;

public class GooGunTool : Tool
{
    public float MaxAmmo;
    public float AmmoPerSecond = 2f;
    public float CleanStrength = 1f;
    [SerializeField] GPUPainter painter;
    [SerializeField] List<ParticleSystem> sprayEffects;
    [SerializeField] ProgressBar ammoBar;
    [SerializeField] GooParticleNotifier particleHitNotifier;
    float currentAmmo;
    bool isActive;

    public override void Initialize()
    {
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
        if (!isActive)
            return;

        currentAmmo -= Time.deltaTime * AmmoPerSecond;
        currentAmmo = Mathf.Clamp(currentAmmo, 0f, MaxAmmo);
        ammoBar.SetPercent((currentAmmo / MaxAmmo) * 100f);

        if (currentAmmo <= 0f)
            OnShootStop();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        particleHitNotifier.OnGooHit += OnParticleHit;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        painter.StopPainting();
        particleHitNotifier.OnGooHit -= OnParticleHit;
    }

    public void RefillAmmo()
    {
        currentAmmo = MaxAmmo;
        ammoBar.SetPercent(100f);
    }

    public void FillAmmoAmount(float amount)
    {
        currentAmmo += amount;
        currentAmmo = Mathf.Clamp(currentAmmo, 0f, MaxAmmo);
        ammoBar.SetPercent((currentAmmo / MaxAmmo) * 100f);
    }

    void OnParticleHit(Vector3 hitPos, GameObject hitObject)
    {
        // Debug.Log($"Goo hit {hitObject.name} at: {hitPos}");
        if (hitObject.TryGetComponent(out GPUPaintableObject paintable) && paintable.AllowGooCleaning)
        {
            painter.Paint(paintable, hitPos, CleanStrength);
        }
    }
}
