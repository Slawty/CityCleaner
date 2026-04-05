using UnityEngine;

/// <summary>
/// Mark a class or field with this to make it drawn inline in the Inspector
/// </summary>
public class InlineEditableAttribute : PropertyAttribute
{
    // You can add options here in the future if you want (foldout, bold, etc.)
}

public class InlineScriptableObjectAttribute : PropertyAttribute
{
    public bool showFoldout;

    public InlineScriptableObjectAttribute(bool showFoldout = true)
    {
        this.showFoldout = showFoldout;
    }
}