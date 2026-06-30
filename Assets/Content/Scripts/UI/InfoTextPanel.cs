using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class InfoTextPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private float basePanelHeight = 125f;
    [SerializeField] private float extraHeightPerLine = 60f;
    [SerializeField] private float lettersPerLine = 24f;
    [SerializeField] private float popDurationSeconds = 0.22f;
    [SerializeField] private float popStartScale = 0.5f;
    [SerializeField] private float popOvershootScale = 1.08f;

    private CancellationTokenSource hideTextCts;
    private RectTransform panelRect;
    private RectTransform popTargetRect;
    private Vector3 baseScale = Vector3.one;
    private Tween popTween;
    private bool initialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        panelRect = transform as RectTransform;
        popTargetRect = panelRect;

        if (popTargetRect != null)
            baseScale = popTargetRect.localScale;
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

        EnsureInitialized();
        CancelHideTask();
        KillPopTween();

        infoText.text = text;
        if (panelRoot != null)
            UpdatePanelHeight();
        SetPanelVisible(true);
        PlayPopAnimation();

        if (durationSeconds <= 0f)
            return;

        hideTextCts = new CancellationTokenSource();
        HideAfterDelayAsync(durationSeconds, hideTextCts.Token).Forget();
    }

    public void HideText()
    {
        EnsureInitialized();
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
        if (popTargetRect != null)
            popTargetRect.localScale = baseScale;

        if (infoText != null)
            infoText.text = string.Empty;

        ResetPanelHeight();
        SetPanelVisible(false);
    }

    private void UpdatePanelHeight()
    {
        if (panelRect == null || infoText == null)
            return;

        int lineCount = GetEstimatedLineCount(infoText.text);
        Vector2 size = panelRect.sizeDelta;
        size.y = basePanelHeight + (lineCount - 1) * extraHeightPerLine;
        panelRect.sizeDelta = size;
    }

    private int GetEstimatedLineCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 1;

        int letterCount = 0;
        foreach (char character in text)
        {
            if (character == '\n' || character == '\r')
                continue;

            letterCount++;
        }

        return Mathf.Max(1, Mathf.CeilToInt(letterCount / lettersPerLine));
    }

    private void ResetPanelHeight()
    {
        if (panelRect == null)
            return;

        Vector2 size = panelRect.sizeDelta;
        size.y = basePanelHeight;
        panelRect.sizeDelta = size;
    }

    private void SetPanelVisible(bool visible)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(visible);
            return;
        }

        if (infoText != null)
            infoText.gameObject.SetActive(visible);
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
        if (popTargetRect == null)
            return;

        popTargetRect.localScale = baseScale * popStartScale;
        popTween = DOTween.Sequence()
            .Append(popTargetRect.DOScale(baseScale * popOvershootScale, popDurationSeconds * 0.65f).SetEase(Ease.OutCubic))
            .Append(popTargetRect.DOScale(baseScale, popDurationSeconds * 0.35f).SetEase(Ease.OutBack))
            .SetLink(popTargetRect.gameObject);
    }

    private void KillPopTween()
    {
        if (popTween == null || !popTween.IsActive())
            return;

        popTween.Kill();
        popTween = null;
    }
}
