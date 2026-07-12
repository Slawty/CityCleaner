using System;
using System.Collections.Generic;
using UnityEngine;

public class GrassPatch : GooHitGrowable
{
    [Header("Renderers")]
    [SerializeField] List<Renderer> growableRenderers;
    [SerializeField] List<Renderer> growableDirtRenderers;

    [Header("Growth")]
    [SerializeField] float minStrength = 0f;
    [SerializeField] float maxStrength = 1f;

    readonly int growStrengthID = Shader.PropertyToID("_GrowStrength");
    readonly int dirtStrengthID = Shader.PropertyToID("_DirtAmount");
    MaterialPropertyBlock mpb;
    readonly CleanFlashPlayer dirtCleanFlash = new();
    readonly List<Renderer> coordinatedFlashRenderers = new();
    bool skipCoordinatedFullGrowthFlash;

    void Awake()
    {
        ConfigurePrerequisitePaintables();
        EnsureMpb();
        InitializeGrowable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        dirtCleanFlash.Stop(invalidateRunning: true);
        dirtCleanFlash.ResetFlash(growableDirtRenderers);
    }

    void EnsureMpb()
    {
        mpb ??= new MaterialPropertyBlock();
    }

    public void PrepareCoordinatedFullGrowth()
    {
        skipCoordinatedFullGrowthFlash = true;
    }

    public void CompleteCoordinatedFullGrowth()
    {
        DisablePrerequisiteGlow();
        FinalizePrerequisiteCleanablesWithoutFlash();
    }

    public void CollectFlashRenderers(List<Renderer> buffer)
    {
        AddUniqueRenderers(buffer, growableDirtRenderers);
        CollectPrerequisiteFlashRenderers(buffer);
    }

    protected override void OnPrerequisiteCleanedVisual()
    {
        if (!IsReadyForGoo)
            return;

        PlayCoordinatedCleanFlash(null);
    }

    protected override void OnFullyGrown()
    {
        if (skipCoordinatedFullGrowthFlash)
        {
            skipCoordinatedFullGrowthFlash = false;
            return;
        }

        PlayCoordinatedCleanFlash(() =>
        {
            DisablePrerequisiteGlow();
            FinalizePrerequisiteCleanablesWithoutFlash();
        });
    }

    protected override void ApplyGrowth(float progress, float hitMultiplier)
    {
        EnsureMpb();

        float baseStrength = Mathf.Lerp(minStrength, maxStrength, progress);
        float bumpedStrength = Mathf.Clamp01(baseStrength * hitMultiplier);
        mpb.SetFloat(growStrengthID, bumpedStrength);

        if (growableRenderers != null)
        {
            for (int i = 0; i < growableRenderers.Count; i++)
            {
                if (growableRenderers[i] == null)
                    continue;

                growableRenderers[i].SetPropertyBlock(mpb);
            }
        }

        ApplyGrowableDirt(1f - progress);
    }

    void ApplyGrowableDirt(float dirtAmount)
    {
        if (dirtCleanFlash.IsPlaying || growableDirtRenderers == null)
            return;

        for (int i = 0; i < growableDirtRenderers.Count; i++)
        {
            Renderer renderer = growableDirtRenderers[i];
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(mpb);
            mpb.SetFloat(dirtStrengthID, dirtAmount);
            renderer.SetPropertyBlock(mpb);
        }
    }

    void PlayCoordinatedCleanFlash(Action onComplete)
    {
        StopPrerequisiteCleanFlashes();
        coordinatedFlashRenderers.Clear();
        CollectFlashRenderers(coordinatedFlashRenderers);
        dirtCleanFlash.Play(coordinatedFlashRenderers, onComplete);
    }
}
