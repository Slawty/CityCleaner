using UnityEngine;

public enum JobSpeechAction
{
    None,
    AcceptJob,
    TurnInJob
}

public class JobManager : MonoBehaviour
{
    [SerializeField] JobTargetHighlighter targetHighlighter;

    JobClient pendingClient;
    JobSpeechAction pendingSpeechAction;
    JobClient activeClient;
    Job activeJob;

    public Job ActiveJob => activeJob;
    public DirtArea ActiveArea => (activeJob as DirtAreaJob)?.TargetArea;
    public bool HasActiveJob => activeClient != null && activeClient.State == JobClientState.Active;

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

        if (activeClient != null && activeClient != pendingClient)
        {
            activeClient.SetState(JobClientState.Available);
            StopTrackingActiveJob();
        }

        activeClient = pendingClient;
        activeJob = activeClient.Job;
        activeClient.SetState(JobClientState.Active);

        activeJob.OnProgressChanged += OnActiveJobProgressChanged;
        activeJob.StartTracking();
    }

    void TurnInJob()
    {
        if (pendingClient == null)
            return;

        pendingClient.PayReward();
        pendingClient.SetState(JobClientState.TurnedIn);

        if (activeClient == pendingClient)
            activeClient = null;
    }

    void OnActiveJobProgressChanged(float progress)
    {
        if (activeJob == null || activeClient == null)
            return;

        if (progress < activeJob.CompletionFraction)
            return;

        FinishJobObjectives();
    }

    void FinishJobObjectives()
    {
        if (activeJob == null || activeClient == null)
            return;

        activeJob.OnProgressChanged -= OnActiveJobProgressChanged;
        activeJob.CompleteRemaining();
        activeJob.MarkCompleted();

        activeClient.SetState(JobClientState.CompletedPendingTurnIn);
        Managers.UI.ShowInfoText("Job Completed");

        activeJob = null;
        ClearTargetHighlights();
    }

    void StopTrackingActiveJob()
    {
        if (activeJob == null)
            return;

        activeJob.OnProgressChanged -= OnActiveJobProgressChanged;
        activeJob.StopTracking();
        activeJob = null;
        ClearTargetHighlights();
    }

    void ClearTargetHighlights()
    {
        if (targetHighlighter != null)
            targetHighlighter.StopHighlight();
    }
}
