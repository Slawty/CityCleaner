using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UpgradeSaveData
{
    public List<string> nodeIds = new();
    public List<int> ranks = new();

    public Dictionary<string, int> ToDictionary()
    {
        Dictionary<string, int> result = new();
        int count = Mathf.Min(nodeIds.Count, ranks.Count);

        for (int index = 0; index < count; index++)
        {
            string nodeId = nodeIds[index];
            if (string.IsNullOrEmpty(nodeId))
                continue;

            result[nodeId] = ranks[index];
        }

        return result;
    }

    public static UpgradeSaveData FromDictionary(Dictionary<string, int> ranksById)
    {
        UpgradeSaveData saveData = new();

        foreach (KeyValuePair<string, int> entry in ranksById)
        {
            saveData.nodeIds.Add(entry.Key);
            saveData.ranks.Add(entry.Value);
        }

        return saveData;
    }
}
