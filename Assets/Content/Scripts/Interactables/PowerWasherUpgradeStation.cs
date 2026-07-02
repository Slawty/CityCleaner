using UnityEngine;

public class PowerWasherUpgradeStation : MonoBehaviour, IInteractable
{
    [SerializeField] Collider interactionCollider;

    bool isAvailable;

    public string Prompt => isAvailable ? "Upgrade Power Washer" : string.Empty;

    void Awake()
    {
        if (interactionCollider == null)
            interactionCollider = GetComponent<Collider>();

        SetAvailable(false);
    }

    void Start()
    {
        Managers.Tutorial.RegisterPowerWasherUpgradeStation(this);
    }

    public void SetAvailable(bool available)
    {
        isAvailable = available;

        if (interactionCollider != null)
            interactionCollider.enabled = available;
    }

    public void Interact(GameObject interactor)
    {
        if (!isAvailable)
            return;

        Managers.UpgradeMenu.Open();
    }

    public void InteractReleased(GameObject interactor)
    {
    }
}
