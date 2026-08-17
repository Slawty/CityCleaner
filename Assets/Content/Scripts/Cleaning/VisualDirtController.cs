using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class VisualDirtController : MonoBehaviour
{
    [Header("Dirt Zones")]
    [Tooltip("Zone dirt maps are processed top to bottom. Earlier zones claim renderers first.")]
    [SerializeField] List<ZoneDirtMap> zoneDirtMaps = new();

    public IReadOnlyList<ZoneDirtMap> ZoneDirtMaps => zoneDirtMaps;

    void Start()
    {
        if (!Application.isPlaying || runtimeRefreshDone)
            return;

        runtimeRefreshDone = true;
        RefreshAll(rebuildTextures: true);
    }

    void OnDestroy()
    {
        runtimeRefreshDone = false;
    }

    static bool runtimeRefreshDone;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRuntimeRefresh()
    {
        runtimeRefreshDone = false;
    }

    public static void RefreshAll(bool rebuildTextures = true)
    {
        HashSet<Renderer> ownedRenderers = new();
        HashSet<ZoneDirtMap> processedMaps = new();

        VisualDirtController[] controllers = Object.FindObjectsByType<VisualDirtController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (VisualDirtController controller in controllers)
        {
            if (controller == null)
                continue;

            controller.Refresh(ownedRenderers, processedMaps, rebuildTextures);
        }

        RefreshUnlistedZoneDirtMaps(ownedRenderers, processedMaps, rebuildTextures);
    }

    public void Refresh(bool rebuildTextures = true)
    {
        HashSet<Renderer> ownedRenderers = new();
        HashSet<ZoneDirtMap> processedMaps = new();
        Refresh(ownedRenderers, processedMaps, rebuildTextures);
    }

    public void Refresh(HashSet<Renderer> ownedRenderers, HashSet<ZoneDirtMap> processedMaps, bool rebuildTextures)
    {
        foreach (ZoneDirtMap zoneDirtMap in zoneDirtMaps)
        {
            if (zoneDirtMap == null || processedMaps.Contains(zoneDirtMap))
                continue;

            RefreshZoneDirtMap(zoneDirtMap, ownedRenderers, rebuildTextures);
            processedMaps.Add(zoneDirtMap);
        }
    }

    public void CollectZoneDirtMapsFromChildren()
    {
        zoneDirtMaps.Clear();
        CollectZoneDirtMapsInHierarchyOrder(transform, zoneDirtMaps);
    }

    static void RefreshUnlistedZoneDirtMaps(HashSet<Renderer> ownedRenderers, HashSet<ZoneDirtMap> processedMaps, bool rebuildTextures)
    {
        ZoneDirtMap[] unlistedZoneDirtMaps = Object.FindObjectsByType<ZoneDirtMap>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (ZoneDirtMap zoneDirtMap in unlistedZoneDirtMaps)
        {
            if (zoneDirtMap == null || processedMaps.Contains(zoneDirtMap))
                continue;

            RefreshZoneDirtMap(zoneDirtMap, ownedRenderers, rebuildTextures);
            processedMaps.Add(zoneDirtMap);
        }
    }

    static void RefreshZoneDirtMap(ZoneDirtMap zoneDirtMap, HashSet<Renderer> ownedRenderers, bool rebuildTextures)
    {
        zoneDirtMap.UpdateZoneBounds();
        if (rebuildTextures)
            zoneDirtMap.RebuildZoneTexture();

        zoneDirtMap.ApplyToTargetRenderers(ownedRenderers);
    }

    static void CollectZoneDirtMapsInHierarchyOrder(Transform root, List<ZoneDirtMap> results)
    {
        ZoneDirtMap zoneDirtMap = root.GetComponent<ZoneDirtMap>();
        if (zoneDirtMap != null)
            results.Add(zoneDirtMap);

        for (int childIndex = 0; childIndex < root.childCount; childIndex++)
            CollectZoneDirtMapsInHierarchyOrder(root.GetChild(childIndex), results);
    }
}
