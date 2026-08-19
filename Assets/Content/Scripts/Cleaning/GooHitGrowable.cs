using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public abstract class GooHitGrowable : MonoBehaviour, IGooHitReceiver
{
    [Header("Growth")]
    [SerializeField, Min(1)] int hitsToFullGrowth = 10;
    [Tooltip("Initial growth when play mode starts. Runtime progress is not saved to scenes.")]
    [SerializeField, Range(0f, 1f)] float startGrowthProgress = 0f;

    float growthProgress;

    [Header("Hit Effect")]
    [SerializeField] float bumpAmount = 0.1f;
    [SerializeField] float bumpDuration = 0.15f;
    [SerializeField, Range(0.1f, 0.9f)] float bumpPeakAt = 0.35f;
    [SerializeField] Ease bumpUpEase = Ease.OutBack;
    [SerializeField] Ease bumpDownEase = Ease.OutQuad;

    [Header("Clean Before Goo")]
    [SerializeField] protected List<GPUPaintableObject> prerequisiteCleanables = new();

    [Header("Linked Growables")]
    [SerializeField] List<GooHitGrowable> linkedGrowables = new();

    [Header("Reward")]
    [SerializeField] int winCoins = 1;
    [SerializeField] Transform coinSpawnPos;

    int gooHitCount;
    bool fullyGrown;
    bool prerequisitesSubscribed;
    float bumpMultiplier = 1f;
    Tween bumpTween;

    /// <summary>0–1 goo growth for area progress.</summary>
    public float GrowthProgress01 => growthProgress;

    public bool IsFullyGrown => fullyGrown;

    public bool IsReadyForGoo => prerequisiteCleanables.Count == 0 || AllPrerequisitesClean();

    public UnityAction OnGrowthProgressChanged;

    /// <summary>Fires once when growth reaches 100% (including linked propagation).</summary>
    public UnityAction OnFullyGrownCompleted;

    public void CollectLinkedGroup(HashSet<GooHitGrowable> results)
    {
        if (results == null || !results.Add(this))
            return;

        for (int i = 0; i < linkedGrowables.Count; i++)
        {
            GooHitGrowable linkedGrowable = linkedGrowables[i];
            if (linkedGrowable != null)
                linkedGrowable.CollectLinkedGroup(results);
        }
    }

    protected void InitializeGrowable()
    {
        growthProgress = Mathf.Clamp01(startGrowthProgress);
        ApplyGrowth(growthProgress, 1f);
        fullyGrown = growthProgress >= 1f;
        if (fullyGrown)
            gooHitCount = Mathf.Max(1, hitsToFullGrowth);
    }

    void Start()
    {
        ConfigurePrerequisitePaintables();
        SubscribePrerequisites();
        SyncGooReadyVisualState();
    }

    protected virtual void OnEnable()
    {
        SubscribePrerequisites();
        SyncGooReadyVisualState();
    }

    protected virtual void OnDisable()
    {
        bumpTween?.Kill();
        UnsubscribePrerequisites();
    }

    public void DebugSetFullyGrown()
    {
        bumpTween?.Kill();
        bumpMultiplier = 1f;
        gooHitCount = Mathf.Max(1, hitsToFullGrowth);
        growthProgress = 1f;
        fullyGrown = true;

        ApplyGrowth(growthProgress, bumpMultiplier);
        OnDebugSetFullyGrown();
        OnGrowthProgressChanged?.Invoke();
    }

    public void DebugResetGrowth()
    {
        bumpTween?.Kill();
        bumpMultiplier = 1f;
        gooHitCount = 0;
        growthProgress = 0f;
        fullyGrown = false;

        ApplyGrowth(growthProgress, bumpMultiplier);
        OnDebugResetGrowth();
        SyncGooReadyVisualState();
    }

    public void OnGooHit(Vector3 hitPoint, GameObject source)
    {
        if (!IsReadyForGoo)
            return;

        HashSet<GooHitGrowable> visited = new();
        PropagateHit(visited);
    }

    void PropagateHit(HashSet<GooHitGrowable> visited)
    {
        if (!visited.Add(this))
            return;

        if (!IsReadyForGoo)
            return;

        ApplyHit();

        for (int i = 0; i < linkedGrowables.Count; i++)
        {
            GooHitGrowable linkedGrowable = linkedGrowables[i];
            if (linkedGrowable == null)
                continue;

            linkedGrowable.PropagateHit(visited);
        }
    }

    void ApplyHit()
    {
        if (fullyGrown)
            return;

        gooHitCount++;
        int safeHitsToFull = Mathf.Max(1, hitsToFullGrowth);
        growthProgress = Mathf.Clamp01((float)gooHitCount / safeHitsToFull);

        OnGrowthProgressChanged?.Invoke();

        TriggerBump();
        ApplyGrowth(growthProgress, bumpMultiplier);

        if (growthProgress >= 1f)
        {
            fullyGrown = true;
            OnFullyGrown();
            SpawnWinCoins();
            OnFullyGrownCompleted?.Invoke();
        }
    }

    void TriggerBump()
    {
        bumpTween?.Kill();

        float peakMultiplier = 1f + bumpAmount;
        float upDuration = Mathf.Max(0.01f, bumpDuration * bumpPeakAt);
        float downDuration = Mathf.Max(0.01f, bumpDuration - upDuration);

        bumpTween = DOTween.Sequence()
            .Append(DOTween.To(() => bumpMultiplier, x =>
            {
                bumpMultiplier = x;
                ApplyGrowth(growthProgress, bumpMultiplier);
            }, peakMultiplier, upDuration).SetEase(bumpUpEase))
            .Append(DOTween.To(() => bumpMultiplier, x =>
            {
                bumpMultiplier = x;
                ApplyGrowth(growthProgress, bumpMultiplier);
            }, 1f, downDuration).SetEase(bumpDownEase));
    }

    protected virtual void ConfigurePrerequisitePaintables()
    {
        for (int i = 0; i < prerequisiteCleanables.Count; i++)
        {
            GPUPaintableObject paintable = prerequisiteCleanables[i];
            if (paintable == null)
                continue;

            paintable.DeferCleanMaterialSwapUntilGrowComplete = true;
        }
    }

    void SubscribePrerequisites()
    {
        if (prerequisitesSubscribed)
            return;

        prerequisitesSubscribed = true;

        for (int i = 0; i < prerequisiteCleanables.Count; i++)
        {
            GPUPaintableObject paintable = prerequisiteCleanables[i];
            if (paintable == null)
                continue;

            paintable.OnCleaned += OnPrerequisiteCleaned;
        }
    }

    void UnsubscribePrerequisites()
    {
        if (!prerequisitesSubscribed)
            return;

        prerequisitesSubscribed = false;

        for (int i = 0; i < prerequisiteCleanables.Count; i++)
        {
            GPUPaintableObject paintable = prerequisiteCleanables[i];
            if (paintable == null)
                continue;

            paintable.OnCleaned -= OnPrerequisiteCleaned;
        }
    }

    void OnPrerequisiteCleaned()
    {
        if (fullyGrown)
            return;

        OnPrerequisiteCleanedVisual();
        EnableGooReadyGlow();
    }

    protected virtual void OnPrerequisiteCleanedVisual() { }

    void SyncGooReadyVisualState()
    {
        if (fullyGrown)
        {
            DisableGooReadyGlow();
            FinalizePrerequisiteCleanables();
            return;
        }

        if (IsReadyForGoo)
            EnableGooReadyGlow();
        else
            DisableGooReadyGlow();
    }

    bool AllPrerequisitesClean()
    {
        for (int i = 0; i < prerequisiteCleanables.Count; i++)
        {
            GPUPaintableObject paintable = prerequisiteCleanables[i];
            if (paintable != null && !paintable.isClean)
                return false;
        }

        return true;
    }

    void EnableGooReadyGlow()
    {
        for (int i = 0; i < prerequisiteCleanables.Count; i++)
        {
            GPUPaintableObject paintable = prerequisiteCleanables[i];
            if (paintable == null || !paintable.isClean)
                continue;

            paintable.SetGooReadyGlow(1f);
        }
    }

    void DisableGooReadyGlow()
    {
        for (int i = 0; i < prerequisiteCleanables.Count; i++)
        {
            GPUPaintableObject paintable = prerequisiteCleanables[i];
            if (paintable == null)
                continue;

            paintable.SetGooReadyGlow(0f);
        }
    }

    void FinalizePrerequisiteCleanables()
    {
        for (int i = 0; i < prerequisiteCleanables.Count; i++)
        {
            GPUPaintableObject paintable = prerequisiteCleanables[i];
            if (paintable == null || !paintable.isClean)
                continue;

            paintable.FinalizeCleanMaterial();
        }
    }

    protected IReadOnlyList<GooHitGrowable> LinkedGrowables => linkedGrowables;

    protected void CollectPrerequisiteFlashRenderers(List<Renderer> buffer)
    {
        for (int i = 0; i < prerequisiteCleanables.Count; i++)
        {
            GPUPaintableObject paintable = prerequisiteCleanables[i];
            if (paintable == null)
                continue;

            Renderer renderer = paintable.GetFlashRenderer();
            if (renderer == null || buffer.Contains(renderer))
                continue;

            buffer.Add(renderer);
        }
    }

    protected static void AddUniqueRenderers(List<Renderer> buffer, List<Renderer> renderers)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || buffer.Contains(renderer))
                continue;

            buffer.Add(renderer);
        }
    }

    protected void StopPrerequisiteCleanFlashes()
    {
        for (int i = 0; i < prerequisiteCleanables.Count; i++)
        {
            GPUPaintableObject paintable = prerequisiteCleanables[i];
            if (paintable == null)
                continue;

            paintable.StopCleanFlash();
        }
    }

    protected void FinalizePrerequisiteCleanablesWithoutFlash()
    {
        for (int i = 0; i < prerequisiteCleanables.Count; i++)
        {
            GPUPaintableObject paintable = prerequisiteCleanables[i];
            if (paintable == null || !paintable.isClean)
                continue;

            paintable.FinalizeCleanMaterialWithoutFlash();
        }
    }

    protected void DisablePrerequisiteGlow()
    {
        DisableGooReadyGlow();
    }

    void SpawnWinCoins()
    {
        if (winCoins <= 0)
            return;

        Vector3 spawnPos = coinSpawnPos != null ? coinSpawnPos.position : transform.position;
        Managers.Spawning.SpawnCoins(winCoins, spawnPos).Forget();
    }

    protected virtual void OnFullyGrown()
    {
        DisableGooReadyGlow();
        FinalizePrerequisiteCleanables();
    }

    protected virtual void OnDebugSetFullyGrown() { }

    protected virtual void OnDebugResetGrowth() { }

    protected abstract void ApplyGrowth(float progress, float hitMultiplier);
}
