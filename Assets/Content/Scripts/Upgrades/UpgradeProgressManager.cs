using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeProgressManager : MonoBehaviour
{
    readonly Dictionary<string, int> ranksById = new();
    readonly List<UpgradeNode> registeredNodes = new();

    [SerializeField] UpgradeEffectApplicator effectApplicator;

    public event Action<UpgradeNode, int> OnRankChanged;
    public event Action OnProgressRefreshed;

    public IReadOnlyList<UpgradeNode> RegisteredNodes => registeredNodes;

    public void RegisterBoard(UpgradeBoard board)
    {
        registeredNodes.Clear();

        foreach (UpgradeNode node in board.Nodes)
        {
            if (node.Data == null || string.IsNullOrEmpty(node.Data.id))
            {
                Debug.LogError($"{nameof(UpgradeProgressManager)}: {node.name} is missing valid {nameof(UpgradeNodeData)}.", node);
                continue;
            }

            if (!ranksById.ContainsKey(node.Data.id))
                ranksById[node.Data.id] = 0;

            registeredNodes.Add(node);
        }

        RefreshEffects();
        OnProgressRefreshed?.Invoke();
    }

    public int GetRank(UpgradeNode node)
    {
        if (node?.Data == null)
            return 0;

        return GetRankById(node.Data.id);
    }

    public int GetRankById(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId))
            return 0;

        return ranksById.TryGetValue(upgradeId, out int rank) ? rank : 0;
    }

    public UpgradeNodeState GetState(UpgradeNode node)
    {
        if (node?.Data == null)
            return UpgradeNodeState.Hidden;

        if (!IsRevealed(node))
            return UpgradeNodeState.Hidden;

        int rank = GetRank(node);
        if (rank >= node.Data.maxRanks)
            return UpgradeNodeState.Maxed;

        if (!ArePrerequisitesMet(node))
            return UpgradeNodeState.Locked;

        return UpgradeNodeState.Available;
    }

    public bool IsRevealed(UpgradeNode node)
    {
        if (node == null)
            return false;

        if (node.IsRoot)
            return true;

        if (node.Prerequisites == null || node.Prerequisites.Length == 0)
            return false;

        foreach (UpgradeNode prerequisite in node.Prerequisites)
        {
            if (prerequisite != null && GetRank(prerequisite) >= 1)
                return true;
        }

        return false;
    }

    public bool ArePrerequisitesMet(UpgradeNode node)
    {
        if (node == null)
            return false;

        if (node.IsRoot)
            return true;

        if (node.Prerequisites == null || node.Prerequisites.Length == 0)
            return false;

        foreach (UpgradeNode prerequisite in node.Prerequisites)
        {
            if (prerequisite == null || GetRank(prerequisite) < 1)
                return false;
        }

        return true;
    }

    public bool CanPurchase(UpgradeNode node)
    {
        if (node?.Data == null || !node.Data.IsValid())
            return false;

        UpgradeNodeState state = GetState(node);
        if (state != UpgradeNodeState.Available)
            return false;

        UpgradeRankData nextRank = node.Data.GetRankData(GetRank(node));
        return nextRank != null && Managers.Inventory.HasEnoughCoins(nextRank.cost);
    }

    public bool TryPurchase(UpgradeNode node)
    {
        if (!CanPurchase(node))
            return false;

        int currentRank = GetRank(node);
        UpgradeRankData rankData = node.Data.GetRankData(currentRank);
        Managers.Inventory.DecreaseCoins(rankData.cost);

        int newRank = currentRank + 1;
        ranksById[node.Data.id] = newRank;

        node.InvokeRankPurchased(newRank);
        RefreshEffects();
        OnRankChanged?.Invoke(node, newRank);
        OnProgressRefreshed?.Invoke();
        return true;
    }

    public void RefreshEffects()
    {
        if (effectApplicator == null)
            return;

        effectApplicator.ApplyAll(registeredNodes, GetRank);
    }

    public void ResetProgressForDebug()
    {
        ranksById.Clear();
        foreach (UpgradeNode node in registeredNodes)
        {
            if (node.Data != null && !string.IsNullOrEmpty(node.Data.id))
                ranksById[node.Data.id] = 0;
        }

        RefreshEffects();
        OnProgressRefreshed?.Invoke();
    }
}
