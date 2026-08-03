using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PaintableMeshUvPostprocessor : AssetPostprocessor
{
    void OnPostprocessModel(GameObject root)
    {
        ModelImporter importer = assetImporter as ModelImporter;
        if (importer == null || !importer.generateSecondaryUV)
            return;

        if (!assetPath.StartsWith(PaintableMeshUvUtility.CleaningContentRoot))
            return;

        HashSet<Mesh> meshes = new HashSet<Mesh>();
        PaintableMeshUvUtility.CollectMeshesFromHierarchy(root, meshes);

        foreach (Mesh mesh in meshes)
            PaintableMeshUvUtility.CopyLightmapUvsToDirtChannel(mesh);
    }
}
