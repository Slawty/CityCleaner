using TMPro;
using UnityEngine;

public class JobReminderItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI descriptionText;

    void Awake()
    {
        if (descriptionText == null)
            descriptionText = transform.Find("Description/Description Text")?.GetComponent<TextMeshProUGUI>();
    }

    public void SetDescription(string description)
    {
        if (descriptionText == null)
            return;

        descriptionText.text = description;
    }
}
