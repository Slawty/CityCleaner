using UnityEngine;

public abstract class JobCompletionCondition : MonoBehaviour
{
    public abstract bool IsMet { get; }

    public virtual Transform GetWaypointTransform() => null;

    public abstract void StartListening();
    public abstract void StopListening();

    public event System.Action Changed;

    protected void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
