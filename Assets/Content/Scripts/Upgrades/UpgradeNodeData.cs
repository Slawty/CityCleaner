using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeNode", menuName = "City Cleaner/Upgrade Node")]
public class UpgradeNodeData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    public int maxRanks = 3;
    public UpgradeRankData[] ranks;

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(id) && ranks != null && ranks.Length == maxRanks;
    }

    public UpgradeRankData GetRankData(int rankIndex)
    {
        if (ranks == null || rankIndex < 0 || rankIndex >= ranks.Length)
            return null;

        return ranks[rankIndex];
    }
}
