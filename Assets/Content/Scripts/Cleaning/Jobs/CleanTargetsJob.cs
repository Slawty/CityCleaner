using System.Collections.Generic;
using UnityEngine;

public class CleanTargetsJob : Job
{
    [SerializeField] List<GPUPaintableObject> targets = new();

    int completedCount;
    bool tracking;

    public IReadOnlyList<GPUPaintableObject> Targets => targets;

    public override float NormalizedProgress =>
        targets.Count == 0 ? 1f : (float)completedCount / targets.Count;

    public override void StartTracking()
    {
        if (tracking)
            return;

        tracking = true;
        completedCount = 0;

        foreach (GPUPaintableObject target in targets)
        {
            if (target == null)
                continue;

            if (target.isClean)
                completedCount++;
            else
                target.OnCleaned += OnTargetCleaned;
        }

        BeginJobProgressUi();
    }

    public override void StopTracking()
    {
        if (!tracking)
            return;

        tracking = false;

        foreach (GPUPaintableObject target in targets)
        {
            if (target != null)
                target.OnCleaned -= OnTargetCleaned;
        }

        EndJobProgressUi();
    }

    public override void CompleteRemaining()
    {
        foreach (GPUPaintableObject target in targets)
        {
            if (target == null || target.isClean)
                continue;

            if (!target.IsInitialized)
                target.Initialize(128);

            target.SetClean();
        }
    }

    public override void MarkCompleted()
    {
        StopTracking();
    }

    void OnTargetCleaned()
    {
        completedCount++;
        PushUi();
    }

    void PushUi()
    {
        NotifyProgressChanged(NormalizedProgress);
        PushJobProgressUi();
    }

    public override void CollectIncompletePaintables(List<GPUPaintableObject> results)
    {
        foreach (GPUPaintableObject target in targets)
        {
            if (target != null && !target.isClean)
                results.Add(target);
        }
    }
}
