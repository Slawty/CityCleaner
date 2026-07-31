using System.Collections.Generic;
using UnityEngine;

public class WaterRefill : MonoBehaviour, IInteractable
{
    [SerializeField] float refillDuration = 3f;
    [SerializeField] List<ParticleSystem> refillEffects;

    bool isRefilling;

    public string Prompt => "Refill Water";

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

    public void Interact(GameObject interactor)
    {
        WaterSprayTool waterSprayer = Managers.Tools.WaterSprayer;
        if (waterSprayer.NormalizedAmmo >= 1f)
            return;

        isRefilling = true;
        Managers.Input.BlockInteraction(this);
        PlayRefillEffects();
    }

    public void InteractReleased(GameObject interactor)
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
        Managers.Input.UnblockInteraction(this);
        StopRefillEffects();
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
}
