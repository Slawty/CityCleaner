using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsSliderRow : MonoBehaviour
{
    [SerializeField] TMP_Text labelText;
    [SerializeField] Slider slider;
    [SerializeField] TMP_Text valueText;
    [SerializeField] bool showAsPercent = true;

    public Slider Slider => slider;

    public void SetLabel(string text)
    {
        if (labelText != null)
            labelText.text = text;
    }

    public void Configure(float min, float max, float value, UnityAction<float> onChanged)
    {
        slider.minValue = min;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(value);
        RefreshValueText(value);
        slider.onValueChanged.RemoveListener(HandleSliderChanged);
        slider.onValueChanged.AddListener(HandleSliderChanged);
        changedHandler = onChanged;
    }

    UnityAction<float> changedHandler;

    void HandleSliderChanged(float value)
    {
        RefreshValueText(value);
        changedHandler?.Invoke(value);
    }

    void RefreshValueText(float value)
    {
        if (valueText == null)
            return;

        valueText.text = showAsPercent ? $"{Mathf.RoundToInt(value * 100f)}%" : value.ToString("0.00");
    }
}
