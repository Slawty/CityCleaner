using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GrowableObject : GooHitGrowable
{
    [Header("Growth")]
    public float minScale = 0.5f;
    public float maxScale = 1f;
    public Transform growObject;
    public List<Renderer> cleanRenderers;

    [Header("Clean Flash")]
    [SerializeField] float cleanBeforeFlashDelay = 0.15f;

    readonly CleanFlashPlayer cleanFlashPlayer = new();
    readonly List<Renderer> fullGrowthFlashRenderers = new();
    readonly List<Renderer> coordinatedFlashRenderers = new();

    void Awake()
    {
        ConfigurePrerequisitePaintables();
        BuildFullGrowthFlashRenderers();
        InitializeGrowable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        cleanFlashPlayer.Stop(invalidateRunning: true);
        cleanFlashPlayer.ResetFlash(fullGrowthFlashRenderers);
    }

    void BuildFullGrowthFlashRenderers()
    {
        fullGrowthFlashRenderers.Clear();

        if (growObject != null)
        {
            Renderer[] growRenderers = growObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < growRenderers.Length; i++)
                AddFullGrowthFlashRenderer(growRenderers[i]);
        }

        if (cleanRenderers == null)
            return;

        for (int i = 0; i < cleanRenderers.Count; i++)
            AddFullGrowthFlashRenderer(cleanRenderers[i]);
    }

    void AddFullGrowthFlashRenderer(Renderer renderer)
    {
        if (renderer == null || fullGrowthFlashRenderers.Contains(renderer))
            return;

        fullGrowthFlashRenderers.Add(renderer);
    }

    protected override void OnPrerequisiteCleanedVisual()
    {
        if (!IsReadyForGoo)
            return;

        PlayCoordinatedCleanFlash(null, includeGrowthRenderers: false);
    }

    protected override void OnFullyGrown()
    {
        DisablePrerequisiteGlow();
        PrepareLinkedGrassPatchesForCoordinatedFlash();
        PlayFullGrowthSequence().Forget();
    }

    async UniTaskVoid PlayFullGrowthSequence()
    {
        CleanShaders();

        if (cleanBeforeFlashDelay > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(cleanBeforeFlashDelay));

        PlayCoordinatedCleanFlash(() =>
        {
            FinalizePrerequisiteCleanablesWithoutFlash();
            CompleteLinkedGrassPatchesCoordinatedFullGrowth();
        }, includeGrowthRenderers: true);
    }

    void PlayCoordinatedCleanFlash(Action onComplete, bool includeGrowthRenderers)
    {
        StopPrerequisiteCleanFlashes();
        coordinatedFlashRenderers.Clear();

        if (includeGrowthRenderers)
            AddUniqueRenderers(coordinatedFlashRenderers, fullGrowthFlashRenderers);

        CollectPrerequisiteFlashRenderers(coordinatedFlashRenderers);
        CollectLinkedGrassPatchFlashRenderers(coordinatedFlashRenderers);
        cleanFlashPlayer.Play(coordinatedFlashRenderers, onComplete);
    }

    void PrepareLinkedGrassPatchesForCoordinatedFlash()
    {
        for (int i = 0; i < LinkedGrowables.Count; i++)
        {
            if (LinkedGrowables[i] is GrassPatch grassPatch)
                grassPatch.PrepareCoordinatedFullGrowth();
        }
    }

    void CollectLinkedGrassPatchFlashRenderers(List<Renderer> buffer)
    {
        for (int i = 0; i < LinkedGrowables.Count; i++)
        {
            if (LinkedGrowables[i] is GrassPatch grassPatch)
                grassPatch.CollectFlashRenderers(buffer);
        }
    }

    void CompleteLinkedGrassPatchesCoordinatedFullGrowth()
    {
        for (int i = 0; i < LinkedGrowables.Count; i++)
        {
            if (LinkedGrowables[i] is GrassPatch grassPatch)
                grassPatch.CompleteCoordinatedFullGrowth();
        }
    }

    protected override void OnDebugSetFullyGrown()
    {
        cleanFlashPlayer.Stop(invalidateRunning: true);
        cleanFlashPlayer.ResetFlash(fullGrowthFlashRenderers);
        CleanShaders();
    }

    protected override void OnDebugResetGrowth()
    {
        cleanFlashPlayer.Stop(invalidateRunning: true);
        cleanFlashPlayer.ResetFlash(fullGrowthFlashRenderers);
        DirtyShaders();
    }

    void CleanShaders()
    {
        if (cleanRenderers == null)
            return;

        for (int i = 0; i < cleanRenderers.Count; i++)
            SetDirtAmount(cleanRenderers[i], 0f);
    }

    void DirtyShaders()
    {
        if (cleanRenderers == null)
            return;

        for (int i = 0; i < cleanRenderers.Count; i++)
            SetDirtAmount(cleanRenderers[i], 1f);
    }

    void SetDirtAmount(Renderer renderer, float dirtAmount)
    {
        if (renderer == null)
            return;

        Material[] materials = Application.isPlaying ? renderer.materials : renderer.sharedMaterials;

        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material != null && material.HasProperty("_DirtAmount"))
                material.SetFloat("_DirtAmount", dirtAmount);
        }
    }

    protected override void ApplyGrowth(float progress, float hitMultiplier)
    {
        if (growObject == null)
            return;

        float scale = Mathf.Lerp(minScale, maxScale, progress);
        growObject.localScale = Vector3.one * scale * hitMultiplier;
    }
}
