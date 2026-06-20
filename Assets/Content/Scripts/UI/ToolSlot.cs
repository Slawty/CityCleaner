using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ToolSlot : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] RectTransform icon;
    [SerializeField] Color highlightColor;
    [SerializeField] int toolIndex;
    [SerializeField] float selectedIconScale = 1.15f;
    [SerializeField] float tweenDuration = 0.15f;

    Color normalColor;
    float normalIconScale = 1f;
    bool isSelected;

    public int ToolIndex => toolIndex;

    void Awake()
    {
        normalColor = background.color;
        normalIconScale = icon.localScale.x;
    }

    void OnDestroy()
    {
        background.DOKill();
        icon.DOKill();
    }

    public void SetSelected(bool selected)
    {
        if (isSelected == selected)
            return;

        isSelected = selected;

        background.DOKill();
        icon.DOKill();

        Color targetColor = selected ? highlightColor : normalColor;
        float targetScale = selected ? selectedIconScale : normalIconScale;

        background.DOColor(targetColor, tweenDuration);
        icon.DOScale(Vector3.one * targetScale, tweenDuration).SetEase(Ease.OutBack);
    }
}
