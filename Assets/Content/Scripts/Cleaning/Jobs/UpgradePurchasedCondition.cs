using UnityEngine;

public class UpgradePurchasedCondition : JobCompletionCondition
{
    [SerializeField] UpgradeNodeData upgradeData;
    [SerializeField] int requiredRank = 1;

    bool listening;

    public override bool IsMet =>
        upgradeData != null && Managers.Upgrades.GetRankById(upgradeData.id) >= requiredRank;

    public override void StartListening()
    {
        if (listening)
            return;

        listening = true;
        Managers.Upgrades.OnProgressRefreshed += HandleProgressRefreshed;
    }

    public override void StopListening()
    {
        if (!listening)
            return;

        listening = false;

        if (Managers.Upgrades != null)
            Managers.Upgrades.OnProgressRefreshed -= HandleProgressRefreshed;
    }

    void HandleProgressRefreshed()
    {
        if (IsMet)
            NotifyChanged();
    }
}
