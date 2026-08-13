using UnityEngine;

public class JobHighlightButtonPanel : MonoBehaviour
{
    const float DisabledAlpha = 0.2f;
    const float EnabledAlpha = 1f;

    [SerializeField] CanvasGroup canvasGroup;

    void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        SetAvailable(false);
    }

    public void SetAvailable(bool available)
    {
        if (canvasGroup == null)
            throw new System.InvalidOperationException($"{nameof(JobHighlightButtonPanel)} on {name}: {nameof(canvasGroup)} is not assigned.");

        canvasGroup.alpha = available ? EnabledAlpha : DisabledAlpha;
        canvasGroup.interactable = available;
        canvasGroup.blocksRaycasts = available;
    }
}
