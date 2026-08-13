using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Job : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float completionFraction = 1f;
    [SerializeField] string progressDescription;
    [Header("Chain")]
    [SerializeField] Job followUpJob;
    [SerializeField] bool guideToFollowUpClient = true;
    [SerializeField] bool runPreIntroMovesOnLoad;
    [Header("Completion")]
    [SerializeField] bool requiresTurnIn = true;
    [SerializeField] bool showCompletionPopup = true;
    [Header("Presentation")]
    [SerializeField] JobPresentation presentation = new();
    [SerializeField] JobClient speaker;
    [Header("Waypoint")]
    [SerializeField] bool useWaypoint;
    [SerializeField] protected Transform waypointTarget;
    [SerializeField] bool dismissWaypointWhenInRange;
    [SerializeField] float waypointDismissDistance = 8f;

    bool waypointDismissed;

    public virtual float CompletionFraction => completionFraction;
    public virtual bool UsesProgressBar => true;
    public bool RequiresTurnIn => requiresTurnIn;
    public bool ShowCompletionPopup => showCompletionPopup;
    public JobPresentation Presentation => presentation;
    public JobClient Speaker => speaker;
    public Job FollowUpJob => followUpJob;
    public bool GuideToFollowUpClient => guideToFollowUpClient;
    public string ProgressDescription => progressDescription;
    public abstract float NormalizedProgress { get; }

    public bool UsesChainFlow =>
        speaker != null ||
        followUpJob != null ||
        HasDialogues(presentation.introDialogues) ||
        HasDialogues(presentation.outroDialogues) ||
        presentation.movesBeforeIntro is { Length: > 0 } ||
        presentation.movesAfterOutro is { Length: > 0 } ||
        presentation.payRewardOnComplete;

    public event Action<float> OnProgressChanged;

    void Start()
    {
        if (!runPreIntroMovesOnLoad)
            return;

        NpcMoveRunner.Run(presentation.movesBeforeIntro);
        Managers.Jobs?.MarkPreIntroMovesRan(this);
    }

    static bool HasDialogues(string[] dialogues) => dialogues != null && dialogues.Length > 0;

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

    public void RenameAsDone()
    {
        const string donePrefix = "(done) ";
        if (gameObject.name.StartsWith(donePrefix))
            return;

        gameObject.name = donePrefix + gameObject.name;
    }

    public virtual void OnTurnedIn()
    {
    }

    public virtual void CollectIncompletePaintables(List<GPUPaintableObject> results)
    {
    }

    public virtual bool HasIncompleteHighlightableTargets()
    {
        return false;
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
