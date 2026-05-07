using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DirtArea))]
public class DirtAreaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8f);

        DirtArea dirtArea = (DirtArea)target;
        if (GUILayout.Button("Debug Clean Fixed Percent"))
        {
            dirtArea.DebugCleanFixedPercent();
            EditorUtility.SetDirty(dirtArea);
        }
    }
}
