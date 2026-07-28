using System;
using System.Collections.Generic;
using UnityEngine;

public enum JobSpeechAction
{
    None,
    AcceptJob,
    TurnInJob
}

public class JobManager : MonoBehaviour
{
    sealed class TrackedJob
    {
        public Job Job;
        public JobClient Client;
        public Action<float> ProgressHandler;
        public Action OnObjectivesCompleted;
        public bool IsSequenceStep;
    }

    [SerializeField] JobTargetHighlighter targetHighlighter;

    readonly List<TrackedJob> activeJobs = new();
    readonly List<Job> activeJobsScratch = new();

    JobClient pendingClient;
    JobSpeechAction pendingSpeechAction;

    public Job ActiveJob => activeJobs.Count > 0 ? activeJobs[activeJobs.Count - 1].Job : null;
    public IReadOnlyList<Job> ActiveJobs => GetActiveJobsSnapshot();
    public DirtArea ActiveArea
    {
        get
        {
            for (int index = activeJobs.Count - 1; index >= 0; index--)
            {
                if (activeJobs[index].Job is DirtAreaJob dirtAreaJob)
                    return dirtAreaJob.TargetArea;
            }

            return null;
        }
    }

    public bool HasActiveJob => activeJobs.Count > 0;

    void Update()
    {
        foreach (TrackedJob tracked in activeJobs)
            tracked.Job.UpdateWaypointDismissal();
    }

    public void CompleteActiveJobDebug()
    {
        if (activeJobs.Count == 0)
            return;

        TrackedJob tracked = activeJobs[activeJobs.Count - 1];
        OnJobProgressChanged(tracked, 1f);
    }

    public void OfferJob(JobClient client)
    {
        if (client == null)
        {
            Debug.LogError($"{nameof(JobManager)}.{nameof(OfferJob)}: client is required.", this);
            return;
        }

        pendingClient = client;
        pendingSpeechAction = JobSpeechAction.AcceptJob;
        Managers.Speech.Show(client.OfferDialogue);
    }

    public void OfferTurnIn(JobClient client)
    {
        if (client == null)
        {
            Debug.LogError($"{nameof(JobManager)}.{nameof(OfferTurnIn)}: client is required.", this);
            return;
        }

        pendingClient = client;
        pendingSpeechAction = JobSpeechAction.TurnInJob;
        Managers.Speech.Show(client.CompletionDialogue);
    }

    public void StartJob(JobClient client)
    {
        if (client == null)
        {
            Debug.LogError($"{nameof(JobManager)}.{nameof(StartJob)}: client is required.", this);
            return;
        }

        pendingClient = client;
        AcceptNewJob();
        pendingClient = null;
    }

    public void StartGuidanceJob(Job job)
    {
        if (job == null)
        {
            Debug.LogError($"{nameof(JobManager)}.{nameof(StartGuidanceJob)}: job is required.", this);
            return;
        }

        if (IsTrackingJob(job))
            return;

        BeginTracking(new TrackedJob { Job = job, Client = null });
    }

    public void StartSequenceJob(Job job, JobClient client, Action onObjectivesCompleted)
    {
        if (job == null)
        {
            Debug.LogError($"{nameof(JobManager)}.{nameof(StartSequenceJob)}: job is required.", this);
            return;
        }

        if (IsTrackingJob(job))
        {
            foreach (TrackedJob tracked in activeJobs)
            {
                if (tracked.Job != job)
                    continue;

                tracked.IsSequenceStep = true;
                tracked.Client = client;
                tracked.OnObjectivesCompleted = onObjectivesCompleted;

                if (client != null)
                    client.SetState(JobClientState.Active);

                if (targetHighlighter != null)
                    targetHighlighter.HighlightActiveJobTargets();

                RefreshWaypoint();
                return;
            }
        }

        if (client != null)
            client.SetState(JobClientState.Active);

        BeginTracking(new TrackedJob
        {
            Job = job,
            Client = client,
            IsSequenceStep = true,
            OnObjectivesCompleted = onObjectivesCompleted
        });

        if (targetHighlighter != null)
            targetHighlighter.HighlightActiveJobTargets();
    }

    public void ClearPendingOffer()
    {
        pendingClient = null;
        pendingSpeechAction = JobSpeechAction.None;
    }

    public void OnSpeechAccepted()
    {
        switch (pendingSpeechAction)
        {
            case JobSpeechAction.AcceptJob:
                AcceptNewJob();
                break;
            case JobSpeechAction.TurnInJob:
                TurnInJob();
                break;
        }

        pendingClient = null;
        pendingSpeechAction = JobSpeechAction.None;
    }

