using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(DirtArea))]
public class DirtAreaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8f);

        if (GUILayout.Button("Debug Clean Fixed Percent"))
        {
            foreach (Object selectedObject in targets)
            {
                DirtArea dirtArea = selectedObject as DirtArea;
                if (dirtArea == null)
                    continue;

                dirtArea.DebugCleanFixedPercent();
                EditorUtility.SetDirty(dirtArea);
            }
        }
    }
}
