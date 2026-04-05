using UnityEngine;
using System.Collections.Generic;

public class Task_CollectTrash : QuestTask
{
    // public override string Name => "Collect Trash";
    [SerializeField] List<PickupInteractable> trashObjects;
    [SerializeField] TrashContainer container;
    int collectedCounter;

    public override void StartTask()
    {
        container.OnTrashCollected += OnTrashCollected;
    }

    void OnTrashCollected(PickupInteractable pickup)
    {
        if (!trashObjects.Contains(pickup))
            return;

        collectedCounter++;
        Debug.Log($"Trash collected: {pickup.name}: {GetProgressString()}");

        OnProgressChanged?.Invoke();

        if (collectedCounter >= trashObjects.Count)
        {
            CompleteTask();
        }

    }

    void CompleteTask()
    {
        Debug.Log($"Task completed: {Name}");
        OnTaskCompleted?.Invoke();
        IsCompleted = true;
        container.OnTrashCollected -= OnTrashCollected;
    }

    public override float GetProgressPercentage()
    {
        return (float)collectedCounter / (float)trashObjects.Count;
    }

    public override string GetProgressString()
    {
        return $"{collectedCounter}/{trashObjects.Count}"; ;
    }
}
