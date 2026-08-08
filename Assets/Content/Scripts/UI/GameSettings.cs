using FMODUnity;
using UnityEngine;

public static class GameSettings
{
    const string VolumeKey = "settings.master_volume";
    const string SensitivityKey = "settings.mouse_sensitivity";

    public const float DefaultVolume = 1f;
    public const float DefaultSensitivity = 0.1f;
    public const float MinSensitivity = 0.02f;
    public const float MaxSensitivity = 0.3f;

    public static float MasterVolume { get; private set; } = DefaultVolume;
    public static float MouseSensitivity { get; private set; } = DefaultSensitivity;

    public static void Load()
    {
        MasterVolume = PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
        MouseSensitivity = PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);
    }

    public static void ApplyAll()
    {
        ApplyMasterVolume();
        ApplyMouseSensitivity();
    }

    public static void SetMasterVolume(float volume)
    {
        MasterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
        ApplyMasterVolume();
    }

    public static void SetMouseSensitivity(float sensitivity)
    {
        MouseSensitivity = Mathf.Clamp(sensitivity, MinSensitivity, MaxSensitivity);
        PlayerPrefs.SetFloat(SensitivityKey, MouseSensitivity);
        ApplyMouseSensitivity();
    }

    static void ApplyMasterVolume()
    {
        FMOD.Studio.Bus bus = RuntimeManager.GetBus("bus:/");
        if (bus.isValid())
            bus.setVolume(MasterVolume);
    }

    static void ApplyMouseSensitivity()
    {
        if (Managers.Player == null || Managers.Player.mouseLook == null)
            return;

        Managers.Player.mouseLook.sensitivity = MouseSensitivity;
    }
}
