using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;

public class JobTargetHighlighter : MonoBehaviour
{
    [SerializeField] InputActionReference highlightAction;
    [SerializeField] float highlightDuration = 4f;
    [SerializeField] float fadeInDuration = 0.35f;
    [SerializeField] float fadeOutDuration = 0.65f;
    [SerializeField] EventReference highlightSoundEvent;

    readonly List<GPUPaintableObject> highlightedTargets = new();
    readonly List<GPUPaintableObject> scratchTargets = new();
    Coroutine highlightRoutine;

    void OnEnable()
    {
        if (highlightAction == null)
            return;

        highlightAction.action.Enable();
        highlightAction.action.performed += OnHighlightPressed;
    }

    void OnDisable()
    {
        if (highlightAction == null)
            return;

        highlightAction.action.performed -= OnHighlightPressed;
        highlightAction.action.Disable();
        StopHighlight();
    }

    void OnHighlightPressed(InputAction.CallbackContext context)
    {
        if (Managers.Input.InteractionBlocked())
            return;

        if (!Managers.Jobs.HasActiveJob)
            return;

        StartHighlight();
    }

    public void StopHighlight()
    {
        if (highlightRoutine != null)
        {
            StopCoroutine(highlightRoutine);
            highlightRoutine = null;
        }

        ClearHighlightedTargets();
    }

    public void HighlightActiveJobTargets()
    {
        StartHighlight();
    }

    void StartHighlight()
    {
        scratchTargets.Clear();
        foreach (Job activeJob in Managers.Jobs.ActiveJobs)
            activeJob.CollectIncompletePaintables(scratchTargets);

        if (scratchTargets.Count == 0)
            return;

        StopHighlight();

        foreach (GPUPaintableObject target in scratchTargets)
        {
            if (target == null || target.isClean || !target.gameObject.activeInHierarchy)
                continue;

            highlightedTargets.Add(target);
            target.OnCleaned += OnTargetCleaned;
        }

        if (highlightedTargets.Count == 0)
            return;

        PlayHighlightSound();
        float holdDuration = Mathf.Max(highlightDuration - fadeInDuration - fadeOutDuration, 0f);
        ApplyStrengthToAll(EvaluateHighlightStrength(Time.deltaTime, holdDuration));
        highlightRoutine = StartCoroutine(HighlightRoutine());
    }

    void PlayHighlightSound()
    {
        if (highlightSoundEvent.IsNull)
            throw new System.InvalidOperationException("Highlight FMOD event is not assigned on JobTargetHighlighter.");

        RuntimeManager.PlayOneShotAttached(highlightSoundEvent, gameObject);
    }

    IEnumerator HighlightRoutine()
    {
        float holdDuration = Mathf.Max(highlightDuration - fadeInDuration - fadeOutDuration, 0f);
        float elapsed = 0f;
        float totalDuration = fadeInDuration + holdDuration + fadeOutDuration;

        while (elapsed < totalDuration && highlightedTargets.Count > 0)
        {
            elapsed += Time.deltaTime;
            float strength = EvaluateHighlightStrength(elapsed, holdDuration);
            ApplyStrengthToAll(strength);
            yield return null;
        }

        StopHighlight();
    }

    float EvaluateHighlightStrength(float elapsed, float holdDuration)
    {
        if (elapsed < fadeInDuration)
            return elapsed / fadeInDuration;

        if (elapsed < fadeInDuration + holdDuration)
            return 1f;

        float fadeElapsed = elapsed - fadeInDuration - holdDuration;
        return 1f - fadeElapsed / fadeOutDuration;
    }

    void ApplyStrengthToAll(float strength)
    {
        for (int index = highlightedTargets.Count - 1; index >= 0; index--)
        {
            GPUPaintableObject target = highlightedTargets[index];
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                highlightedTargets.RemoveAt(index);
                continue;
            }

            ApplyStrength(target, strength);
        }
    }

    void ApplyStrength(GPUPaintableObject target, float strength)
    {
        target.SetJobHighlight(strength);
    }

    void OnTargetCleaned()
    {
        for (int index = highlightedTargets.Count - 1; index >= 0; index--)
        {
            GPUPaintableObject target = highlightedTargets[index];
            if (target == null)
            {
                highlightedTargets.RemoveAt(index);
                continue;
            }

            if (!target.isClean)
                continue;

            target.OnCleaned -= OnTargetCleaned;
            ApplyStrength(target, 0f);
            highlightedTargets.RemoveAt(index);
        }

        if (highlightedTargets.Count == 0)
            StopHighlight();
    }

    void ClearHighlightedTargets()
    {
        foreach (GPUPaintableObject target in highlightedTargets)
        {
            if (target == null)
                continue;

            target.OnCleaned -= OnTargetCleaned;
            ApplyStrength(target, 0f);
        }

        highlightedTargets.Clear();
    }
}