    void AcceptNewJob()
    {
        if (pendingClient == null)
            return;

        if (pendingClient.Job == null)
        {
            Debug.LogError($"{nameof(JobManager)}.{nameof(AcceptNewJob)}: {nameof(JobClient.Job)} is not assigned on {pendingClient.name}.", pendingClient);
            return;
        }

        if (IsTrackingJob(pendingClient.Job))
            return;

        pendingClient.SetState(JobClientState.Active);
        BeginTracking(new TrackedJob { Job = pendingClient.Job, Client = pendingClient });

        if (targetHighlighter != null)
            targetHighlighter.HighlightActiveJobTargets();
    }

    void TurnInJob()
    {
        if (pendingClient == null)
            return;

        CompleteClientTurnIn(pendingClient);
        RefreshWaypoint();
    }

    public void RefreshWaypoint()
    {
        RefreshReturnMessages();

        JobClient turnInClient = FindTurnInClient();
        if (turnInClient != null)
        {
            Managers.UI.SetWaypointTurnInTarget(turnInClient.WaypointTransform);
            return;
        }

        for (int index = activeJobs.Count - 1; index >= 0; index--)
        {
            Job job = activeJobs[index].Job;
            Transform target = job.GetWaypointTarget();
            if (target == null)
                continue;

            Managers.UI.SetWaypointTarget(target, job.GetWaypointHideDistance());
            return;
        }

        Managers.UI.ClearWaypointTarget();
    }

    void RefreshReturnMessages()
    {
        Managers.UI.RefreshReturnMessages();
    }

    void BeginTracking(TrackedJob tracked)
    {
        tracked.ProgressHandler = progress => OnJobProgressChanged(tracked, progress);
        tracked.Job.OnProgressChanged += tracked.ProgressHandler;
        tracked.Job.ResetWaypointDismissal();
        tracked.Job.StartTracking();
        activeJobs.Add(tracked);
        RefreshWaypoint();
    }

    void OnJobProgressChanged(TrackedJob tracked, float progress)
    {
        if (progress < tracked.Job.CompletionFraction)
            return;

        if (tracked.IsSequenceStep)
            FinishSequenceJob(tracked);
        else if (tracked.Client != null)
            FinishClientJobObjectives(tracked);
        else
            FinishGuidanceJob(tracked);
    }

    void FinishSequenceJob(TrackedJob tracked)
    {
        StopTracking(tracked);
        tracked.Job.CompleteRemaining();
        tracked.Job.MarkCompleted();
        Managers.UI.ShowInfoText("Job Completed");
        tracked.OnObjectivesCompleted?.Invoke();
        ClearTargetHighlightsIfNeeded();
        RefreshWaypoint();
    }

    void FinishClientJobObjectives(TrackedJob tracked)
    {
        StopTracking(tracked);
        tracked.Job.CompleteRemaining();
        tracked.Job.MarkCompleted();
        Managers.UI.ShowInfoText("Job Completed");

        if (tracked.Job.RequiresTurnIn)
            tracked.Client.SetState(JobClientState.CompletedPendingTurnIn);
        else
            CompleteClientTurnIn(tracked.Client);

        ClearTargetHighlightsIfNeeded();
        RefreshWaypoint();
    }

    void CompleteClientTurnIn(JobClient client)
    {
        client.PayReward();
        client.SetState(JobClientState.TurnedIn);
    }

    void FinishGuidanceJob(TrackedJob tracked)
    {
        StopTracking(tracked);
        tracked.Job.CompleteRemaining();
        tracked.Job.MarkCompleted();
        Managers.UI.ShowInfoText("Task Completed");
        RefreshWaypoint();
    }

    void StopTracking(TrackedJob tracked)
    {
        tracked.Job.OnProgressChanged -= tracked.ProgressHandler;
        tracked.Job.StopTracking();
        activeJobs.Remove(tracked);
    }

    JobClient FindTurnInClient()
    {
        JobClient[] clients = FindObjectsByType<JobClient>(FindObjectsSortMode.None);
        foreach (JobClient client in clients)
        {
            if (client.State == JobClientState.CompletedPendingTurnIn)
                return client;
        }

        return null;
    }

    bool IsTrackingJob(Job job)
    {
        foreach (TrackedJob tracked in activeJobs)
        {
            if (tracked.Job == job)
                return true;
        }

        return false;
    }

    IReadOnlyList<Job> GetActiveJobsSnapshot()
    {
        activeJobsScratch.Clear();
        foreach (TrackedJob tracked in activeJobs)
            activeJobsScratch.Add(tracked.Job);
        return activeJobsScratch;
    }

    void ClearTargetHighlightsIfNeeded()
    {
        if (targetHighlighter == null)
            return;

        if (activeJobs.Count == 0)
            targetHighlighter.StopHighlight();
    }
}
