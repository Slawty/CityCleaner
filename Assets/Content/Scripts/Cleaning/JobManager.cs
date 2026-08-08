using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        public bool IsChainJob;
    }

    [SerializeField] JobTargetHighlighter targetHighlighter;
    [SerializeField] Job postChainGuidanceJob;
    [SerializeField] bool triggerTutorialOnComplete;

    readonly List<TrackedJob> activeJobs = new();
    readonly List<Job> activeJobsScratch = new();

    JobClient pendingClient;
    JobSpeechAction pendingSpeechAction;

    Job chainFirstJob;
    Job currentChainJob;
    bool chainActive;
    bool waitingForChainOutro;
    Job preIntroMovesRanForJob;
    CancellationTokenSource chainFlowCts;

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

    void OnDestroy()
    {
        CancelChainFlow();
    }

    public void MarkPreIntroMovesRan(Job job)
    {
        preIntroMovesRanForJob = job;
    }

    public void CompleteActiveJobDebug()
    {
        if (activeJobs.Count == 0)
            return;

        TrackedJob tracked = activeJobs[activeJobs.Count - 1];
        OnJobProgressChanged(tracked, 1f);
    }

    public void StartJobChain(Job firstJob)
    {
        if (firstJob == null)
        {
            Debug.LogError($"{nameof(JobManager)}.{nameof(StartJobChain)}: job is required.", this);
            return;
        }

        if (chainActive)
            return;

        chainFirstJob = firstJob;
        currentChainJob = firstJob;
        chainActive = true;
        RunBeginCurrentChainJobAsync(BeginChainFlow());
    }

    void RunBeginCurrentChainJobAsync(CancellationToken cancellationToken)
    {
        BeginCurrentChainJobAsync(cancellationToken).Forget();
    }

    public bool TryStartPendingChainJobFromTalk(JobClient client)
    {
        if (!chainActive || currentChainJob == null || client == null)
            return false;

        if (IsTrackingJob(currentChainJob))
            return false;

        if (currentChainJob.Speaker == client)
        {
            RunBeginCurrentChainJobAsync(BeginChainFlow());
            return true;
        }

        Job followUpJob = currentChainJob.FollowUpJob;
        JobPresentation presentation = currentChainJob.Presentation;
        if (followUpJob == null || followUpJob.Speaker != client)
            return false;

        if (presentation.movesAfterOutro is { Length: > 0 })
            return false;

        AdvanceChainToFollowUpJob();
        RunBeginCurrentChainJobAsync(BeginChainFlow());
        return true;
    }

    void AdvanceChainToFollowUpJob()
    {
        Job nextJob = currentChainJob.FollowUpJob;
        if (nextJob == null)
            return;

        waitingForChainOutro = false;
        currentChainJob = nextJob;

        if (currentChainJob.Speaker != null)
            currentChainJob.Speaker.SetState(JobClientState.Available);
    }

    public bool OfferChainJobOutro(JobClient client)
    {
        if (!chainActive || !waitingForChainOutro || client == null || currentChainJob == null)
            return false;

        if (currentChainJob.Speaker != client)
            return false;

        JobPresentation presentation = currentChainJob.Presentation;
        if (presentation.movesAfterOutro is { Length: > 0 })
            Managers.Speech.SuppressDialogueFacingRestore();

        if (HasDialogues(presentation.outroDialogues))
            Managers.Speech.ShowDialogueSequence(presentation.outroDialogues, OnChainOutroFinished);
        else
            OnChainOutroFinished();

        return true;
    }

    public void OfferJob(JobClient client)
    {
        if (client == null)
        {
            Debug.LogError($"{nameof(JobManager)}.{nameof(OfferJob)}: client is required.", this);
            return;
        }

        if (client.Job == null)
        {
            Debug.LogError($"{nameof(JobManager)}.{nameof(OfferJob)}: {nameof(JobClient.Job)} is not assigned on {client.name}.", client);
            return;
        }

        pendingClient = client;
        pendingSpeechAction = JobSpeechAction.AcceptJob;
        ShowJobDialogues(client.Job.Presentation.introDialogues, OnSpeechAccepted);
    }

    public void OfferTurnIn(JobClient client)
    {
        if (client == null)
        {
            Debug.LogError($"{nameof(JobManager)}.{nameof(OfferTurnIn)}: client is required.", this);
            return;
        }

        if (client.Job == null)
        {
            Debug.LogError($"{nameof(JobManager)}.{nameof(OfferTurnIn)}: {nameof(JobClient.Job)} is not assigned on {client.name}.", client);
            return;
        }

        pendingClient = client;
        pendingSpeechAction = JobSpeechAction.TurnInJob;
        ShowJobDialogues(client.Job.Presentation.outroDialogues, OnSpeechAccepted);
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

    void BeginCurrentChainJob()
    {
        RunBeginCurrentChainJobAsync(BeginChainFlow());
    }

    CancellationToken BeginChainFlow()
    {
        CancelChainFlow();
        chainFlowCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        return chainFlowCts.Token;
    }

    void CancelChainFlow()
    {
        if (chainFlowCts == null)
            return;

        chainFlowCts.Cancel();
        chainFlowCts.Dispose();
        chainFlowCts = null;
    }

    async UniTask BeginCurrentChainJobAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (currentChainJob == null)
            {
                Debug.LogError($"{nameof(JobManager)} on {name}: current chain job is missing.", this);
                return;
            }

            JobPresentation presentation = currentChainJob.Presentation;
            bool skipPreIntroMoves = currentChainJob == preIntroMovesRanForJob;
            if (!skipPreIntroMoves)
                await NpcMoveRunner.RunAsync(presentation.movesBeforeIntro);

            cancellationToken.ThrowIfCancellationRequested();

            preIntroMovesRanForJob = null;

            if (HasDialogues(presentation.introDialogues))
                Managers.Speech.ShowDialogueSequence(presentation.introDialogues, OnChainIntroFinished);
            else
                OnChainIntroFinished();
        }
        catch (OperationCanceledException)
        {
        }
    }

    void OnChainIntroFinished()
    {
        JobPresentation presentation = currentChainJob.Presentation;

        if (HasOnJobStartedListeners(presentation))
            Managers.Speech.SuppressDialogueFacingRestore();

        presentation.onJobStarted?.Invoke();
        StartChainJobTracking(currentChainJob, currentChainJob.Speaker);
    }

    static bool HasOnJobStartedListeners(JobPresentation presentation)
    {
        return presentation.onJobStarted != null
            && presentation.onJobStarted.GetPersistentEventCount() > 0;
    }

    void StartChainJobTracking(Job job, JobClient client)
    {
        if (IsTrackingJob(job))
        {
            foreach (TrackedJob tracked in activeJobs)
            {
                if (tracked.Job != job)
                    continue;

                tracked.IsChainJob = true;
                tracked.Client = client;

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
            IsChainJob = true
        });

        if (targetHighlighter != null)
            targetHighlighter.HighlightActiveJobTargets();
    }

    void OnChainJobObjectivesCompleted()
    {
        currentChainJob.Presentation.onJobCompleted?.Invoke();

        if (currentChainJob.Speaker != null && currentChainJob.RequiresTurnIn)
        {
            waitingForChainOutro = true;
            currentChainJob.Speaker.SetState(JobClientState.CompletedPendingTurnIn);
        }
        else
            RunOnChainOutroFinishedAsync(BeginChainFlow(), startFollowUpIntro: false);
    }

    void OnChainOutroFinished()
    {
        RunOnChainOutroFinishedAsync(BeginChainFlow(), startFollowUpIntro: true);
    }

    void RunOnChainOutroFinishedAsync(CancellationToken cancellationToken, bool startFollowUpIntro)
    {
        OnChainOutroFinishedAsync(cancellationToken, startFollowUpIntro).Forget();
    }

    async UniTaskVoid OnChainOutroFinishedAsync(CancellationToken cancellationToken, bool startFollowUpIntro)
    {
        try
        {
            waitingForChainOutro = false;
            JobPresentation presentation = currentChainJob.Presentation;
            await NpcMoveRunner.RunAsync(presentation.movesAfterOutro);

            cancellationToken.ThrowIfCancellationRequested();

            if (presentation.payRewardOnComplete && currentChainJob.Speaker != null)
                currentChainJob.Speaker.PayReward();

            Job nextJob = currentChainJob.FollowUpJob;
            if (nextJob == null)
            {
                if (currentChainJob.Speaker != null)
                    currentChainJob.Speaker.SetState(JobClientState.TurnedIn);

                chainActive = false;
                chainFirstJob = null;
                currentChainJob = null;

                if (postChainGuidanceJob != null)
                    StartGuidanceJob(postChainGuidanceJob);
                else if (triggerTutorialOnComplete)
                    Managers.Tutorial.NotifyJobChainCompleted();

                return;
            }

            currentChainJob = nextJob;
            if (currentChainJob.Speaker != null)
                currentChainJob.Speaker.SetState(JobClientState.Available);

            if (!startFollowUpIntro || presentation.movesAfterOutro is { Length: > 0 })
            {
                RefreshWaypoint();
                return;
            }

            await BeginCurrentChainJobAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
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

        if (tracked.IsChainJob)
            FinishChainJob(tracked);
        else if (tracked.Client != null)
            FinishClientJobObjectives(tracked);
        else
            FinishGuidanceJob(tracked);
    }

    void FinishChainJob(TrackedJob tracked)
    {
        StopTracking(tracked);
        tracked.Job.CompleteRemaining();
        tracked.Job.MarkCompleted();
        ShowJobCompletedPopup(tracked.Job, "Job Completed");
        OnChainJobObjectivesCompleted();
        ClearTargetHighlightsIfNeeded();
        RefreshWaypoint();
    }

    void FinishClientJobObjectives(TrackedJob tracked)
    {
        StopTracking(tracked);
        tracked.Job.CompleteRemaining();
        tracked.Job.MarkCompleted();
        ShowJobCompletedPopup(tracked.Job, "Job Completed");

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
        ShowJobCompletedPopup(tracked.Job, "Task Completed");
        RefreshWaypoint();
    }

    void ShowJobCompletedPopup(Job job, string message)
    {
        if (job.ShowCompletionPopup)
            Managers.UI.ShowInfoText(message);
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

    static bool HasDialogues(string[] dialogues)
    {
        return dialogues != null && dialogues.Length > 0;
    }

    static void ShowJobDialogues(string[] dialogues, Action onFinished)
    {
        if (HasDialogues(dialogues))
            Managers.Speech.ShowDialogueSequence(dialogues, onFinished);
        else
            onFinished?.Invoke();
    }
}
