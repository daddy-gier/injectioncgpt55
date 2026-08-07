using UnityEditor;
using UnityEngine;

namespace UnityFixes.Editor
{
    /// <summary>
    /// Batch-fixes the "MaterialLocation.External is obsolete" import
    /// warning by switching every model in the project from the
    /// deprecated "Use External Materials (Legacy)" setting over to
    /// "Use Embedded Materials".
    /// </summary>
    public static class FixObsoleteMaterialLocation
    {
        [MenuItem("Tools/Unity Fixes/Fix Obsolete External Material Location (Whole Project)")]
        private static void FixWholeProject()
        {
            Run("Assets");
        }

        private static void Run(string searchFolder)
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { searchFolder });
            int fixedCount = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) continue;

#pragma warning disable 618 // materialLocation / External are the obsolete members we're specifically cleaning up.
                if (importer.materialLocation == ModelImporterMaterialLocation.External)
                {
                    importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                    importer.SaveAndReimport();
                    fixedCount++;
                    Debug.Log($"[FixObsoleteMaterialLocation] Fixed: {path}");
                }
#pragma warning restore 618
            }

            Debug.Log($"[FixObsoleteMaterialLocation] Done — updated {fixedCount} model(s) under '{searchFolder}'.");
        }
    }
}
