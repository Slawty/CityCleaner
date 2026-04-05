using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] Image fillImage;
    [SerializeField] TextMeshProUGUI percentText;

    [Header("Animation")]
    [SerializeField] bool smooth = true;
    [SerializeField] float smoothSpeed = 8f;

    float targetPercent = 0f;
    float displayedPercent = 0f;

    void Update()
    {
        if (smooth && displayedPercent != targetPercent)
        {
            displayedPercent = Mathf.Lerp(
                displayedPercent,
                targetPercent,
                Time.deltaTime * smoothSpeed);
        }
        else
        {
            displayedPercent = targetPercent;
        }

        UpdateUI(displayedPercent);
    }

    // --------------------------------------------------
    // Call this from anywhere
    // --------------------------------------------------

    public void SetPercent(float percent)
    {
        targetPercent = Mathf.Clamp(percent, 0f, 100f);
    }

    // --------------------------------------------------

    void UpdateUI(float percent)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = percent / 100f;
        }

        if (percentText != null)
        {
            percentText.text =
                Mathf.RoundToInt(percent) + "%";
        }
    }
}
