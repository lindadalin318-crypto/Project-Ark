#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectArk.SpaceLife.EditorSupport
{
    /// <summary>
    /// Single source of truth for locating the project's <c>ShipActions</c>
    /// <see cref="InputActionAsset"/> from Editor code. Consolidates the
    /// previously duplicated <c>AssetDatabase.FindAssets("ShipActions t:InputActionAsset")</c>
    /// calls scattered across SpaceLife runtime/editor scripts.
    /// <para>
    /// Lives in the Runtime assembly but is compiled only under
    /// <c>UNITY_EDITOR</c>. This placement is intentional: Runtime
    /// <c>TryFindInputActionAsset</c> helpers (themselves guarded by
    /// <c>#if UNITY_EDITOR</c>) need to call this without Runtime→Editor
    /// assembly dependency, which would violate asmdef reference rules.
    /// </para>
    /// <para>
    /// Do NOT reference from runtime code paths that survive player builds.
    /// Strictly a developer convenience (e.g. auto-fill serialized
    /// <see cref="InputActionAsset"/> slots when missing in Edit Mode).
    /// </para>
    /// </summary>
    public static class ShipInputActionLocator
    {
        private const string SEARCH_FILTER = "ShipActions t:InputActionAsset";
        private const string CANONICAL_PATH = "Assets/Input/ShipActions.inputactions";

        /// <summary>
        /// Locates the canonical <c>ShipActions</c> <see cref="InputActionAsset"/>
        /// via <see cref="AssetDatabase"/>. Prefers the canonical path for
        /// determinism; falls back to a filtered search.
        /// </summary>
        /// <param name="assetPath">Resolved asset path, or <c>null</c> when not found.</param>
        /// <returns>The loaded <see cref="InputActionAsset"/>, or <c>null</c>.</returns>
        public static InputActionAsset Find(out string assetPath)
        {
            // Preferred: canonical path. Stable across refactors; avoids
            // non-deterministic ordering of AssetDatabase.FindAssets.
            var canonical = AssetDatabase.LoadAssetAtPath<InputActionAsset>(CANONICAL_PATH);
            if (canonical != null)
            {
                assetPath = CANONICAL_PATH;
                return canonical;
            }

            // Fallback: filtered search (kept for robustness if the file is moved).
            var guids = AssetDatabase.FindAssets(SEARCH_FILTER);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (asset != null)
                {
                    assetPath = path;
                    return asset;
                }
            }

            assetPath = null;
            return null;
        }

        /// <summary>
        /// Convenience overload for callers that don't care about the resolved
        /// asset path (e.g. runtime <c>TryFindInputActionAsset</c> helpers).
        /// </summary>
        public static InputActionAsset Find()
        {
            return Find(out _);
        }

        /// <summary>
        /// Convenience overload that returns an <see cref="UnityEngine.Object"/>
        /// for call sites that use the base type (e.g. legacy editor wizards
        /// that assign to <c>UnityEngine.Object</c> fields).
        /// </summary>
        public static Object FindAsObject(out string assetPath)
        {
            return Find(out assetPath);
        }
    }
}
#endif
