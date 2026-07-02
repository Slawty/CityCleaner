using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UpgradeMenuController : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] GameObject detailPanel;
    [SerializeField] UpgradeBoard board;
    [SerializeField] TMP_Text coinValueText;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text rankText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] TMP_Text costText;
    [SerializeField] Image detailIcon;
    [SerializeField] Button purchaseButton;
    [SerializeField] Button closeButton;
    [SerializeField] Color affordableCostColor = Color.white;
    [SerializeField] Color unaffordableCostColor = Color.red;

    bool isOpen;
    UpgradeNode selectedNode;
    Coroutine enableGameplayInputRoutine;

    void Awake()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(HandlePurchaseClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (panelRoot != null)
            panelRoot.SetActive(false);

        SetDetailPanelVisible(false);
    }

    void OnDestroy()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(HandlePurchaseClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        StopEnableGameplayInputRoutine();

        if (Managers.Upgrades != null)
            Managers.Upgrades.OnProgressRefreshed -= HandleProgressRefreshed;
    }

    public void Open()
    {
        if (isOpen)
            return;

        Managers.Upgrades.OnProgressRefreshed += HandleProgressRefreshed;
        Managers.Upgrades.RegisterBoard(board);

        isOpen = true;
        selectedNode = null;
        panelRoot.SetActive(true);

        Managers.UI.HideInteractText();
        Managers.UI.SetHudVisible(false);
        Managers.Input.BlockInteraction(this);
        Managers.Player.SetMovementEnabled(false);
        Managers.Player.mouseLook.SetPointerMode(true);
        Managers.Tools.StopActiveShooting();

        RefreshCoinDisplay();
        RefreshBoard();
        RefreshDetailPanel();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        if (Managers.Upgrades != null)
            Managers.Upgrades.OnProgressRefreshed -= HandleProgressRefreshed;

        isOpen = false;
        selectedNode = null;
        panelRoot.SetActive(false);

        Managers.UI.SetHudVisible(true);
        Managers.Player.SetMovementEnabled(true);
        Managers.Player.mouseLook.SetPointerMode(false);
        enableGameplayInputRoutine = StartCoroutine(EnableGameplayInputAfterPointerReleased());
    }

    public void SelectNode(UpgradeNode node)
    {
        if (!isOpen || node == null)
            return;

        UpgradeNodeState state = Managers.Upgrades.GetState(node);
        if (state == UpgradeNodeState.Hidden)
            return;

        selectedNode = node;
        RefreshBoard();
        RefreshDetailPanel();
    }

    void HandlePurchaseClicked()
    {
        if (selectedNode == null)
            return;

        bool purchased = Managers.Upgrades.TryPurchase(selectedNode);
        if (!purchased)
            return;

        Managers.Tutorial.NotifyPowerWasherUpgraded();
        RefreshCoinDisplay();
        RefreshBoard();
        RefreshDetailPanel();
    }

    void HandleProgressRefreshed()
    {
        if (!isOpen)
            return;

        RefreshBoard();
        RefreshDetailPanel();
    }

    void RefreshBoard()
    {
        if (selectedNode != null && Managers.Upgrades.GetState(selectedNode) == UpgradeNodeState.Hidden)
            selectedNode = null;

        board.RefreshAllViews(selectedNode);
    }

    void RefreshDetailPanel()
    {
        if (selectedNode == null || selectedNode.Data == null)
        {
            SetDetailEmpty();
            return;
        }

        UpgradeNodeData data = selectedNode.Data;
        int rank = Managers.Upgrades.GetRank(selectedNode);
        UpgradeNodeState state = Managers.Upgrades.GetState(selectedNode);

        SetDetailPanelVisible(true);

        if (titleText != null)
            titleText.text = data.displayName;

        if (rankText != null)
            rankText.text = $"{rank}/{data.maxRanks}";

        if (detailIcon != null)
            detailIcon.sprite = data.icon;

        UpgradeRankData nextRank = data.GetRankData(rank);

        if (state == UpgradeNodeState.Locked)
        {
            if (descriptionText != null)
                descriptionText.text = "Requires previous upgrades.";

            if (costText != null)
            {
                costText.text = nextRank != null ? nextRank.cost.ToString() : string.Empty;
                costText.color = unaffordableCostColor;
            }

            if (purchaseButton != null)
                purchaseButton.interactable = false;
            return;
        }

        if (state == UpgradeNodeState.Maxed)
        {
            if (descriptionText != null)
                descriptionText.text = "Fully upgraded.";

            if (costText != null)
                costText.text = string.Empty;

            if (purchaseButton != null)
                purchaseButton.interactable = false;
            return;
        }

        if (descriptionText != null)
            descriptionText.text = nextRank != null ? nextRank.description : string.Empty;

        if (costText != null && nextRank != null)
        {
            costText.text = nextRank.cost.ToString();
            bool canAfford = Managers.Inventory.HasEnoughCoins(nextRank.cost);
            costText.color = canAfford ? affordableCostColor : unaffordableCostColor;
        }

        if (purchaseButton != null)
            purchaseButton.interactable = state == UpgradeNodeState.Available && Managers.Upgrades.CanPurchase(selectedNode);
    }

    void SetDetailEmpty()
    {
        SetDetailPanelVisible(false);

        if (titleText != null)
            titleText.text = string.Empty;

        if (rankText != null)
            rankText.text = string.Empty;

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (costText != null)
            costText.text = string.Empty;

        if (purchaseButton != null)
            purchaseButton.interactable = false;
    }

    void SetDetailPanelVisible(bool visible)
    {
        if (detailPanel != null)
            detailPanel.SetActive(visible);
    }

    void RefreshCoinDisplay()
    {
        if (coinValueText != null)
            coinValueText.text = Managers.Inventory.Coins.ToString();
    }

    IEnumerator EnableGameplayInputAfterPointerReleased()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            while (mouse.leftButton.isPressed)
                yield return null;
        }

        yield return null;
        enableGameplayInputRoutine = null;
        Managers.Input.UnblockInteraction(this);
    }

    void StopEnableGameplayInputRoutine()
    {
        if (enableGameplayInputRoutine == null)
            return;

        StopCoroutine(enableGameplayInputRoutine);
        enableGameplayInputRoutine = null;
    }
}
