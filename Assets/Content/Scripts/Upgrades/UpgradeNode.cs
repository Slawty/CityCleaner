using UnityEngine;
using UnityEngine.Events;

public class UpgradeNode : MonoBehaviour
{
    [SerializeField] UpgradeNodeData data;
    [SerializeField] bool isRoot;
    [SerializeField] UpgradeNode[] prerequisites;
    [SerializeField] RectTransform lineAnchor;
    [SerializeField] UnityEvent<int> onRankPurchased;

    public UpgradeNodeData Data => data;
    public bool IsRoot => isRoot;
    public UpgradeNode[] Prerequisites => prerequisites;
    public RectTransform LineAnchor => lineAnchor != null ? lineAnchor : transform as RectTransform;

    public void InvokeRankPurchased(int newRank)
    {
        onRankPurchased?.Invoke(newRank);
    }
}
