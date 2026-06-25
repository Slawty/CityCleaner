using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] Image fillImage;
    [SerializeField] TextMeshProUGUI percentText;
    [SerializeField] TextMeshProUGUI descriptionText;

    [Header("Animation")]
    [SerializeField] bool smooth = true;
    [SerializeField] float smoothSpeed = 8f;

    float targetPercent = 0f;
    float displayedPercent = 0f;

    const float ReachEpsilon = 0.01f;

    void Start()
    {
        UpdateUI(displayedPercent);
    }

    void Update()
    {
        float deltaToTarget = Mathf.Abs(displayedPercent - targetPercent);
        if (deltaToTarget <= ReachEpsilon)
        {
            if (!Mathf.Approximately(displayedPercent, targetPercent))
            {
                displayedPercent = targetPercent;
                UpdateUI(displayedPercent);
            }

            return;
        }

        if (smooth)
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

    public void SetPercent(float percent, bool onlyIncrease = false)
    {
        percent = Mathf.Clamp(percent, 0f, 100f);
        if (onlyIncrease && percent < targetPercent)
            return;

        targetPercent = percent;
    }

    public void ResetProgress()
    {
        targetPercent = 0f;
        displayedPercent = 0f;
        UpdateUI(0f);
    }

    public void SetFillImage(Image image)
    {
        fillImage = image;
    }

    public void SetDescription(string description)
    {
        if (descriptionText == null)
            return;

        descriptionText.text = description;
        descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(description));
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
