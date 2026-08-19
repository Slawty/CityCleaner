using System.Collections.Generic;
using UnityEngine;

public class DirtMaterialPreview : MonoBehaviour
{
    const string DirtAmountProperty = "_DirtAmount";

    [SerializeField] List<Material> materials = new();
    [SerializeField] bool previewDirty = true;

    public IReadOnlyList<Material> Materials => materials;
    public bool PreviewDirty
    {
        get => previewDirty;
        set => previewDirty = value;
    }

    public static void ToggleAll()
    {
        DirtMaterialPreview[] previews = Object.FindObjectsByType<DirtMaterialPreview>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (DirtMaterialPreview preview in previews)
            preview.Toggle(applyGrowables: false);

        if (previews.Length > 0)
            ApplyGrowablePreview(previews[0].PreviewDirty);
    }

    public void Toggle(bool applyGrowables)
    {
        PreviewDirty = !PreviewDirty;
        ApplyMaterials();
        if (applyGrowables)
            ApplyGrowablePreview(PreviewDirty);
    }

    public void ApplyMaterials()
    {
        float dirtAmount = PreviewDirty ? 1f : 0f;

        foreach (Material material in materials)
        {
            if (material == null || !material.HasProperty(DirtAmountProperty))
                continue;

            material.SetFloat(DirtAmountProperty, dirtAmount);
        }
    }

    public static void ApplyGrowablePreview(bool previewDirty)
    {
        GooHitGrowable[] growables = Object.FindObjectsByType<GooHitGrowable>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (GooHitGrowable growable in growables)
        {
            if (growable == null)
                continue;

            if (previewDirty)
                growable.DebugResetGrowth();
            else
                growable.DebugSetFullyGrown();
        }
    }
}
