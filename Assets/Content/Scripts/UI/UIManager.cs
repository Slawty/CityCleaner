using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] ProgressBar cleanProgressBar;
    [SerializeField] private TMP_Text interactText;
    [SerializeField] private TMP_Text coinValueText;
    [SerializeField] private TMP_Text poopValueText;



    public void ShowInteractText(string text)
    {
        // Debug.Log($"ShowInteractText: {text}");
        interactText.text = text;
        interactText.gameObject.SetActive(true);
    }

    public void HideInteractText()
    {
        interactText.gameObject.SetActive(false);
    }

    public void SetCleanProgressBarPercent(float value)
    {
        // Debug.Log($"SetCleanProgressBarPercent: {value}");
        cleanProgressBar.SetPercent(value);
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
