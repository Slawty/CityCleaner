using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class QuestManager : MonoBehaviour
{
    [SerializeField] QuestTracker questTracker;
    private QuestInstance activeQuest;

    public void StartQuest(QuestData questData)
    {
        Debug.Log($"Quest started: {questData.title}");
        if (!questData.Instance.WasStarted)
            questData.Instance.StartQuest();

        questTracker.TrackQuest(questData.Instance);
    }

    public void StopCurrentTrackedQuest()
    {
        questTracker.StopTracking();
    }
}