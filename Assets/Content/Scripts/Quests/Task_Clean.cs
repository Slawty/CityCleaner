using UnityEngine;
using System.Collections.Generic;

public class Task_Clean : QuestTask
{
    // public override string Name => "Clean House";
    [SerializeField] List<GPUPaintableObject> cleanableObjects;
    float totalProgress;

    public override void StartTask()
    {
        RegisterEvents();
    }

    void CompleteTask()
    {
        Debug.Log($"Task completed: {Name}");
        OnTaskCompleted?.Invoke();
        IsCompleted = true;
        UnregisterEvents();
    }

    void RegisterEvents()
    {
        foreach (var cleanable in cleanableObjects)
        {
            cleanable.OnProgress += OnCleanableProgress;
        }
    }

    void UnregisterEvents()
    {
        foreach (var cleanable in cleanableObjects)
        {
            cleanable.OnProgress -= OnCleanableProgress;
        }
    }

    void OnCleanableProgress()
    {
        totalProgress = 0;

        foreach (var cleanable in cleanableObjects)
        {
            totalProgress += cleanable.GetCleanPercent();
        }

        totalProgress /= cleanableObjects.Count;

        OnProgressChanged?.Invoke();
    }

    public override float GetProgressPercentage()
    {
        return totalProgress;
    }

    public override string GetProgressString()
    {
        return $"{Mathf.RoundToInt(totalProgress * 100f)}%"; ;
    }
}
