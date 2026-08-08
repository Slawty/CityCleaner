using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PaintableMeshUvUtility
{
    public const int LightmapUvChannel = 1;
    public const int DirtUvChannel = 3;
    public const string CleaningContentRoot = "Assets/Content/Cleaning";

    public static UnwrapParam CreateDefaultUnwrapParam()
    {
        UnwrapParam unwrapParam = new UnwrapParam();
        UnwrapParam.SetDefaults(out unwrapParam);
        unwrapParam.angleError = 8f;
        unwrapParam.areaError = 15f;
        unwrapParam.hardAngle = 88f;
        unwrapParam.packMargin = 0.004f;
        return unwrapParam;
    }

    public static UnwrapParam CreateUnwrapParam(ModelImporter importer)
    {
        UnwrapParam unwrapParam = CreateDefaultUnwrapParam();
        if (importer == null)
            return unwrapParam;

        unwrapParam.angleError = importer.secondaryUVAngleDistortion;
        unwrapParam.areaError = importer.secondaryUVAreaDistortion;
        unwrapParam.hardAngle = importer.secondaryUVHardAngle;
        unwrapParam.packMargin = importer.secondaryUVPackMargin * 0.001f;
        return unwrapParam;
    }

    public static bool HasLightmapUvs(Mesh mesh)
    {
        if (mesh == null)
            return false;

        List<Vector2> lightmapUvs = new List<Vector2>();
        mesh.GetUVs(LightmapUvChannel, lightmapUvs);
        return lightmapUvs.Count == mesh.vertexCount;
    }

    public static bool GenerateLightmapUvs(Mesh mesh, UnwrapParam unwrapParam)
    {
        if (mesh == null || !mesh.isReadable)
            return false;

        Unwrapping.GenerateSecondaryUVSet(mesh, unwrapParam);
        return HasLightmapUvs(mesh);
    }

    public static bool CopyLightmapUvsToDirtChannel(Mesh mesh)
    {
        if (mesh == null || !mesh.isReadable)
            return false;

        List<Vector2> lightmapUvs = new List<Vector2>();
        mesh.GetUVs(LightmapUvChannel, lightmapUvs);
        if (lightmapUvs.Count != mesh.vertexCount)
            return false;

        mesh.SetUVs(DirtUvChannel, lightmapUvs);
        return true;
    }

    public static void CollectMeshesFromHierarchy(GameObject root, HashSet<Mesh> meshes)
    {
        if (root == null)
            return;

        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh != null)
                meshes.Add(meshFilter.sharedMesh);
        }

        SkinnedMeshRenderer[] skinnedMeshRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
        {
            if (skinnedMeshRenderer.sharedMesh != null)
                meshes.Add(skinnedMeshRenderer.sharedMesh);
        }
    }

    public static HashSet<Mesh> CollectPaintableMeshes(bool includeCleaningMeshAssets)
    {
        HashSet<Mesh> meshes = new HashSet<Mesh>();

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Content" }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
                continue;

            foreach (GPUPaintableObject paintable in prefab.GetComponentsInChildren<GPUPaintableObject>(true))
            {
                MeshFilter meshFilter = paintable.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                    meshes.Add(meshFilter.sharedMesh);
            }
        }

        if (!includeCleaningMeshAssets)
            return meshes;

        foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { CleaningContentRoot }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (mesh != null)
                meshes.Add(mesh);
        }

        return meshes;
    }

    public static HashSet<Mesh> CollectMeshesFromSelection()
    {
        HashSet<Mesh> meshes = new HashSet<Mesh>();

        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Mesh mesh)
            {
                meshes.Add(mesh);
                continue;
            }

            if (selectedObject is GameObject gameObject)
                CollectMeshesFromHierarchy(gameObject, meshes);
        }

        return meshes;
    }

    public static bool SelectionHasMigratableMeshes()
    {
        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is Mesh)
                return true;

            if (selectedObject is not GameObject gameObject)
                continue;

            if (gameObject.GetComponentInChildren<MeshFilter>(true) != null)
                return true;

            if (gameObject.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
                return true;
        }

        return false;
    }
}
