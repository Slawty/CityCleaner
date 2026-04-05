using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestTracker : MonoBehaviour
{
    [SerializeField] Transform questPanel;
    [SerializeField] Transform taskGroupRoot;
    [SerializeField] GameObject taskEntryPrefab;
    QuestInstance activeQuest;
    Dictionary<QuestTask, TaskProgressEntry> activeTaskEntries = new();

    public void TrackQuest(QuestInstance questInstance)
    {
        activeQuest = questInstance;
        questPanel.gameObject.SetActive(true);
        SetupTasks();
    }

    public void StopTracking()
    {
        questPanel.gameObject.SetActive(false);

        if (activeTaskEntries.Count > 0)
        {
            foreach (var entry in activeTaskEntries)
                Destroy(entry.Value.gameObject);
        }

        activeTaskEntries.Clear();
    }

    void SetupTasks()
    {
        foreach (var taskInstance in activeQuest.TaskInstances)
        {
            var newTaskProgress = Instantiate(taskEntryPrefab, taskGroupRoot).GetComponent<TaskProgressEntry>();
            activeTaskEntries.Add(taskInstance, newTaskProgress);
            newTaskProgress.Setup(taskInstance);
        }
    }
}
