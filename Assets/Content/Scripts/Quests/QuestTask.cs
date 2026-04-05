using UnityEngine;
using UnityEngine.Events;

public abstract class QuestTask : MonoBehaviour
{
    public UnityAction OnTaskCompleted { get; set; }
    public UnityAction OnProgressChanged { get; set; }
    public string Name;
    public abstract void StartTask();
    public abstract float GetProgressPercentage();
    public abstract string GetProgressString();
    public bool IsCompleted;

}
