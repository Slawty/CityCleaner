using UnityEngine;
using UnityEngine.Events;

public class Poopling : MonoBehaviour
{
    public UnityAction OnConsumed;
    public PickupInteractable PickupInteractable { get; private set; }
    public NpcNavMovement Movement { get; private set; }


    void Awake()
    {
        PickupInteractable = GetComponent<PickupInteractable>();
        Movement = GetComponent<NpcNavMovement>();
    }

    void OnDestroy()
    {
        OnConsumed?.Invoke();
    }
}
