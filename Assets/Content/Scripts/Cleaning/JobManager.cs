using UnityEngine;

public enum JobSpeechAction
{
    None,
    AcceptJob,
    TurnInJob
}

public class JobManager : MonoBehaviour
{
    JobClient pendingClient;
    JobSpeechAction pendingSpeechAction;
    JobClient activeClient;
    DirtArea activeArea;

    public DirtArea ActiveArea => activeArea;
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

        if (activeClient != null && activeClient != pendingClient)
        {
            activeClient.SetState(JobClientState.Available);
            StopTrackingActiveArea();
        }

        activeClient = pendingClient;
        activeArea = activeClient.TargetArea;
        activeClient.SetState(JobClientState.Active);

        activeArea.SetJobTargetActive(true);
        activeArea.OnAreaProgressChanged.AddListener(OnActiveAreaProgressChanged);
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

    void OnActiveAreaProgressChanged(float progress)
    {
        if (activeArea == null || activeClient == null)
            return;

        if (progress < activeArea.JobCompletionFraction)
            return;

        FinishJobObjectives();
    }

    void FinishJobObjectives()
    {
        if (activeArea == null || activeClient == null)
            return;

        activeArea.OnAreaProgressChanged.RemoveListener(OnActiveAreaProgressChanged);
        activeArea.CompleteAllRemainingTargets();
        activeArea.SetJobCompleted();

        activeClient.SetState(JobClientState.CompletedPendingTurnIn);
        Managers.UI.ShowInfoText("Job Completed");

        activeArea = null;
    }

    void StopTrackingActiveArea()
    {
        if (activeArea == null)
            return;

        activeArea.OnAreaProgressChanged.RemoveListener(OnActiveAreaProgressChanged);
        activeArea.SetJobTargetActive(false);
        activeArea = null;
    }
}
