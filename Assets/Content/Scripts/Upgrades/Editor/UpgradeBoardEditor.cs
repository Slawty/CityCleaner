using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UpgradeBoard))]
public class UpgradeBoardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UpgradeBoard board = (UpgradeBoard)target;

        if (GUILayout.Button("Rebuild Connections"))
            board.EditorRebuildConnections();
    }
}
