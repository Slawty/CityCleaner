using UnityEngine;

public class AreaTrigger : MonoBehaviour
{
    [SerializeField] DirtArea dirtArea;

    void Reset()
    {
        dirtArea = GetComponentInParent<DirtArea>();
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Entering area: {dirtArea.name}");
        Managers.Areas.EnterArea(dirtArea);
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"Exited area: {dirtArea.name}");

        Managers.Areas.ExitArea(dirtArea);
    }
}
