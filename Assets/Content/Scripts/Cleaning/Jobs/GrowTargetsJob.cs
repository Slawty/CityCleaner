using System.Collections.Generic;
using UnityEngine;

public class GrowTargetsJob : Job
{
    [SerializeField] List<GooHitGrowable> targets = new();

    bool tracking;

    public IReadOnlyList<GooHitGrowable> Targets => targets;

    public override float NormalizedProgress => GetTargetsProgress();

    public override void StartTracking()
    {
        if (tracking)
            return;

        tracking = true;
        ResetWaypointDismissal();

        foreach (GooHitGrowable target in targets)
        {
            if (target == null || target.IsFullyGrown)
                continue;

            target.OnGrowthProgressChanged += OnTargetProgressChanged;
            target.OnFullyGrownCompleted += OnTargetFullyGrown;
        }

        BeginJobProgressUi();
        PushUi();
    }

    public override void StopTracking()
    {
        if (!tracking)
            return;

        tracking = false;

        foreach (GooHitGrowable target in targets)
        {
            if (target == null)
                continue;

            target.OnGrowthProgressChanged -= OnTargetProgressChanged;
            target.OnFullyGrownCompleted -= OnTargetFullyGrown;
        }

        EndJobProgressUi();
    }

    public override void CompleteRemaining()
    {
        foreach (GooHitGrowable target in targets)
        {
            if (target == null || target.IsFullyGrown)
                continue;

            target.DebugSetFullyGrown();
        }
    }

    public override void MarkCompleted()
    {
        StopTracking();
    }

    void OnTargetProgressChanged()
    {
        PushUi();
    }

    void OnTargetFullyGrown()
    {
        PushUi();
    }

    void PushUi()
    {
        NotifyProgressChanged(NormalizedProgress);
        PushJobProgressUi();
    }

    float GetTargetsProgress()
    {
        if (targets.Count == 0)
            return 1f;

        float progressSum = 0f;

        foreach (GooHitGrowable target in targets)
        {
            if (target != null)
                progressSum += GetTargetProgressContribution(target);
        }

        return progressSum / targets.Count;
    }

    static float GetTargetProgressContribution(GooHitGrowable target)
    {
        if (target.IsFullyGrown)
            return 1f;

        return target.GrowthProgress01;
    }
}
