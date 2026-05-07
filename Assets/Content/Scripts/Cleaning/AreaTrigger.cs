using UnityEngine;
using UnityEngine.Events;

public class AreaTrigger : MonoBehaviour
{
    [SerializeField] DirtArea dirtArea;
     public UnityAction OnPlayerEnter;
    public UnityAction OnPlayerExit;

    void Reset()
    {
        dirtArea = GetComponentInParent<DirtArea>();
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Entering area: {dirtArea.name}");
        // Managers.Areas.EnterArea(dirtArea);
        OnPlayerEnter?.Invoke();
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"Exited area: {dirtArea.name}");
        // Managers.Areas.ExitArea(dirtArea);
        OnPlayerExit?.Invoke();
    }
}
