using DG.Tweening;
using FMODUnity;
using UnityEngine;
using UnityEngine.Serialization;

public class ToolPickup : MonoBehaviour, IInteractable
{
    [SerializeField] Transform spinTarget;
    [SerializeField] float spinSpeed = 90f;
    [FormerlySerializedAs("toolIndex")]
    [SerializeField] PlayerToolType toolType = PlayerToolType.GooGun;
    [Header("Audio")]
    [SerializeField] EventReference throwStartEvent;
    [SerializeField] EventReference throwLandEvent;
    [SerializeField] EventReference pickupEvent;

    bool collected;
    bool isInFlight;

    public string Prompt => collected || isInFlight ? string.Empty : GetPickupPrompt(toolType);

    void Awake()
    {
        if (spinTarget == null)
            spinTarget = transform;
    }

    void OnDestroy()
    {
        transform.DOKill();
    }

    public void ThrowTo(Vector3 target, float arcHeight, float duration)
    {
        isInFlight = true;
        transform.DOKill();
        PlaySound(throwStartEvent);
        transform
            .DOJump(target, arcHeight, 1, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                isInFlight = false;
                PlaySound(throwLandEvent);
            });
    }

    void Update()
    {
        if (collected)
            return;

        spinTarget.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    public void Interact(GameObject interactor)
    {
        if (collected || isInFlight)
            return;

        collected = true;
        PlaySound(pickupEvent);
        Managers.Tools.UnlockTool(toolType);
        Destroy(gameObject);
    }

    public void InteractReleased(GameObject interactor)
    {
    }

    void PlaySound(EventReference eventReference)
    {
        if (eventReference.IsNull)
            throw new System.InvalidOperationException($"{nameof(ToolPickup)} on {name}: required FMOD event is not assigned.");

        RuntimeManager.PlayOneShotAttached(eventReference, gameObject);
    }

    static string GetPickupPrompt(PlayerToolType type)
    {
        return type switch
        {
            PlayerToolType.Laser => "Pick up Laser",
            PlayerToolType.PowerWasher => "Pick up Power Washer",
            PlayerToolType.GooGun => "Pick up Goo Gun",
            _ => "Pick up"
        };
    }
}
