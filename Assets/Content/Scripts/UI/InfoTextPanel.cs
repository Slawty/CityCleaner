using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class InfoTextPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private float popDurationSeconds = 0.22f;
    [SerializeField] private float popStartScale = 0.85f;
    [SerializeField] private float popOvershootScale = 1.08f;

    private CancellationTokenSource hideTextCts;
    private RectTransform infoTextRect;
    private Vector3 baseScale = Vector3.one;
    private Tween popTween;

    private void Awake()
    {
        if (infoText != null)
        {
            infoTextRect = infoText.rectTransform;
            baseScale = infoTextRect.localScale;
        }

        HideTextImmediate();
    }

    private void OnDestroy()
    {
        CancelHideTask();
        KillPopTween();
    }

    public void ShowText(string text, float durationSeconds)
    {
        if (infoText == null)
            return;

        CancelHideTask();
        KillPopTween();

        infoText.text = text;
        infoText.gameObject.SetActive(true);
        PlayPopAnimation();

        if (durationSeconds <= 0f)
            return;

        hideTextCts = new CancellationTokenSource();
        HideAfterDelayAsync(durationSeconds, hideTextCts.Token).Forget();
    }

    public void HideText()
    {
        CancelHideTask();
        KillPopTween();
        HideTextImmediate();
    }

    private async UniTaskVoid HideAfterDelayAsync(float durationSeconds, CancellationToken cancellationToken)
    {
        try
        {
            int delayMs = Mathf.CeilToInt(durationSeconds * 1000f);
            await UniTask.Delay(delayMs, cancellationToken: cancellationToken);
            HideTextImmediate();
        }
        catch (System.OperationCanceledException)
        {
            // Suppress cancellation when a new text replaces the old one.
        }
    }

    private void HideTextImmediate()
    {
        if (infoText == null)
            return;

        if (infoTextRect != null)
            infoTextRect.localScale = baseScale;

        infoText.text = string.Empty;
        infoText.gameObject.SetActive(false);
    }

    private void CancelHideTask()
    {
        if (hideTextCts == null)
            return;

        hideTextCts.Cancel();
        hideTextCts.Dispose();
        hideTextCts = null;
    }

    private void PlayPopAnimation()
    {
        if (infoTextRect == null)
            return;

        infoTextRect.localScale = baseScale * popStartScale;
        popTween = infoTextRect.DOScale(baseScale * popOvershootScale, popDurationSeconds * 0.65f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                popTween = infoTextRect.DOScale(baseScale, popDurationSeconds * 0.35f)
                    .SetEase(Ease.OutBack);
            });
    }

    private void KillPopTween()
    {
        if (popTween == null || !popTween.IsActive())
            return;

        popTween.Kill();
        popTween = null;
    }
}
