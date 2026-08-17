using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class WaterRefill : MonoBehaviour, IVacuumable
{
    [SerializeField] float refillDuration = 3f;
    [SerializeField] List<ParticleSystem> refillEffects;

    [Header("Audio")]
    [SerializeField] EventReference refillLoopEvent;

    bool isRefilling;
    EventInstance refillLoopInstance;

    public bool CanVacuum => Managers.Tools.WaterSprayer.NormalizedAmmo < 1f;
    public string VacuumPrompt => "Refill Water";

    void Update()
    {
        if (!isRefilling)
            return;

        WaterSprayTool waterSprayer = Managers.Tools.WaterSprayer;
        float refillPerSecond = waterSprayer.MaxAmmo / refillDuration;
        waterSprayer.FillWaterAmount(refillPerSecond * Time.deltaTime);

        if (waterSprayer.NormalizedAmmo >= 1f)
            StopRefill();
    }

    public void VacuumStart()
    {
        if (!CanVacuum || isRefilling)
            return;

        isRefilling = true;
        PlayRefillEffects();
        StartRefillLoop();
    }

    public void VacuumEnd()
    {
        StopRefill();
    }

    void OnDisable()
    {
        StopRefill();
    }

    void StopRefill()
    {
        if (!isRefilling)
            return;

        isRefilling = false;
        StopRefillEffects();
        StopRefillLoop();
    }

    void PlayRefillEffects()
    {
        if (refillEffects == null)
            return;

        foreach (ParticleSystem effect in refillEffects)
        {
            if (effect != null)
                effect.Play();
        }
    }

    void StopRefillEffects()
    {
        if (refillEffects == null)
            return;

        foreach (ParticleSystem effect in refillEffects)
        {
            if (effect != null)
                effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    void StartRefillLoop()
    {
        if (refillLoopEvent.IsNull)
            throw new System.InvalidOperationException("Water refill loop FMOD event is not assigned on WaterRefill.");

        StopRefillLoop();

        refillLoopInstance = RuntimeManager.CreateInstance(refillLoopEvent);
        RuntimeManager.AttachInstanceToGameObject(refillLoopInstance, gameObject);
        refillLoopInstance.start();
    }

    void StopRefillLoop()
    {
        if (!refillLoopInstance.isValid())
            return;

        refillLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        refillLoopInstance.release();
        refillLoopInstance.clearHandle();
    }
}
