using System;
using UnityEngine;

[Serializable]
public struct UpgradeModifier
{
    public UpgradeModifierType type;
    public UpgradeModifierOperation operation;
    public float value;
}
