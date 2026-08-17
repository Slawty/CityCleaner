using UnityEditor;
using UnityEditor.SceneManagement;
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
static class ZoneDirtMapEditorRefresh
{
    static ZoneDirtMapEditorRefresh()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.delayCall -= RefreshAllZoneDirtInOpenScenes;
        EditorApplication.delayCall += RefreshAllZoneDirtInOpenScenes;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange playModeState)
    {
        if (playModeState != PlayModeStateChange.EnteredEditMode)
            return;

        RefreshAllZoneDirtInOpenScenes();
    }

    static void RefreshAllZoneDirtInOpenScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        VisualDirtController.RefreshAll(rebuildTextures: true);
        SceneView.RepaintAll();
    }
}
