using System;
using UnityEngine;

[Serializable]
public class UpgradeRankData
{
    public int cost;
    [TextArea(2, 5)] public string description;
    public UpgradeModifier[] modifiers;
}
