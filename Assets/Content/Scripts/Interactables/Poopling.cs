using UnityEngine;

public class Poopling : MonoBehaviour
{
    public PickupInteractable PickupInteractable { get; private set; }

    void Awake()
    {
        PickupInteractable = GetComponent<PickupInteractable>();
    }
}
