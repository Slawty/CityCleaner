using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UpgradeNode))]
public class UpgradeNodeView : MonoBehaviour
{
    [SerializeField] UpgradeNode node;
    [SerializeField] Button selectButton;
    [SerializeField] Image iconImage;
    [SerializeField] Image frameImage;
    [SerializeField] TMP_Text rankLabel;
    [SerializeField] Color hiddenColor = new(1f, 1f, 1f, 0f);
    [SerializeField] Color lockedColor = new(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] Color availableColor = Color.white;
    [SerializeField] Color maxedColor = new(0.85f, 1f, 0.85f, 1f);
    [SerializeField] Color selectedColor = new(1f, 0.92f, 0.45f, 1f);

    bool isSelected;

    public UpgradeNode Node => node;

    void Awake()
    {
        if (node == null)
            node = GetComponent<UpgradeNode>();

        if (selectButton == null)
            selectButton = GetComponent<Button>();

        if (selectButton != null)
            selectButton.onClick.AddListener(HandleClicked);
    }

    void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(HandleClicked);
    }

    public void Refresh(UpgradeNodeState state, int currentRank, int maxRanks, bool selected)
    {
        isSelected = selected;
        gameObject.SetActive(state != UpgradeNodeState.Hidden);

        if (state == UpgradeNodeState.Hidden)
            return;

        if (iconImage != null && node.Data != null && node.Data.icon != null)
            iconImage.sprite = node.Data.icon;

        if (rankLabel != null)
            rankLabel.text = $"{currentRank}/{maxRanks}";

        Color frame = state switch
        {
            UpgradeNodeState.Locked => lockedColor,
            UpgradeNodeState.Available => availableColor,
            UpgradeNodeState.Maxed => maxedColor,
            _ => availableColor
        };

        if (selected)
            frame = selectedColor;

        if (frameImage != null)
            frameImage.color = frame;

        if (iconImage != null)
            iconImage.color = state == UpgradeNodeState.Locked ? lockedColor : Color.white;

        if (selectButton != null)
            selectButton.interactable = state != UpgradeNodeState.Hidden;
    }

    void HandleClicked()
    {
        if (Managers.UpgradeMenu != null)
            Managers.UpgradeMenu.SelectNode(node);
    }
}
