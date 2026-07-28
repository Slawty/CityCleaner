using System.Collections.Generic;
using UnityEngine;

public class JobsProgressUI : MonoBehaviour
{
    [SerializeField] ProgressBar progressBarPrefab;
    [SerializeField] JobReminderItem jobReminderTemplate;
    [SerializeField] JobReminderItem returnMessageTemplate;
    [SerializeField] Transform listRoot;

    readonly Dictionary<Job, ProgressBar> barsByJob = new();
    readonly Dictionary<Job, JobReminderItem> remindersByJob = new();
    readonly Dictionary<JobClient, JobReminderItem> returnMessagesByClient = new();

    void Awake()
    {
        if (listRoot == null)
            listRoot = transform;

        HideTemplates();
    }

    void HideTemplates()
    {
        ProgressBar[] existingBars = listRoot.GetComponentsInChildren<ProgressBar>(true);
        foreach (ProgressBar bar in existingBars)
            bar.gameObject.SetActive(false);

        if (jobReminderTemplate != null)
            jobReminderTemplate.gameObject.SetActive(false);

        if (returnMessageTemplate != null)
            returnMessageTemplate.gameObject.SetActive(false);
    }

    public void RegisterJob(Job job)
    {
        if (job == null || barsByJob.ContainsKey(job))
            return;

        ProgressBar bar = Instantiate(progressBarPrefab, listRoot);
        bar.gameObject.SetActive(true);
        bar.ResetProgress();
        bar.SetDescription(job.ProgressDescription);
        barsByJob[job] = bar;
    }

    public void UnregisterJob(Job job)
    {
        if (job == null || !barsByJob.TryGetValue(job, out ProgressBar bar))
            return;

        barsByJob.Remove(job);
        Destroy(bar.gameObject);
    }

    public void RegisterReminder(Job job)
    {
        if (job == null || jobReminderTemplate == null || remindersByJob.ContainsKey(job))
            return;

        JobReminderItem reminder = Instantiate(jobReminderTemplate, listRoot);
        reminder.gameObject.SetActive(true);
        reminder.SetDescription(job.ProgressDescription);
        remindersByJob[job] = reminder;
    }

    public void UnregisterReminder(Job job)
    {
        if (job == null || !remindersByJob.TryGetValue(job, out JobReminderItem reminder))
            return;

        remindersByJob.Remove(job);
        Destroy(reminder.gameObject);
    }

    public void SetJobProgress(Job job, float percent, string description = null)
    {
        if (job == null || !barsByJob.TryGetValue(job, out ProgressBar bar))
            return;

        bar.SetPercent(percent, onlyIncrease: true);
        if (description != null)
            bar.SetDescription(description);
    }

    public void SetReminderDescription(Job job, string description)
    {
        if (job == null || !remindersByJob.TryGetValue(job, out JobReminderItem reminder))
            return;

        reminder.SetDescription(description);
    }

    public void ResetJobProgress(Job job)
    {
        if (job == null || !barsByJob.TryGetValue(job, out ProgressBar bar))
            return;

        bar.ResetProgress();
    }

    public void RefreshReturnMessages()
    {
        JobClient[] clients = FindObjectsByType<JobClient>(FindObjectsSortMode.None);
        HashSet<JobClient> pendingTurnInClients = new();

        foreach (JobClient client in clients)
        {
            if (client.State == JobClientState.CompletedPendingTurnIn)
                pendingTurnInClients.Add(client);
        }

        List<JobClient> staleClients = new();
        foreach (JobClient client in returnMessagesByClient.Keys)
        {
            if (!pendingTurnInClients.Contains(client))
                staleClients.Add(client);
        }

        foreach (JobClient client in staleClients)
            UnregisterReturnMessage(client);

        foreach (JobClient client in pendingTurnInClients)
        {
            if (returnMessagesByClient.ContainsKey(client))
                returnMessagesByClient[client].SetDescription(GetReturnMessageText(client));
            else
                RegisterReturnMessage(client);
        }
    }

    public void RegisterReturnMessage(JobClient client)
    {
        if (client == null || returnMessageTemplate == null || returnMessagesByClient.ContainsKey(client))
            return;

        JobReminderItem message = Instantiate(returnMessageTemplate, listRoot);
        message.gameObject.SetActive(true);
        message.SetDescription(GetReturnMessageText(client));
        returnMessagesByClient[client] = message;
    }

    public void UnregisterReturnMessage(JobClient client)
    {
        if (client == null || !returnMessagesByClient.TryGetValue(client, out JobReminderItem message))
            return;

        returnMessagesByClient.Remove(client);
        Destroy(message.gameObject);
    }

    static string GetReturnMessageText(JobClient client)
    {
        return $"Return to {client.ReturnDestinationName}";
    }
}
