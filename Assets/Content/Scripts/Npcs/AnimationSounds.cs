using System;
using FMODUnity;
using UnityEngine;

[Serializable]
public class AnimationSoundTrigger
{
    public string name;
    public EventReference eventReference;
}

public class AnimationSounds : MonoBehaviour
{
    [SerializeField] AnimationSoundTrigger[] sounds;
    [SerializeField] Transform attachPoint;

    Transform AttachTarget => attachPoint != null ? attachPoint : transform;

    public void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName))
            throw new ArgumentException("Animation event sound name is empty.", nameof(soundName));

        foreach (AnimationSoundTrigger sound in sounds)
        {
            if (sound.name != soundName)
                continue;

            if (sound.eventReference.IsNull)
                throw new InvalidOperationException($"FMOD event '{soundName}' is not assigned on AnimationSounds ({name}).");

            RuntimeManager.PlayOneShotAttached(sound.eventReference, AttachTarget.gameObject);
            return;
        }

        throw new InvalidOperationException($"No sound named '{soundName}' on AnimationSounds ({name}).");
    }
}
