using System;
using System.Collections.Generic;
using UnityEngine;

public class DestroySplitablesJob : Job
{
    [SerializeField] Transform targetsRoot;
    [SerializeField] List<SplitableObject> targets = new();

    readonly List<SplitableObject> resolvedTargets = new();
    bool tracking;

    public override float NormalizedProgress => GetTargetsProgress();

    public override void StartTracking()
    {
        if (tracking)
            return;

        tracking = true;
        ResetWaypointDismissal();
        BuildTargetList();

        foreach (SplitableObject target in resolvedTargets)
        {
            if (target == null || target.IsDestroyed)
                continue;

            target.Destroyed += OnTargetDestroyed;
        }

        BeginJobProgressUi();
        PushUi();
    }

    public override void StopTracking()
    {
        if (!tracking)
            return;

        tracking = false;

        foreach (SplitableObject target in resolvedTargets)
        {
            if (target == null)
                continue;

            target.Destroyed -= OnTargetDestroyed;
        }

        resolvedTargets.Clear();
        EndJobProgressUi();
    }

    public override void CompleteRemaining()
    {
        BuildTargetList();

        foreach (SplitableObject target in resolvedTargets)
        {
            if (target == null || target.IsDestroyed)
                continue;

            target.DebugDestroyNow();
        }
    }

    public override void MarkCompleted()
    {
        StopTracking();
    }

    void OnTargetDestroyed()
    {
        PushUi();
    }

    void PushUi()
    {
        NotifyProgressChanged(NormalizedProgress);
        PushJobProgressUi();
    }

    void BuildTargetList()
    {
        resolvedTargets.Clear();

        foreach (SplitableObject target in targets)
        {
            if (target == null || resolvedTargets.Contains(target))
                continue;

            resolvedTargets.Add(target);
        }

        if (targetsRoot == null)
            return;

        SplitableObject[] splitables = targetsRoot.GetComponentsInChildren<SplitableObject>(true);
        foreach (SplitableObject splitable in splitables)
        {
            if (splitable == null || resolvedTargets.Contains(splitable))
                continue;

            resolvedTargets.Add(splitable);
        }
    }

    float GetTargetsProgress()
    {
        if (resolvedTargets.Count == 0)
            BuildTargetList();

        if (resolvedTargets.Count == 0)
            return 1f;

        int destroyedCount = 0;

        foreach (SplitableObject target in resolvedTargets)
        {
            if (target == null || target.IsDestroyed)
                destroyedCount++;
        }

        return (float)destroyedCount / resolvedTargets.Count;
    }

    protected override Transform GetWaypointTargetTransform()
    {
        if (waypointTarget != null)
            return waypointTarget;

        if (targetsRoot != null)
            return targetsRoot;

        BuildTargetList();

        foreach (SplitableObject target in resolvedTargets)
        {
            if (target != null && !target.IsDestroyed)
                return target.transform;
        }

        return null;
    }
}
