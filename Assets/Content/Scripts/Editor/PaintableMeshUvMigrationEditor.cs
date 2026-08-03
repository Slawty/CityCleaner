using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class PaintableMeshUvMigrationEditor
{
    [MenuItem("City Cleaner/Mesh/Migrate Paintable UVs (UV1 to UV3)")]
    static void MigratePaintableUvs()
    {
        bool generateMissingLightmapUvs = EditorUtility.DisplayDialog(
            "Migrate Paintable UVs",
            "Copy lightmap UVs (channel 1) to dirt UVs (channel 3) on paintable meshes.\n\nGenerate lightmap UVs first when channel 1 is missing?",
            "Generate If Missing",
            "Skip Missing");

        HashSet<Mesh> meshes = PaintableMeshUvUtility.CollectPaintableMeshes(includeCleaningMeshAssets: true);
        if (meshes.Count == 0)
        {
            EditorUtility.DisplayDialog("Migrate Paintable UVs", "No paintable meshes were found.", "OK");
            return;
        }

        UnwrapParam unwrapParam = PaintableMeshUvUtility.CreateDefaultUnwrapParam();
        int migratedCount = 0;
        int generatedCount = 0;
        int skippedUnreadableCount = 0;
        int skippedMissingCount = 0;
        StringBuilder skippedNames = new StringBuilder();

        try
        {
            int processedCount = 0;
            foreach (Mesh mesh in meshes)
            {
                processedCount++;
                EditorUtility.DisplayProgressBar(
                    "Migrate Paintable UVs",
                    mesh.name,
                    (float)processedCount / meshes.Count);

                if (!mesh.isReadable)
                {
                    skippedUnreadableCount++;
                    skippedNames.AppendLine($"{mesh.name} (not readable)");
                    continue;
                }

                if (!PaintableMeshUvUtility.HasLightmapUvs(mesh))
                {
                    if (!generateMissingLightmapUvs)
                    {
                        skippedMissingCount++;
                        skippedNames.AppendLine($"{mesh.name} (missing UV1)");
                        continue;
                    }

                    if (!PaintableMeshUvUtility.GenerateLightmapUvs(mesh, unwrapParam))
                    {
                        skippedMissingCount++;
                        skippedNames.AppendLine($"{mesh.name} (failed to generate UV1)");
                        continue;
                    }

                    generatedCount++;
                }

                if (!PaintableMeshUvUtility.CopyLightmapUvsToDirtChannel(mesh))
                {
                    skippedMissingCount++;
                    skippedNames.AppendLine($"{mesh.name} (failed to copy UV1 to UV3)");
                    continue;
                }

                EditorUtility.SetDirty(mesh);
                migratedCount++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();

        string summary =
            $"Migrated: {migratedCount}\n" +
            $"Generated UV1: {generatedCount}\n" +
            $"Skipped (not readable): {skippedUnreadableCount}\n" +
            $"Skipped (missing/failed): {skippedMissingCount}";

        if (skippedNames.Length > 0)
            summary += $"\n\nSkipped meshes:\n{skippedNames}";

        EditorUtility.DisplayDialog("Migrate Paintable UVs", summary, "OK");
    }
}
