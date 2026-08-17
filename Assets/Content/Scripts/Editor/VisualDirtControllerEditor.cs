using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VisualDirtController))]
public class VisualDirtControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VisualDirtController visualDirtController = (VisualDirtController)target;

        GUILayout.Space(8f);

        if (GUILayout.Button("Collect Zone Dirt Maps From Children"))
        {
            Undo.RecordObject(visualDirtController, "Collect Zone Dirt Maps");
            visualDirtController.CollectZoneDirtMapsFromChildren();
            EditorUtility.SetDirty(visualDirtController);
        }

        if (GUILayout.Button("Quick Refresh All Zone Dirt"))
            VisualDirtController.RefreshAll(rebuildTextures: false);

        if (GUILayout.Button("Full Rebuild All Zone Dirt"))
            VisualDirtController.RefreshAll(rebuildTextures: true);
    }
}
