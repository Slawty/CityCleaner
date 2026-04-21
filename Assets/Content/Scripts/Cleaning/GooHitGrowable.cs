using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public abstract class GooHitGrowable : MonoBehaviour, IGooHitReceiver
{
    [Header("Growth")]
    [SerializeField, Min(1)] int hitsToFullGrowth = 10;
    [SerializeField, Range(0f, 1f)] float growthProgress = 0f;

    [Header("Hit Effect")]
    [SerializeField] float bumpAmount = 0.1f;
    [SerializeField] float bumpDuration = 0.15f;
    [SerializeField, Range(0.1f, 0.9f)] float bumpPeakAt = 0.35f;
    [SerializeField] Ease bumpUpEase = Ease.OutBack;
    [SerializeField] Ease bumpDownEase = Ease.OutQuad;

    [Header("Linked Growables")]
    [SerializeField] List<GooHitGrowable> linkedGrowables = new();

    int gooHitCount;
    bool fullyGrown;
    float bumpMultiplier = 1f;
    Tween bumpTween;

    protected void InitializeGrowable()
    {
        ApplyGrowth(growthProgress, 1f);
        fullyGrown = growthProgress >= 1f;
        if (fullyGrown)
            gooHitCount = Mathf.Max(1, hitsToFullGrowth);
    }

    protected virtual void OnDisable()
    {
        bumpTween?.Kill();
    }

    public void OnGooHit(Vector3 hitPoint, GameObject source)
    {
        HashSet<GooHitGrowable> visited = new();
        PropagateHit(visited);
    }

    void PropagateHit(HashSet<GooHitGrowable> visited)
    {
        if (!visited.Add(this))
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

        TriggerBump();
        ApplyGrowth(growthProgress, bumpMultiplier);

        if (growthProgress >= 1f)
        {
            fullyGrown = true;
            OnFullyGrown();
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

    protected virtual void OnFullyGrown() { }

    protected abstract void ApplyGrowth(float progress, float hitMultiplier);
}
