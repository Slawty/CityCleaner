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
    [SerializeField] ParticleSystem laserHitEffects;
    [SerializeField] ProgressBar ammoBar;
    [SerializeField] LayerMask hitMask;
    [SerializeField] LineRenderer laserBeam;
    [SerializeField] float maxLaserDistance = 10f;

    [Header("Audio")]
    [SerializeField] EventReference laserLoopEvent;
    [SerializeField] EventReference laserOffEvent;

    float currentAmmo;
    bool isActive;
    bool laserHitEffectsPlaying;
    Quaternion hitEffectsShapeOffset;
    Camera cam;
    EventInstance laserLoopInstance;

    public override void Initialize()
    {
        cam = Managers.MainCam;

        if (laserBeam == null && Tip != null)
            laserBeam = Tip.GetComponentInChildren<LineRenderer>(true);

        if (laserHitEffects == null && Tip != null)
        {
            Transform hitEffectsTransform = Tip.Find("Splitable Smoke PS");
            if (hitEffectsTransform != null)
                laserHitEffects = hitEffectsTransform.GetComponent<ParticleSystem>();
        }

        if (laserHitEffects != null)
            hitEffectsShapeOffset = laserHitEffects.transform.localRotation;

        if (laserBeam != null)
            laserBeam.enabled = false;

        StopLaserHitEffects();

        RefillAmmo();
    }

    protected override void OnShootStart()
    {
        if (currentAmmo <= 0f)
            return;

        isActive = true;

        if (laserBeam != null)
            laserBeam.enabled = true;

        foreach (ParticleSystem effect in sprayEffects)
            effect.Play();

        StartLaserLoop();
    }

    protected override void OnShootStop()
    {
        bool wasActive = isActive;
        isActive = false;

        if (laserBeam != null)
            laserBeam.enabled = false;

        foreach (ParticleSystem effect in sprayEffects)
            effect.Stop();

        StopLaserHitEffects();
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
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, maxLaserDistance, hitMask, QueryTriggerInteraction.Ignore);

        UpdateLaserBeam(hitSomething, hit);
        UpdateLaserHitEffects(hitSomething, hit, ray);

        if (!hitSomething)
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

    void UpdateLaserBeam(bool hitSomething, RaycastHit hit)
    {
        if (laserBeam == null)
            return;

        float beamLength = maxLaserDistance;

        if (hitSomething)
        {
            Vector3 localHit = laserBeam.transform.InverseTransformPoint(hit.point);
            beamLength = Mathf.Clamp(localHit.z, 0f, maxLaserDistance);
        }

        laserBeam.SetPosition(0, Vector3.zero);
        laserBeam.SetPosition(1, new Vector3(0f, 0f, beamLength));
    }

    void UpdateLaserHitEffects(bool hitSomething, RaycastHit hit, Ray ray)
    {
        if (laserHitEffects == null)
            return;

        if (!hitSomething)
        {
            StopLaserHitEffects();
            return;
        }

        Vector3 cameraDirection = ray.direction;

        if (cameraDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(cameraDirection.normalized, hit.normal);
            laserHitEffects.transform.SetPositionAndRotation(hit.point, lookRotation * hitEffectsShapeOffset);
        }
        else
        {
            laserHitEffects.transform.position = hit.point;
        }

        if (laserHitEffectsPlaying)
            return;

        laserHitEffects.Play(true);
        laserHitEffectsPlaying = true;
    }

    void StopLaserHitEffects()
    {
        if (laserHitEffects == null || !laserHitEffectsPlaying)
            return;

        laserHitEffects.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        laserHitEffectsPlaying = false;
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
