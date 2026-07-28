using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Job : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float completionFraction = 1f;
    [SerializeField] string progressDescription;
    [Header("Completion")]
    [SerializeField] bool requiresTurnIn = true;
    [Header("Waypoint")]
    [SerializeField] bool useWaypoint;
    [SerializeField] protected Transform waypointTarget;
    [SerializeField] bool dismissWaypointWhenInRange;
    [SerializeField] float waypointDismissDistance = 8f;

    bool waypointDismissed;

    public virtual float CompletionFraction => completionFraction;
    public virtual bool UsesProgressBar => true;
    public bool RequiresTurnIn => requiresTurnIn;
    public string ProgressDescription => progressDescription;
    public abstract float NormalizedProgress { get; }

    public event Action<float> OnProgressChanged;

    protected void BeginJobProgressUi()
    {
        Managers.UI.RegisterJobProgress(this);
        PushJobProgressUi();
    }

    protected void EndJobProgressUi()
    {
        Managers.UI.UnregisterJobProgress(this);
    }

    protected void BeginJobReminderUi()
    {
        Managers.UI.RegisterJobReminder(this);
        PushJobReminderUi();
    }

    protected void EndJobReminderUi()
    {
        Managers.UI.UnregisterJobReminder(this);
    }

    protected void PushJobReminderUi()
    {
        Managers.UI.SetJobReminderDescription(this, progressDescription);
    }

    protected void PushJobProgressUi()
    {
        Managers.UI.SetJobProgress(this, NormalizedProgress * 100f, progressDescription);
    }

    protected void NotifyProgressChanged(float normalizedProgress)
    {
        OnProgressChanged?.Invoke(normalizedProgress);
    }

    public abstract void StartTracking();
    public abstract void StopTracking();
    public abstract void CompleteRemaining();
    public abstract void MarkCompleted();

    public virtual void CollectIncompletePaintables(List<GPUPaintableObject> results)
    {
    }

    public virtual Transform GetWaypointTarget()
    {
        if (!useWaypoint || waypointDismissed)
            return null;

        return GetWaypointTargetTransform();
    }

    public virtual float GetWaypointHideDistance()
    {
        return useWaypoint ? 0f : -1f;
    }

    public void ResetWaypointDismissal()
    {
        waypointDismissed = false;
    }

    public void UpdateWaypointDismissal()
    {
        if (!useWaypoint || !dismissWaypointWhenInRange || waypointDismissed)
            return;

        Transform target = GetWaypointTargetTransform();
        if (target == null)
            return;

        Transform playerTransform = Managers.Player != null ? Managers.Player.transform : null;
        if (playerTransform == null)
            return;

        if (Vector3.Distance(playerTransform.position, target.position) > waypointDismissDistance)
            return;

        waypointDismissed = true;
        Managers.Jobs?.RefreshWaypoint();
    }

    protected virtual Transform GetWaypointTargetTransform()
    {
        return waypointTarget;
    }
}
