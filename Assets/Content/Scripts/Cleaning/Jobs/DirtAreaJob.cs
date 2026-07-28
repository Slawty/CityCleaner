using UnityEngine;
using System.Collections.Generic;

public class DirtAreaJob : Job
{
    [SerializeField] DirtArea targetArea;

    public DirtArea TargetArea => targetArea;

    public override float NormalizedProgress => targetArea != null ? targetArea.NormalizedProgress : 1f;

    public override float CompletionFraction =>
        targetArea != null ? targetArea.JobCompletionFraction : base.CompletionFraction;

    public override void StartTracking()
    {
        if (targetArea == null)
        {
            Debug.LogError($"{nameof(DirtAreaJob)} on {name}: {nameof(targetArea)} is not assigned.", this);
            return;
        }

        targetArea.SetJobTargetActive(true);
        targetArea.OnAreaProgressChanged.AddListener(HandleAreaProgressChanged);
        BeginJobProgressUi();
    }

    public override void StopTracking()
    {
        if (targetArea == null)
            return;

        targetArea.OnAreaProgressChanged.RemoveListener(HandleAreaProgressChanged);
        targetArea.SetJobTargetActive(false);
        EndJobProgressUi();
    }

    public override void MarkCompleted()
    {
        targetArea?.SetJobCompleted();
        StopTracking();
    }

    public override void CompleteRemaining()
    {
        targetArea?.CompleteAllRemainingTargets();
    }

    void HandleAreaProgressChanged(float progress)
    {
        NotifyProgressChanged(progress);
        PushJobProgressUi();
    }

    public override void CollectIncompletePaintables(List<GPUPaintableObject> results)
    {
        targetArea?.CollectIncompletePaintables(results);
    }

    protected override Transform GetWaypointTargetTransform()
    {
        if (waypointTarget != null)
            return waypointTarget;

        if (targetArea == null || !targetArea.ShouldShowWaypoint())
            return null;

        return targetArea.transform;
    }
}
