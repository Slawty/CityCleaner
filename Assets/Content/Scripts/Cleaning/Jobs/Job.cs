using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Job : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float completionFraction = 1f;
    [SerializeField] string progressDescription;

    public virtual float CompletionFraction => completionFraction;
    public string ProgressDescription => progressDescription;
    public abstract float NormalizedProgress { get; }

    public event Action<float> OnProgressChanged;

    protected void BeginJobProgressUi()
    {
        Managers.UI.ShowJobProgress(true);
        Managers.UI.ResetJobProgress();
        PushJobProgressUi();
    }

    protected void EndJobProgressUi()
    {
        Managers.UI.ShowJobProgress(false);
    }

    protected void PushJobProgressUi()
    {
        Managers.UI.SetJobProgress(NormalizedProgress * 100f, progressDescription);
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
}
