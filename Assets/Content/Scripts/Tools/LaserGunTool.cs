using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
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

    [Header("Audio")]
    [SerializeField] EventReference laserLoopEvent;
    [SerializeField] EventReference laserOffEvent;

    float currentAmmo;
    bool isActive;
    Camera cam;
    EventInstance laserLoopInstance;

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

        StartLaserLoop();
    }

    protected override void OnShootStop()
    {
        bool wasActive = isActive;
        isActive = false;

        foreach (ParticleSystem effect in sprayEffects)
            effect.Stop();

        StopLaserLoop();

        if (wasActive)
            PlayLaserOff();
    }

    void StartLaserLoop()
    {
        if (laserLoopEvent.IsNull)
            throw new System.InvalidOperationException("Laser loop FMOD event is not assigned on LaserGunTool.");

        StopLaserLoop();

        laserLoopInstance = RuntimeManager.CreateInstance(laserLoopEvent);
        RuntimeManager.AttachInstanceToGameObject(laserLoopInstance, GetAudioAttachTarget().transform);
        laserLoopInstance.start();
    }

    void StopLaserLoop()
    {
        if (!laserLoopInstance.isValid())
            return;

        laserLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        laserLoopInstance.release();
        laserLoopInstance.clearHandle();
    }

    void PlayLaserOff()
    {
        if (laserOffEvent.IsNull)
            throw new System.InvalidOperationException("Laser off FMOD event is not assigned on LaserGunTool.");

        RuntimeManager.PlayOneShotAttached(laserOffEvent, GetAudioAttachTarget());
    }

    GameObject GetAudioAttachTarget()
    {
        return Tip != null ? Tip.gameObject : gameObject;
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

        Dirtling dirtling = hit.collider.GetComponentInParent<Dirtling>();
        if (dirtling != null)
        {
            dirtling.StateController.ApplyLaser(Time.deltaTime);
            return;
        }

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
            dirtNest.ApplyDamageOverTime(DamagePerSecond);
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
