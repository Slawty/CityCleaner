using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CityCleaner/QuestData")]
public class QuestData : ScriptableObject
{
    public string questID;
    public string title;
    [TextArea] public string description;
    public int rewardMoney;
    public QuestInstance Instance { get; private set; }

    public void SetQuestInstance(QuestInstance questInstance)
    {
        if (Instance != null)
            Debug.LogError($"Quest Instance of QuestData {title} is already set");
        Instance = questInstance;
    }

    [ContextMenu("Reset Values")]
    public void ResetValues()
    {
        Instance = null;
    }

}

public enum TaskType
{
    CleanArea,
    CollectTrash,
    RemoveWeeds
}