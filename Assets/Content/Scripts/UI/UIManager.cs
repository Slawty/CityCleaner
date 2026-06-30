using UnityEngine;
using UnityEngine.Serialization;
using TMPro;

public class UIManager : MonoBehaviour
{
    private const float DefaultInfoTextDurationSeconds = 2f;

    [SerializeField] ProgressBar cleanProgressBar;
    [FormerlySerializedAs("jobProgressBar")]
    [SerializeField] JobsProgressUI jobsProgressUI;
    [SerializeField] ProgressBar radioactivesProgressBar;
    [SerializeField] InfoTextPanel popupInfoText;
    [SerializeField] InfoTextPanel tutorialInfoPanel;
    [SerializeField] GameObject interactPromptPanel;
    [FormerlySerializedAs("interactText")]
    [SerializeField] TMP_Text interactPromptText;
    [SerializeField] private TMP_Text coinValueText;
    [SerializeField] private TMP_Text poopValueText;
    [SerializeField] GameObject hudRoot;

    void Start()
    {
        ShowRadioactivesProgress(false);
    }

    public void ShowInteractText(string text)
    {
        interactPromptText.text = text;
        interactPromptPanel.SetActive(true);
    }

    public void ShowInfoText(string text, float durationSeconds)
    {
        popupInfoText?.ShowText(text, durationSeconds);
    }

    public void ShowInfoText(string text)
    {
        ShowInfoText(text, DefaultInfoTextDurationSeconds);
    }

    public void ShowTutorialInfoText(string text, float durationSeconds)
    {
        tutorialInfoPanel?.ShowText(text, durationSeconds);
    }

    public void ShowTutorialInfoText(string text)
    {
        ShowTutorialInfoText(text, 0f);
    }

    public void HideTutorialInfoText()
    {
        tutorialInfoPanel?.HideText();
    }

    public void HideInfoText()
    {
        popupInfoText?.HideText();
    }

    public void ShowRadioactivesProgress(bool visible)
    {
        radioactivesProgressBar.gameObject.SetActive(visible);
    }

    public void HideInteractText()
    {
        interactPromptPanel.SetActive(false);
    }

    public void SetHudVisible(bool visible)
    {
        hudRoot.SetActive(visible);
    }

    public void SetCleanProgressBarPercent(float value)
    {
        cleanProgressBar.SetPercent(value);
    }

    public void RegisterJobProgress(Job job)
    {
        jobsProgressUI.RegisterJob(job);
    }

    public void UnregisterJobProgress(Job job)
    {
        jobsProgressUI.UnregisterJob(job);
    }

    public void SetJobProgress(Job job, float percent, string description = null)
    {
        jobsProgressUI.SetJobProgress(job, percent, description);
    }

    public void ResetJobProgress(Job job)
    {
        jobsProgressUI.ResetJobProgress(job);
    }

    public void SetRadioactivesProgressBarPercent(float value)
    {
        radioactivesProgressBar.SetPercent(value);
    }

    public void SetCoinValue(int value)
    {
        coinValueText.text = value.ToString();
    }

    public void SetPoopValue(int value)
    {
        poopValueText.text = value.ToString();
    }
}
