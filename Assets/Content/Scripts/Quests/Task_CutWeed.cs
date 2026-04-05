using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Task_CutWeed : QuestTask
{
    // public override string Name => "Cut Weed";
    [SerializeField] Transform cuttableWeedRoot;
    List<CuttableGrass> weedObjects;
    int cutCounter;

    public override void StartTask()
    {
        weedObjects = cuttableWeedRoot.GetComponentsInChildren<CuttableGrass>().ToList();
        RegisterEvents();
    }

    void OnWeedCut(CuttableGrass weed)
    {
        if (!weedObjects.Contains(weed))
            return;

        cutCounter++;
        Debug.Log($"Weed Cut: {weed.name}: {GetProgressString()}");

        OnProgressChanged?.Invoke();

        if (cutCounter >= weedObjects.Count)
        {
            CompleteTask();
        }
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
        foreach (var weed in weedObjects)
        {
            weed.OnCut += OnWeedCut;
        }
    }

    void UnregisterEvents()
    {
        foreach (var weed in weedObjects)
        {
            weed.OnCut -= OnWeedCut;
        }
    }

    public override float GetProgressPercentage()
    {
        return (float)cutCounter / (float)weedObjects.Count;
    }

    public override string GetProgressString()
    {
        return $"{cutCounter}/{weedObjects.Count}"; ;
    }
}
