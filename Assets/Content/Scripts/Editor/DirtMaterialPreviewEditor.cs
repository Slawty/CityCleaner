using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(DirtMaterialPreview))]
public class DirtMaterialPreviewEditor : Editor
{
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
                ToggleWithUndo(preview, applyGrowables: true);
        }
    }

    [Shortcut("City Cleaner/Toggle Dirt Material Preview", KeyCode.U)]
    static void ToggleShortcut()
    {
        if (Application.isPlaying)
            return;

        DirtMaterialPreview[] previews = Object.FindObjectsByType<DirtMaterialPreview>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (DirtMaterialPreview preview in previews)
            ToggleWithUndo(preview, applyGrowables: false);

        if (previews.Length > 0)
            DirtMaterialPreview.ApplyGrowablePreview(previews[0].PreviewDirty);

        SceneView.RepaintAll();
    }

    static void ToggleWithUndo(DirtMaterialPreview preview, bool applyGrowables)
    {
        Undo.RecordObject(preview, "Toggle Dirt Material Preview");
        preview.Toggle(applyGrowables);

        foreach (Material material in preview.Materials)
        {
            if (material == null)
                continue;

            Undo.RecordObject(material, "Toggle Dirt Material Preview");
            EditorUtility.SetDirty(material);
        }

        EditorUtility.SetDirty(preview);
        SceneView.RepaintAll();
    }
}
