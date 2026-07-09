using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(ZoneDirtMap))]
public class ZoneDirtMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8f);

        if (GUILayout.Button("Quick Refresh Zone Dirt"))
        {
            foreach (Object selectedObject in targets)
            {
                ZoneDirtMap zoneDirtMap = selectedObject as ZoneDirtMap;
                if (zoneDirtMap != null)
                    RefreshZoneDirt(zoneDirtMap, rebuildTextures: false);
            }
        }

        if (GUILayout.Button("Full Rebuild Zone Dirt"))
        {
            foreach (Object selectedObject in targets)
            {
                ZoneDirtMap zoneDirtMap = selectedObject as ZoneDirtMap;
                if (zoneDirtMap != null)
                    RefreshZoneDirt(zoneDirtMap, rebuildTextures: true);
            }
        }
    }

    internal static void RefreshZoneDirt(ZoneDirtMap zoneDirtMap, bool rebuildTextures)
    {
        if (zoneDirtMap == null)
            return;

        zoneDirtMap.UpdateZoneBounds();
        if (rebuildTextures)
            zoneDirtMap.RebuildZoneTexture();

        zoneDirtMap.ApplyToTargetRenderers();
        EditorUtility.SetDirty(zoneDirtMap);
        SceneView.RepaintAll();
    }
}

[InitializeOnLoad]
static class ZoneDirtMapPlayModeRefresh
{
    static ZoneDirtMapPlayModeRefresh()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange playModeState)
    {
        if (playModeState != PlayModeStateChange.EnteredEditMode)
            return;

        ZoneDirtMap[] zoneDirtMaps = Object.FindObjectsByType<ZoneDirtMap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ZoneDirtMap zoneDirtMap in zoneDirtMaps)
        {
            if (zoneDirtMap == null)
                continue;

            ZoneDirtMapEditor.RefreshZoneDirt(zoneDirtMap, rebuildTextures: true);
        }
    }
}
