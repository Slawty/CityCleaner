using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(DirtMaterialPreview))]
public class DirtMaterialPreviewEditor : Editor
{
    const string DirtAmountProperty = "_DirtAmount";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8f);

        foreach (Object selectedObject in targets)
        {
            DirtMaterialPreview preview = selectedObject as DirtMaterialPreview;
            if (preview == null)
                continue;

            string buttonLabel = preview.PreviewDirty ? "Set Clean (0)" : "Set Dirty (1)";
            if (GUILayout.Button($"{buttonLabel}  (U)"))
                TogglePreview(preview);
        }
    }

    [Shortcut("City Cleaner/Toggle Dirt Material Preview", KeyCode.U)]
    static void ToggleShortcut()
    {
         DirtMaterialPreview[] previews = Object.FindObjectsByType<DirtMaterialPreview>(
        FindObjectsInactive.Include,
        FindObjectsSortMode.None);
    foreach (DirtMaterialPreview preview in previews)
        TogglePreview(preview);
    }

    static void TogglePreview(DirtMaterialPreview preview)
    {
        Undo.RecordObject(preview, "Toggle Dirt Material Preview");
        preview.PreviewDirty = !preview.PreviewDirty;
        ApplyPreview(preview);
        EditorUtility.SetDirty(preview);
    }

    static void ApplyPreview(DirtMaterialPreview preview)
    {
        float dirtAmount = preview.PreviewDirty ? 1f : 0f;

        foreach (Material material in preview.Materials)
        {
            if (material == null || !material.HasProperty(DirtAmountProperty))
                continue;

            Undo.RecordObject(material, "Toggle Dirt Material Preview");
            material.SetFloat(DirtAmountProperty, dirtAmount);
            EditorUtility.SetDirty(material);
        }

        SceneView.RepaintAll();
    }
}
