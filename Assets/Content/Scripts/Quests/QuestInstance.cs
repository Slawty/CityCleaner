using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using System.Linq;

public class QuestInstance : MonoBehaviour
{
    [InlineScriptableObject]
    [SerializeField] QuestData data;
    public QuestData Data => data;
    public event UnityAction OnQuestCompleted;
    public List<QuestTask> TaskInstances { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool WasStarted { get; private set; }

    void Start()
    {
        Debug.Log($"Set Quest Instance of {data.title}");
        data.SetQuestInstance(this);
    }

    public void StartQuest()
    {
        WasStarted = true;
        TaskInstances = GetComponents<QuestTask>().ToList();

        foreach (var task in TaskInstances)
        {
            task.OnTaskCompleted += HandleTaskCompleted;
            task.StartTask();
        }
    }

    private void HandleTaskCompleted()
    {
        foreach (var task in TaskInstances)
        {
            if (!task.IsCompleted)
                return;
        }

        IsCompleted = true;
        OnQuestCompleted?.Invoke();
        Debug.Log($"All Tasks completed");
    }

    void OnDestroy()
    {
        data.ResetValues();
    }
}