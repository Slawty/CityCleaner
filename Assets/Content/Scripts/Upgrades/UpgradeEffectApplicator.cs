using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeEffectApplicator : MonoBehaviour
{
    struct StatBases
    {
        public float waterMaxAmmo;
        public float waterDamagePerSecond;
        public float waterAmmoPerSecond;
        public float gpuCleanSpeed;
        public float gpuBrushSize;
    }

    StatBases bases;
    bool basesCaptured;
    readonly Dictionary<UpgradeModifierType, float> additiveTotals = new();
    readonly Dictionary<UpgradeModifierType, float> multiplicativeTotals = new();

    public void CaptureBasesIfNeeded()
    {
        if (basesCaptured)
            return;

        WaterSprayTool sprayer = Managers.Tools.WaterSprayer;
        GPUPainterWorld painter = sprayer.Painter;

        bases.waterMaxAmmo = sprayer.MaxAmmo;
        bases.waterDamagePerSecond = sprayer.DamagePerSecond;
        bases.waterAmmoPerSecond = sprayer.AmmoPerSecond;
        bases.gpuCleanSpeed = painter.CleanSpeed;
        bases.gpuBrushSize = painter.BrushWorldSize;
        basesCaptured = true;
    }

    public void ApplyAll(IReadOnlyList<UpgradeNode> nodes, Func<UpgradeNode, int> getRank)
    {
        CaptureBasesIfNeeded();
        ResetModifierTotals();

        foreach (UpgradeNode node in nodes)
        {
            if (node.Data == null || !node.Data.IsValid())
                continue;

            int rank = getRank(node);
            for (int rankIndex = 0; rankIndex < rank; rankIndex++)
            {
                UpgradeRankData rankData = node.Data.GetRankData(rankIndex);
                if (rankData?.modifiers == null)
                    continue;

                foreach (UpgradeModifier modifier in rankData.modifiers)
                    AccumulateModifier(modifier);
            }
        }

        ApplyToGameplay();
    }

    void ResetModifierTotals()
    {
        additiveTotals.Clear();
        multiplicativeTotals.Clear();

        foreach (UpgradeModifierType type in Enum.GetValues(typeof(UpgradeModifierType)))
        {
            additiveTotals[type] = 0f;
            multiplicativeTotals[type] = 1f;
        }
    }

    void AccumulateModifier(UpgradeModifier modifier)
    {
        if (modifier.operation == UpgradeModifierOperation.Add)
            additiveTotals[modifier.type] += modifier.value;
        else
            multiplicativeTotals[modifier.type] *= modifier.value;
    }

    float ResolveStat(float baseValue, UpgradeModifierType type)
    {
        return (baseValue + additiveTotals[type]) * multiplicativeTotals[type];
    }

    void ApplyToGameplay()
    {
        WaterSprayTool sprayer = Managers.Tools.WaterSprayer;
        GPUPainterWorld painter = sprayer.Painter;

        sprayer.MaxAmmo = ResolveStat(bases.waterMaxAmmo, UpgradeModifierType.WaterMaxAmmo);
        sprayer.DamagePerSecond = ResolveStat(bases.waterDamagePerSecond, UpgradeModifierType.WaterDamagePerSecond);
        sprayer.AmmoPerSecond = ResolveStat(bases.waterAmmoPerSecond, UpgradeModifierType.WaterAmmoPerSecond);
        painter.CleanSpeed = ResolveStat(bases.gpuCleanSpeed, UpgradeModifierType.GpuCleanSpeed);
        painter.BrushWorldSize = ResolveStat(bases.gpuBrushSize, UpgradeModifierType.GpuBrushSize);
    }
}
