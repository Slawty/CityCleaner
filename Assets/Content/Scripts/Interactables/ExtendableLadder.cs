using DG.Tweening;
using UnityEngine;

public class ExtendableLadder : MonoBehaviour, IInteractable
{
    [SerializeField] Transform ladder;
    [SerializeField] GameObject ladderTrigger;
    [SerializeField] float extendDuration = 1f;
    [SerializeField] float retractedScaleY = 0.001f;
    [SerializeField] bool enablePlayerInteraction;
    [SerializeField] bool startExtended;

    Vector3 extendedScale;
    bool isExtended;
    bool isAnimating;
    string prompt;

    public string Prompt => enablePlayerInteraction ? prompt : string.Empty;

    void Awake()
    {
        if (ladder == null)
            throw new MissingReferenceException($"{nameof(ExtendableLadder)} on {name} is missing a ladder transform.");

        if (ladderTrigger == null)
            throw new MissingReferenceException($"{nameof(ExtendableLadder)} on {name} is missing a ladder trigger.");

        extendedScale = ladder.localScale;

        if (startExtended)
            ApplyExtendedState();
        else
            ApplyRetractedState();

        UpdatePlayerInteractionCollider();
    }

    void OnDestroy()
    {
        ladder.DOKill();
    }

    public void Interact(GameObject interactor)
    {
        if (!enablePlayerInteraction || isAnimating)
            return;

        if (isExtended)
            Retract();
        else
            Extend();
    }

    public void InteractReleased(GameObject interactor)
    {
    }

    public void Extend()
    {
        if (isExtended || isAnimating)
            return;

        isAnimating = true;
        ladderTrigger.SetActive(false);
        UpdatePrompt();

        ladder.DOScale(extendedScale, extendDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(OnExtendComplete);
    }

    public void Retract()
    {
        if (!isExtended || isAnimating)
            return;

        isAnimating = true;
        ladderTrigger.SetActive(false);
        UpdatePrompt();

        Vector3 retractedScale = extendedScale;
        retractedScale.y = retractedScaleY;

        ladder.DOScale(retractedScale, extendDuration)
            .SetEase(Ease.InCubic)
            .OnComplete(OnRetractComplete);
    }

    void OnExtendComplete()
    {
        isExtended = true;
        isAnimating = false;
        ladderTrigger.SetActive(true);
        UpdatePrompt();
    }

    void OnRetractComplete()
    {
        isExtended = false;
        isAnimating = false;
        UpdatePrompt();
    }

    void ApplyExtendedState()
    {
        ladder.localScale = extendedScale;
        ladderTrigger.SetActive(true);
        isExtended = true;
        isAnimating = false;
        UpdatePrompt();
    }

    void ApplyRetractedState()
    {
        Vector3 retractedScale = extendedScale;
        retractedScale.y = retractedScaleY;
        ladder.localScale = retractedScale;
        ladderTrigger.SetActive(false);
        isExtended = false;
        isAnimating = false;
        UpdatePrompt();
    }

    void UpdatePrompt()
    {
        if (isAnimating)
            prompt = string.Empty;
        else
            prompt = isExtended ? "Retract Ladder" : "Extend Ladder";
    }

    void UpdatePlayerInteractionCollider()
    {
        Collider collider = GetComponent<Collider>();
        if (collider == null || collider.isTrigger)
            return;

        collider.enabled = enablePlayerInteraction;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        UpdatePlayerInteractionCollider();
    }
#endif
}
