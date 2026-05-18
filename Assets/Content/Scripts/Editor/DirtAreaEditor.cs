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

        GUILayout.Space(8f);

        if (GUILayout.Button("Quick Refresh Zone Dirt"))
        {
            foreach (Object selectedObject in targets)
            {
                DirtArea dirtArea = selectedObject as DirtArea;
                if (dirtArea != null)
                    RefreshZoneDirt(dirtArea, rebuildTextures: false);
            }
        }

        if (GUILayout.Button("Full Rebuild Zone Dirt"))
        {
            foreach (Object selectedObject in targets)
            {
                DirtArea dirtArea = selectedObject as DirtArea;
                if (dirtArea != null)
                    RefreshZoneDirt(dirtArea, rebuildTextures: true);
            }
        }
    }

    internal static void RefreshZoneDirt(DirtArea dirtArea, bool rebuildTextures)
    {
        ZoneDirtMap[] zoneDirtMaps = dirtArea.GetComponentsInChildren<ZoneDirtMap>(true);
        int refreshedZoneCount = 0;
        foreach (ZoneDirtMap zoneDirtMap in zoneDirtMaps)
        {
            if (zoneDirtMap == null)
                continue;

            zoneDirtMap.UpdateZoneBounds();
            if (rebuildTextures)
                zoneDirtMap.RebuildZoneTexture();

            zoneDirtMap.ApplyToTargetRenderers();
            EditorUtility.SetDirty(zoneDirtMap);
            refreshedZoneCount++;
        }

        if (rebuildTextures)
            Debug.Log($"Full Rebuild Zone Dirt: refreshed {refreshedZoneCount} ZoneDirtMap components in area {dirtArea.name}.", dirtArea);

        EditorUtility.SetDirty(dirtArea);
        SceneView.RepaintAll();
    }
}

[InitializeOnLoad]
static class DirtAreaPlayModeRefresh
{
    static DirtAreaPlayModeRefresh()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange playModeState)
    {
        if (playModeState != PlayModeStateChange.EnteredEditMode)
            return;

        DirtArea[] dirtAreas = Object.FindObjectsByType<DirtArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (DirtArea dirtArea in dirtAreas)
        {
            if (dirtArea == null)
                continue;

            DirtAreaEditor.RefreshZoneDirt(dirtArea, rebuildTextures: true);
        }
    }
}
