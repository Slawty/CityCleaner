using UnityEngine;
using UnityEngine.Serialization;
using TMPro;

public class UIManager : MonoBehaviour
{
    private const float DefaultInfoTextDurationSeconds = 2f;

    [SerializeField] ProgressBar cleanProgressBar;
    [FormerlySerializedAs("zoneCleanProgressBar")]
    [SerializeField] ProgressBar jobProgressBar;
    [SerializeField] ProgressBar radioactivesProgressBar;
    [SerializeField] InfoTextPanel infoTextPanel;
    [SerializeField] private TMP_Text interactText;
    [SerializeField] private TMP_Text coinValueText;
    [SerializeField] private TMP_Text poopValueText;
    [SerializeField] GameObject hudRoot;

    void Start()
    {
        ShowJobProgress(false);
        ShowRadioactivesProgress(false);
    }

    public void ShowInteractText(string text)
    {
        // Debug.Log($"ShowInteractText: {text}");
        interactText.text = text;
        interactText.gameObject.SetActive(true);
    }

    public void ShowInfoText(string text, float durationSeconds)
    {
        infoTextPanel?.ShowText(text, durationSeconds);
    }

    public void ShowInfoText(string text)
    {
        ShowInfoText(text, DefaultInfoTextDurationSeconds);
    }

    public void HideInfoText()
    {
        infoTextPanel?.HideText();
    }

    public void ShowJobProgress(bool visible)
    {
        jobProgressBar.gameObject.SetActive(visible);
    }

    public void ShowRadioactivesProgress(bool visible)
    {
        radioactivesProgressBar.gameObject.SetActive(visible);
    }

    public void HideInteractText()
    {
        interactText.gameObject.SetActive(false);
    }

    public void SetHudVisible(bool visible)
    {
        hudRoot.SetActive(visible);
    }

    public void SetCleanProgressBarPercent(float value)
    {
        cleanProgressBar.SetPercent(value);
    }

    public void SetJobProgress(float percent, string description = null)
    {
        jobProgressBar.SetPercent(percent, onlyIncrease: true);
        if (description != null)
            jobProgressBar.SetDescription(description);
    }

    public void ResetJobProgress()
    {
        jobProgressBar.ResetProgress();
    }

    public void SetRadioactivesProgressBarPercent(float value)
    {
        // Debug.Log($"Radioactives Progress: {value}");
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
