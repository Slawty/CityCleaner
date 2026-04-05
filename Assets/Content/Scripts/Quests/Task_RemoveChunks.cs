using UnityEngine;
using System.Collections.Generic;

public class Task_RemoveChunks : QuestTask
{
    // public override string Name => "Remove Chunks";
    public int ChunksAmount = 0;
    [SerializeField] TrashContainer container;
    int collectedCounter;

    public override void StartTask()
    {
        container.OnTrashCollected += OnTrashCollected;
    }

    void OnTrashCollected(PickupInteractable pickup)
    {
        if (collectedCounter >= ChunksAmount)
            return;

        collectedCounter++;
        Debug.Log($"Chunk collected: {pickup.name}: {GetProgressString()}");

        OnProgressChanged?.Invoke();

        if (collectedCounter >= ChunksAmount)
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
        return (float)collectedCounter / (float)ChunksAmount;
    }

    public override string GetProgressString()
    {
        return $"{collectedCounter}/{ChunksAmount}"; ;
    }
}
