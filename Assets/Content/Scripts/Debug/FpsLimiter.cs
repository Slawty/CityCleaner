using UnityEngine;

public class FpsLimiter : MonoBehaviour
{
    [Header("Target FPS")]
    [Tooltip("Set the FPS limit for the game.")]
    public bool limitFPS = true;
    public int targetFPS = 60;

    void Start()
    {
        if (!limitFPS)
            return;

        // Apply the FPS limit
        Application.targetFrameRate = targetFPS;

        // Optional: VSync off to make FPS limit effective
        QualitySettings.vSyncCount = 0;

        Debug.Log("FPS limited to: " + targetFPS);
    }
}