using TMPro;
using UnityEngine;

public class PlaytimeTimerText : MonoBehaviour
{
    [SerializeField] TMP_Text timerText;
    [SerializeField] bool ignoreTimeScale = true;

    float startTime;

    void Awake()
    {
        startTime = ignoreTimeScale ? Time.unscaledTime : Time.time;
        UpdateText(0f);
    }

    void Update()
    {
        float now = ignoreTimeScale ? Time.unscaledTime : Time.time;
        UpdateText(now - startTime);
    }

    void UpdateText(float seconds)
    {
        if (timerText == null)
            return;

        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int secs = totalSeconds % 60;

        timerText.text = $"{hours:00}:{minutes:00}:{secs:00}";
    }
}

