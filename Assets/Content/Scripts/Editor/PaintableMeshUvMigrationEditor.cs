using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class PaintableMeshUvMigrationEditor
{
    const string DialogTitle = "Migrate Paintable UVs";

    [MenuItem("City Cleaner/Mesh/Migrate Paintable UVs (UV1 to UV3)")]
    static void MigratePaintableUvs()
    {
        HashSet<Mesh> meshes = PaintableMeshUvUtility.CollectPaintableMeshes(includeCleaningMeshAssets: true);
        if (meshes.Count == 0)
        {
            EditorUtility.DisplayDialog(DialogTitle, "No paintable meshes were found.", "OK");
            return;
        }

        RunMigration(meshes, "all paintable meshes");
    }

    [MenuItem("City Cleaner/Mesh/Migrate Paintable UVs (Selection)")]
    static void MigrateSelectedPaintableUvs()
    {
        HashSet<Mesh> meshes = PaintableMeshUvUtility.CollectMeshesFromSelection();
        if (meshes.Count == 0)
        {
            EditorUtility.DisplayDialog(DialogTitle, "No meshes were found in the current selection.", "OK");
            return;
        }

        RunMigration(meshes, "selection");
    }

    [MenuItem("City Cleaner/Mesh/Migrate Paintable UVs (Selection)", validate = true)]
    static bool ValidateMigrateSelectedPaintableUvs()
    {
        return PaintableMeshUvUtility.SelectionHasMigratableMeshes();
    }

    static void RunMigration(HashSet<Mesh> meshes, string scopeLabel)
    {
        bool generateMissingLightmapUvs = EditorUtility.DisplayDialog(
            DialogTitle,
            $"Copy lightmap UVs (channel 1) to dirt UVs (channel 3) on {scopeLabel} ({meshes.Count} mesh{(meshes.Count == 1 ? "" : "es")}).\n\nGenerate lightmap UVs first when channel 1 is missing?",
            "Generate If Missing",
            "Skip Missing");

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
                    DialogTitle,
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

        EditorUtility.DisplayDialog(DialogTitle, summary, "OK");
    }
}
