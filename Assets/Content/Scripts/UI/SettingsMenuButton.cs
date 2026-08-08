using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsMenuButton : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TMP_Text labelText;

    public Button Button => button;

    public void SetLabel(string text)
    {
        if (labelText != null)
            labelText.text = text;
    }

    public void SetClickHandler(UnityAction handler)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(handler);
    }
}
