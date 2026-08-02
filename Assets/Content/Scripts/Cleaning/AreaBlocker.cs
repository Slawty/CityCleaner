using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AreaBlocker : MonoBehaviour, IInteractable
{
    [SerializeField] DirtArea targetArea;
    [SerializeField, Range(0.01f, 1f)] float requiredCleanFraction = 0.5f;
    [SerializeField] GameObject progressScreen;
    [SerializeField] GameObject cleanedScreen;
    [SerializeField] Image progressFill;
    [SerializeField] TMP_Text requiredText;
    [SerializeField] string openPrompt = "Open Barrier";
    [SerializeField] GameObject[] deactivateOnOpen;

    bool requirementMet;
    bool isOpen;

    public string Prompt => requirementMet && !isOpen ? openPrompt : string.Empty;

    void Awake()
    {
        if (targetArea == null)
            targetArea = GetComponentInParent<DirtArea>();

        if (targetArea == null)
            throw new System.InvalidOperationException($"{nameof(AreaBlocker)} on {name}: {nameof(targetArea)} is not assigned.");
        if (progressScreen == null)
            throw new System.InvalidOperationException($"{nameof(AreaBlocker)} on {name}: {nameof(progressScreen)} is not assigned.");
        if (cleanedScreen == null)
            throw new System.InvalidOperationException($"{nameof(AreaBlocker)} on {name}: {nameof(cleanedScreen)} is not assigned.");
        if (progressFill == null)
            throw new System.InvalidOperationException($"{nameof(AreaBlocker)} on {name}: {nameof(progressFill)} is not assigned.");
        if (requiredText == null)
            throw new System.InvalidOperationException($"{nameof(AreaBlocker)} on {name}: {nameof(requiredText)} is not assigned.");

        cleanedScreen.SetActive(false);
        progressScreen.SetActive(true);
    }

    void OnEnable()
    {
        targetArea.OnAreaProgressChanged.AddListener(OnAreaProgressChanged);
        RefreshUi();
    }

    void OnDisable()
    {
        targetArea.OnAreaProgressChanged.RemoveListener(OnAreaProgressChanged);
    }

    void Start()
    {
        requiredText.text = $"required: {Mathf.RoundToInt(requiredCleanFraction * 100f)}%";
        RefreshUi();
    }

    void OnAreaProgressChanged(float progress)
    {
        RefreshUi();
    }

    void RefreshUi()
    {
        if (isOpen)
            return;

        float areaProgress = targetArea.NormalizedProgress;
        progressFill.fillAmount = Mathf.Clamp01(areaProgress / requiredCleanFraction);

        if (requirementMet || areaProgress < requiredCleanFraction)
            return;

        ShowCleanedScreen();
    }

    void ShowCleanedScreen()
    {
        requirementMet = true;
        progressScreen.SetActive(false);
        cleanedScreen.SetActive(true);
    }

    public void Interact(GameObject interactor)
    {
        if (!requirementMet || isOpen)
            return;

        OpenBarrier();
    }

    public void InteractReleased(GameObject interactor)
    {
    }

    void OpenBarrier()
    {
        isOpen = true;

        foreach (GameObject target in deactivateOnOpen)
        {
            if (target != null)
                target.SetActive(false);
        }

        Managers.UI.HideInteractText();
    }
}
