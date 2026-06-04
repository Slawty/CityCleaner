using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    private const float DefaultInfoTextDurationSeconds = 2f;

    [SerializeField] ProgressBar cleanProgressBar;
    [SerializeField] ProgressBar zoneCleanProgressBar;
    [SerializeField] ProgressBar radioactivesProgressBar;
    [SerializeField] InfoTextPanel infoTextPanel;
    [SerializeField] private TMP_Text interactText;
    [SerializeField] private TMP_Text coinValueText;
    [SerializeField] private TMP_Text poopValueText;
    [SerializeField] GameObject hudRoot;



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

    public void ShowZoneProgress(bool b)
    {
        zoneCleanProgressBar.gameObject.SetActive(b);
        radioactivesProgressBar.gameObject.SetActive(b);
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
        // Debug.Log($"SetCleanProgressBarPercent: {value}");
        cleanProgressBar.SetPercent(value);
    }

    public void SetZoneCleanProgressBarPercent(float value)
    {
        // Debug.Log($"SetCleanProgressBarPercent: {value}");
        zoneCleanProgressBar.SetPercent(value);
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
