using System.Collections.Generic;
using UnityEngine;

public class CleanTargetsJob : Job
{
    [SerializeField] List<GPUPaintableObject> targets = new();
    [Header("Completion")]
    [SerializeField] List<GPUPaintableObject> additionalCleanOnComplete = new();

    bool tracking;

    public IReadOnlyList<GPUPaintableObject> Targets => targets;

    public override float NormalizedProgress => GetTargetsProgress();

    public override void StartTracking()
    {
        if (tracking)
            return;

        tracking = true;
        ResetWaypointDismissal();

        foreach (GPUPaintableObject target in targets)
        {
            if (target == null || target.isClean)
                continue;

            if (target.UseContinuousProgress)
                target.OnProgress += OnTargetProgressChanged;
            else
                target.OnCleaned += OnTargetCleaned;
        }

        BeginJobProgressUi();
        PushUi();
    }

    public override void StopTracking()
    {
        if (!tracking)
            return;

        tracking = false;

        foreach (GPUPaintableObject target in targets)
        {
            if (target == null)
                continue;

            target.OnProgress -= OnTargetProgressChanged;
            target.OnCleaned -= OnTargetCleaned;
        }

        EndJobProgressUi();
    }

    public override void CompleteRemaining()
    {
        foreach (GPUPaintableObject target in targets)
            CleanPaintableIfDirty(target, playSuccessSound: true);

        foreach (GPUPaintableObject paintable in additionalCleanOnComplete)
            CleanPaintableIfDirty(paintable, playSuccessSound: false);
    }

    static void CleanPaintableIfDirty(GPUPaintableObject paintable, bool playSuccessSound)
    {
        if (paintable == null || paintable.isClean)
            return;

        if (!paintable.IsInitialized)
            paintable.Initialize(128);

        paintable.SetClean(playSuccessSound);
    }

    public override void MarkCompleted()
    {
        StopTracking();
    }

    void OnTargetProgressChanged()
    {
        PushUi();
    }

    void OnTargetCleaned()
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

        foreach (GPUPaintableObject target in targets)
        {
            if (target != null)
                progressSum += target.GetProgressContribution();
        }

        return progressSum / targets.Count;
    }

    public override void CollectIncompletePaintables(List<GPUPaintableObject> results)
    {
        foreach (GPUPaintableObject target in targets)
        {
            if (target != null && !target.isClean)
                results.Add(target);
        }
    }

    public override bool HasIncompleteHighlightableTargets()
    {
        foreach (GPUPaintableObject target in targets)
        {
            if (target != null && !target.isClean)
                return true;
        }

        return false;
    }
}
