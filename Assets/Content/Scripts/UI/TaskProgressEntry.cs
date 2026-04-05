using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskProgressEntry : MonoBehaviour
{
    [SerializeField] TMP_Text taskName;
    [SerializeField] TMP_Text valueText;
    [SerializeField] Image fillImage;
    [SerializeField] Image checkMark;
    private QuestTask currentTask;

    public void Setup(QuestTask task)
    {
        currentTask = task;
        taskName.text = task.Name;

        RegisterTaskBindings();
        OnProgressUpdate();
    }

    void OnTaskCompleted()
    {
        checkMark.enabled = true;
        fillImage.enabled = false;
        valueText.enabled = false;

        UnregisterTaskBindings();
        currentTask = null;
    }

    void OnProgressUpdate()
    {
        fillImage.fillAmount = currentTask.GetProgressPercentage();
        valueText.text = currentTask.GetProgressString();
    }

    void OnDestroy()
    {
        if (currentTask != null)
        {
            UnregisterTaskBindings();
        }
    }

    void RegisterTaskBindings()
    {
        currentTask.OnProgressChanged += OnProgressUpdate;
        currentTask.OnTaskCompleted += OnTaskCompleted;
    }

    void UnregisterTaskBindings()
    {
        currentTask.OnProgressChanged -= OnProgressUpdate;
        currentTask.OnTaskCompleted -= OnTaskCompleted;
    }
}