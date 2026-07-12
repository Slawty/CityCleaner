using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CleanFlashPlayer
{
    public const float Duration = 1f;
    public const float Peak = 1f;

    static readonly int CleanFlashId = Shader.PropertyToID("_CleanFlash");

    MaterialPropertyBlock propertyBlock;
    CancellationTokenSource cancellationTokenSource;
    int generation;

    MaterialPropertyBlock PropertyBlock => propertyBlock ??= new MaterialPropertyBlock();

    public bool IsPlaying => cancellationTokenSource != null;

    public static bool SupportsCleanFlash(Renderer renderer)
    {
        if (renderer == null)
            return false;

        Material material = renderer.sharedMaterial;
        return material != null && material.HasProperty(CleanFlashId);
    }

    public void Play(IReadOnlyList<Renderer> renderers, Action onComplete = null)
    {
        Stop(invalidateRunning: true);
        cancellationTokenSource = new CancellationTokenSource();
        int flashGeneration = generation;
        bool animateFlash = HasFlashableRenderer(renderers);
        if (animateFlash)
            SetFlash(renderers, Peak);

        PlayAsync(renderers, flashGeneration, cancellationTokenSource.Token, animateFlash, onComplete).Forget();
    }

    public void Stop(bool invalidateRunning)
    {
        if (cancellationTokenSource == null)
            return;

        if (invalidateRunning)
            generation++;

        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        cancellationTokenSource = null;
    }

    public void ResetFlash(IReadOnlyList<Renderer> renderers)
    {
        SetFlash(renderers, 0f);
    }

    static bool HasFlashableRenderer(IReadOnlyList<Renderer> renderers)
    {
        if (renderers == null)
            return false;

        for (int i = 0; i < renderers.Count; i++)
        {
            if (SupportsCleanFlash(renderers[i]))
                return true;
        }

        return false;
    }

    void SetFlash(IReadOnlyList<Renderer> renderers, float amount)
    {
        if (renderers == null)
            return;

        float clampedAmount = Mathf.Clamp01(amount);

        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer renderer = renderers[i];
            if (!SupportsCleanFlash(renderer))
                continue;

            renderer.GetPropertyBlock(PropertyBlock);
            PropertyBlock.SetFloat(CleanFlashId, clampedAmount);
            renderer.SetPropertyBlock(PropertyBlock);
        }
    }

    async UniTaskVoid PlayAsync(IReadOnlyList<Renderer> renderers, int flashGeneration, CancellationToken cancellationToken, bool animateFlash, Action onComplete)
    {
        try
        {
            float elapsed = 0f;

            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                if (animateFlash)
                {
                    float normalizedTime = elapsed / Duration;
                    float fade = 1f - normalizedTime;
                    SetFlash(renderers, Peak * fade * fade);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (flashGeneration != generation)
                return;

            if (animateFlash)
                SetFlash(renderers, 0f);

            onComplete?.Invoke();
        }
        catch (OperationCanceledException)
        {
            if (flashGeneration == generation && animateFlash)
                SetFlash(renderers, 0f);
        }
        finally
        {
            if (flashGeneration == generation)
                cancellationTokenSource = null;
        }
    }
}
