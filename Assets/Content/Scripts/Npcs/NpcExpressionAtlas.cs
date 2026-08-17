using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NpcExpressionAtlas", menuName = "City Cleaner/NPC Expression Atlas")]
public class NpcExpressionAtlas : ScriptableObject
{
    [Serializable]
    public class ExpressionEntry
    {
        public string name;
        public Texture2D texture;
    }

    [SerializeField] string defaultExpressionName = "Neutral";
    [SerializeField] ExpressionEntry[] expressions = Array.Empty<ExpressionEntry>();

    public string DefaultExpressionName => defaultExpressionName;

    public bool TryGetTexture(string expressionName, out Texture2D texture)
    {
        texture = null;

        if (string.IsNullOrEmpty(expressionName))
            return false;

        foreach (ExpressionEntry expression in expressions)
        {
            if (expression == null || expression.name != expressionName)
                continue;

            if (expression.texture == null)
            {
                Debug.LogError($"NpcExpressionAtlas '{name}': expression '{expressionName}' has no texture assigned.", this);
                return false;
            }

            texture = expression.texture;
            return true;
        }

        Debug.LogError($"NpcExpressionAtlas '{name}': no expression named '{expressionName}'.", this);
        return false;
    }
}
