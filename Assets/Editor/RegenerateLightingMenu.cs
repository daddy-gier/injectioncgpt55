using UnityEditor;
using UnityEngine;

namespace UnityFixes.Editor
{
    /// <summary>
    /// One-click stand-ins for Window > Rendering > Lighting > Generate
    /// Lighting, for when you'd rather trigger a rebake from a menu (or
    /// a batch/CI script) than hunt through the Lighting window.
    /// </summary>
    public static class RegenerateLightingMenu
    {
        [MenuItem("Tools/Unity Fixes/Regenerate Lighting (Bake, Blocking)")]
        private static void BakeBlocking()
        {
            Debug.Log("[RegenerateLighting] Baking... this will freeze the Editor until it's done.");
            Lightmapping.Bake();
        }

        [MenuItem("Tools/Unity Fixes/Regenerate Lighting (Bake, Async)")]
        private static void BakeAsync()
        {
            Debug.Log("[RegenerateLighting] Baking asynchronously — watch the progress bar in the bottom right.");
            Lightmapping.BakeAsync();
        }
    }
}
