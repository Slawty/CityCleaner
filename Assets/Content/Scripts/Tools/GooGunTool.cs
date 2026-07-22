using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class GooGunTool : Tool
{
    public float MaxAmmo;
    public float AmmoPerSecond = 2f;
    public float DamagePerSecond = 100f;
    public float CleanStrength = 1f;
    [SerializeField] GPUPainter painter;
    [SerializeField] List<ParticleSystem> sprayEffects;
    [SerializeField] ProgressBar ammoBar;
    [SerializeField] GooParticleNotifier particleHitNotifier;

    [Header("Audio")]
    [SerializeField] EventReference gooShootEvent;

    float currentAmmo;
    bool isActive;
    float shootSoundTimer;

    public override void Initialize()
    {
        particleHitNotifier.Bind(this);
        RefillAmmo();
    }

    public float GooDamagePerParticle
    {
        get
        {
            float emissionRate = particleHitNotifier.EmissionRateOverTime;
            return DamagePerSecond / Mathf.Max(emissionRate, 0.001f);
        }
    }

    protected override void OnShootStart()
    {
        if (currentAmmo <= 0f)
            return;

        isActive = true;
        shootSoundTimer = 0f;

        foreach (ParticleSystem effect in sprayEffects)
            effect.Play();

        PlayGooShoot();
    }

    protected override void OnShootStop()
    {
        isActive = false;
        shootSoundTimer = 0f;

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

        UpdateShootSound();

        if (currentAmmo <= 0f)
            OnShootStop();
    }

    void UpdateShootSound()
    {
        float emissionRate = Mathf.Max(particleHitNotifier.EmissionRateOverTime, 0.001f);
        float interval = 1f / emissionRate;

        shootSoundTimer += Time.deltaTime;
        while (shootSoundTimer >= interval)
        {
            shootSoundTimer -= interval;
            PlayGooShoot();
        }
    }

    void PlayGooShoot()
    {
        if (gooShootEvent.IsNull)
            throw new System.InvalidOperationException("Goo shoot FMOD event is not assigned on GooGunTool.");

        GameObject attachTarget = Tip != null ? Tip.gameObject : gameObject;
        RuntimeManager.PlayOneShotAttached(gooShootEvent, attachTarget);
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
        if (hitObject.GetComponentInParent<GPUPaintableObject>() is { AllowGooCleaning: true })
            painter.PaintAtPosition(hitPos, CleanStrength, gooOnly: true);
    }
}
