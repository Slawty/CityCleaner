using UnityEngine;
using UnityEditor;
using System;

public class GrassField : MonoBehaviour
{
    [SerializeField] private CuttableGrass[] grassPatches;

    public int GetProgress()
    {
        int cutCount = 0;

        for (int i = 0; i < grassPatches.Length; i++)
        {
            if (grassPatches[i].IsCut)
                cutCount++;
        }

        int progress = Mathf.RoundToInt((float)cutCount / grassPatches.Length) * 100;

        return progress;
    }

    [ContextMenu("Collect Grass Patches")]
    private void CollectGrassPatches()
    {
        // Get all GrassPatch components in children (including inactive)
        grassPatches = GetComponentsInChildren<CuttableGrass>(true);

        // Mark dirty so Unity saves the change permanently
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif

        Debug.Log($"Collected {grassPatches.Length} GrassPatch objects.", this);
    }
}