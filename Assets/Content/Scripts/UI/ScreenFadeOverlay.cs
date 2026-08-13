using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFadeOverlay : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;

    void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        SetAlphaImmediate(0f);
    }

    public async UniTask FadeToAsync(float targetAlpha, float durationSeconds, CancellationToken cancellationToken = default)
    {
        if (canvasGroup == null)
            throw new System.InvalidOperationException($"{nameof(ScreenFadeOverlay)} on {name}: {nameof(canvasGroup)} is not assigned.");

        float startAlpha = canvasGroup.alpha;
        if (durationSeconds <= 0f)
        {
            SetAlphaImmediate(targetAlpha);
            return;
        }

        float elapsed = 0f;
        while (elapsed < durationSeconds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            elapsed += Time.unscaledDeltaTime;
            float blend = Mathf.Clamp01(elapsed / durationSeconds);
            SetAlphaImmediate(Mathf.Lerp(startAlpha, targetAlpha, blend));
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        SetAlphaImmediate(targetAlpha);
    }

    public void SetAlphaImmediate(float alpha)
    {
        canvasGroup.alpha = alpha;
        bool visible = alpha > 0.01f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }
}
