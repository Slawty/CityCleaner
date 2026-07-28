using UnityEngine;

public class WaitJob : Job
{
    [SerializeField] JobCompletionCondition condition;
    [SerializeField] PowerWasherUpgradeStation upgradeStation;

    public override bool UsesProgressBar => false;
    public override float NormalizedProgress => condition != null && condition.IsMet ? 1f : 0f;

    void Awake()
    {
        if (condition == null)
            condition = GetComponent<JobCompletionCondition>();
    }

    public override void StartTracking()
    {
        if (condition == null)
        {
            Debug.LogError($"{nameof(WaitJob)} on {name}: {nameof(condition)} is not assigned.", this);
            return;
        }

        condition.Changed += HandleConditionChanged;
        condition.StartListening();
        upgradeStation?.SetAvailable(true);
        BeginJobReminderUi();
        CheckCompletion();
    }

    public override void StopTracking()
    {
        if (condition != null)
        {
            condition.Changed -= HandleConditionChanged;
            condition.StopListening();
        }

        EndJobReminderUi();
    }

    public override void CompleteRemaining()
    {
    }

    public override void MarkCompleted()
    {
        StopTracking();
    }

    void HandleConditionChanged()
    {
        CheckCompletion();
    }

    void CheckCompletion()
    {
        PushJobReminderUi();
        NotifyProgressChanged(NormalizedProgress);
    }

    protected override Transform GetWaypointTargetTransform()
    {
        if (condition != null && condition.IsMet)
            return null;

        if (waypointTarget != null)
            return waypointTarget;

        return condition != null ? condition.GetWaypointTransform() : null;
    }
}
