using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PaintableMaterialSwap
{
    public Material dirtMaterial;
    public Material cleanMaterial;
}

public class MaterialManager : MonoBehaviour
{
    [SerializeField] List<PaintableMaterialSwap> materialSwaps = new();

    readonly Dictionary<Material, Material> cleanByDirt = new();
    readonly Dictionary<string, Material> cleanByDirtName = new();

    void Awake()
    {
        BuildLookup();
    }

    public bool TryGetCleanReplacement(Material dirtMaterial, out Material cleanMaterial)
    {
        cleanMaterial = null;
        if (dirtMaterial == null)
            return false;

        if (cleanByDirt.TryGetValue(dirtMaterial, out cleanMaterial))
            return true;

        string dirtName = StripInstanceSuffix(dirtMaterial.name);
        return cleanByDirtName.TryGetValue(dirtName, out cleanMaterial);
    }

    void BuildLookup()
    {
        cleanByDirt.Clear();
        cleanByDirtName.Clear();

        foreach (PaintableMaterialSwap swap in materialSwaps)
        {
            if (swap.dirtMaterial == null)
            {
                Debug.LogError($"{nameof(MaterialManager)} has a swap entry with a null dirt material.");
                continue;
            }

            if (swap.cleanMaterial == null)
            {
                Debug.LogError($"{nameof(MaterialManager)} is missing a clean material for '{swap.dirtMaterial.name}'.");
                continue;
            }

            if (cleanByDirt.ContainsKey(swap.dirtMaterial))
            {
                Debug.LogError($"{nameof(MaterialManager)} has duplicate dirt material '{swap.dirtMaterial.name}'.");
                continue;
            }

            cleanByDirt[swap.dirtMaterial] = swap.cleanMaterial;
            cleanByDirtName[swap.dirtMaterial.name] = swap.cleanMaterial;
        }
    }

    static string StripInstanceSuffix(string materialName)
    {
        const string instanceSuffix = " (Instance)";
        if (materialName.EndsWith(instanceSuffix))
            return materialName[..^instanceSuffix.Length];

        return materialName;
    }
}
