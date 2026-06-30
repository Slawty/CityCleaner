using System.Collections.Generic;
using UnityEngine;

public class JobsProgressUI : MonoBehaviour
{
    [SerializeField] ProgressBar progressBarPrefab;
    [SerializeField] Transform listRoot;

    readonly Dictionary<Job, ProgressBar> barsByJob = new();

    void Awake()
    {
        if (listRoot == null)
            listRoot = transform;

        HideTemplateBars();
    }

    void HideTemplateBars()
    {
        ProgressBar[] existingBars = listRoot.GetComponentsInChildren<ProgressBar>(true);
        foreach (ProgressBar bar in existingBars)
            bar.gameObject.SetActive(false);
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

    public void SetJobProgress(Job job, float percent, string description = null)
    {
        if (job == null || !barsByJob.TryGetValue(job, out ProgressBar bar))
            return;

        bar.SetPercent(percent, onlyIncrease: true);
        if (description != null)
            bar.SetDescription(description);
    }

    public void ResetJobProgress(Job job)
    {
        if (job == null || !barsByJob.TryGetValue(job, out ProgressBar bar))
            return;

        bar.ResetProgress();
    }

}
