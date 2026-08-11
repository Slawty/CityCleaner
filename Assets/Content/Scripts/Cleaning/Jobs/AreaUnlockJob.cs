using UnityEngine;

public class AreaUnlockJob : Job
{
    [SerializeField] DirtArea targetArea;
    [SerializeField] AreaBlocker barrier;
    [SerializeField, Range(0.01f, 1f)] float requiredCleanFraction = 0.5f;

    public DirtArea TargetArea => targetArea;
    public float RequiredCleanFraction => GetRequiredCleanFraction();

    public override float NormalizedProgress
    {
        get
        {
            if (targetArea == null)
                return 1f;

            float requiredFraction = GetRequiredCleanFraction();
            return Mathf.Clamp01(targetArea.NormalizedProgress / requiredFraction);
        }
    }

    public override void OnTurnedIn()
    {
        if (barrier != null)
            barrier.OpenBarrier();
    }

    public override void StartTracking()
    {
        if (targetArea == null)
        {
            Debug.LogError($"{nameof(AreaUnlockJob)} on {name}: {nameof(targetArea)} is not assigned.", this);
            return;
        }

        targetArea.OnAreaProgressChanged.AddListener(HandleAreaProgressChanged);
        BeginJobProgressUi();
        PushJobProgressUi();
    }

    public override void StopTracking()
    {
        if (targetArea == null)
            return;

        targetArea.OnAreaProgressChanged.RemoveListener(HandleAreaProgressChanged);
        EndJobProgressUi();
    }

    public override void MarkCompleted()
    {
        StopTracking();
    }

    public override void CompleteRemaining()
    {
    }

    void HandleAreaProgressChanged(float progress)
    {
        NotifyProgressChanged(NormalizedProgress);
        PushJobProgressUi();
    }

    float GetRequiredCleanFraction()
    {
        if (barrier != null)
            return barrier.RequiredCleanFraction;

        return requiredCleanFraction;
    }

    protected override Transform GetWaypointTargetTransform()
    {
        if (waypointTarget != null)
            return waypointTarget;

        if (barrier != null)
            return barrier.transform;

        return targetArea != null ? targetArea.transform : null;
    }
}
