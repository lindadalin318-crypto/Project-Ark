#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Replaces the Ship prefab's legacy boost trail with the GG BoostTrailRoot prefab.
    /// Menu: ProjectArk > Ship > Replace Ship Boost Trail (GG)
    ///       ProjectArk > Ship > FORCE Replace Ship Boost Trail (GG)
    /// </summary>
    public static class ShipBoostTrailPrefabReplacer
    {
        private const string ShipPrefabPath = "Assets/_Prefabs/Ship/Ship.prefab";
        private const string BoostTrailPrefabName = "BoostTrailRoot";
        private const string VisualRootName = "ShipVisual";
        private const string LegacyVisualRootName = "VisualChild";
        private const string BackSpriteName = "Ship_Sprite_Back";
        private const string LegacyBoostTrailParticles = "BoostTrailParticles";
        private const string LegacyBoostEmberParticles = "BoostEmberParticles";
        private const string LegacyBoostTrail = "BoostTrail";
        private const string BoostLiquidSpriteName = "Boost_16";
        private const string NormalLiquidSpriteName = "Movement_3";

        [MenuItem("ProjectArk/Ship/Replace Ship Boost Trail (GG)")]
        public static void ReplaceShipBoostTrail()
        {
            Run(forceReplace: false);
        }

        [MenuItem("ProjectArk/Ship/FORCE Replace Ship Boost Trail (GG)")]
        public static void ForceReplaceShipBoostTrail()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Force Replace Ship Boost Trail",
                "This will replace the Ship prefab's boost trail with the GG BoostTrailRoot setup and remove legacy boost trail nodes/components.\n\nContinue?",
                "Yes, Replace",
                "Cancel");

            if (confirmed)
                Run(forceReplace: true);
        }

        private static void Run(bool forceReplace)
        {
            var log = new List<string>();
            var todo = new List<string>();

            MaterialTextureLinker.LinkAllMaterialTextures(showDialog: false);
            log.Add("✓ BoostTrail materials relinked");

            var boostTrailPrefab = BoostTrailPrefabCreator.CreateOrRebuildBoostTrailRootPrefab(showDialog: false);
            if (boostTrailPrefab == null)
            {
                EditorUtility.DisplayDialog("Replace Ship Boost Trail", "BoostTrailRoot prefab could not be created.", "OK");
                return;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(ShipPrefabPath))
            {
                var root = scope.prefabContentsRoot;
                var visualRoot = root.transform.Find(VisualRootName) ?? root.transform.Find(LegacyVisualRootName);

                if (visualRoot == null)
                {
                    var visualGo = new GameObject(VisualRootName);
                    visualGo.transform.SetParent(root.transform, false);
                    visualRoot = visualGo.transform;
                    log.Add("✓ Created ShipVisual root");
                }

                ReplaceBoostTrailChild(root, visualRoot, boostTrailPrefab, log);
                RemoveLegacyBoostTrailNodes(visualRoot, log);
                RemoveLegacyBoostTrailComponent(root, log);

                WireShipView(root, visualRoot, log, todo);
            }

            var summary = BuildSummary(forceReplace, log, todo);
            Debug.Log("[ShipBoostTrailPrefabReplacer] Done.\n" + summary);
            EditorUtility.DisplayDialog(
                forceReplace ? "Ship Boost Trail Force-Replaced" : "Ship Boost Trail Replaced",
                summary,
                "OK");
        }

        private static void ReplaceBoostTrailChild(GameObject root, Transform visualRoot, GameObject boostTrailPrefab, List<string> log)
        {
            var existing = visualRoot.Find(BoostTrailPrefabName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
                log.Add("✗ Removed existing BoostTrailRoot child");
            }

            var instance = PrefabUtility.InstantiatePrefab(boostTrailPrefab, root.scene) as GameObject;
            if (instance == null)
            {
                log.Add("⚠ Failed to instantiate BoostTrailRoot prefab");
                return;
            }

            instance.name = BoostTrailPrefabName;
            instance.transform.SetParent(visualRoot, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            log.Add("✓ Instantiated BoostTrailRoot under ShipVisual");
        }

        private static void RemoveLegacyBoostTrailNodes(Transform visualRoot, List<string> log)
        {
            var back = visualRoot.Find(BackSpriteName);
            if (back == null)
                return;

            DestroyChildIfExists(back, LegacyBoostTrailParticles, log);
            DestroyChildIfExists(back, LegacyBoostEmberParticles, log);
            DestroyChildIfExists(back, LegacyBoostTrail, log);
        }

        private static void RemoveLegacyBoostTrailComponent(GameObject root, List<string> log)
        {
            var legacyComponent = root.GetComponent<ShipBoostTrailVFX>();
            if (legacyComponent == null)
                return;

            Object.DestroyImmediate(legacyComponent);
            log.Add("✗ Removed legacy ShipBoostTrailVFX component");
        }

        private static void DestroyChildIfExists(Transform parent, string childName, List<string> log)
        {
            var child = parent.Find(childName);
            if (child == null)
                return;

            Object.DestroyImmediate(child.gameObject);
            log.Add($"✗ Removed legacy node '{childName}'");
        }

        private static void WireShipView(GameObject root, Transform visualRoot, List<string> log, List<string> todo)
        {
            var shipView = root.GetComponent<ShipView>();
            if (shipView == null)
            {
                todo.Add("ShipView missing on Ship prefab; wire BoostTrailRoot manually.");
                return;
            }

            var boostTrailView = visualRoot.GetComponentInChildren<BoostTrailView>(true);
            if (boostTrailView == null)
            {
                todo.Add("BoostTrailRoot created, but BoostTrailView component was not found.");
                return;
            }

            var shipViewSO = new SerializedObject(shipView);
            WireField(shipViewSO, "_boostTrailView", boostTrailView, log, "ShipView._boostTrailView");
            ClearField(shipViewSO, "_boostTrail", log, "ShipView._boostTrail");

            var boostSprite = FindSprite(BoostLiquidSpriteName);
            if (boostSprite != null)
            {
                WireField(shipViewSO, "_boostLiquidSprite", boostSprite, log, "ShipView._boostLiquidSprite");
            }
            else
            {
                todo.Add("Boost_16 sprite not found; assign ShipView._boostLiquidSprite manually.");
            }

            var normalSprite = FindSprite(NormalLiquidSpriteName);
            if (normalSprite != null)
            {
                WireField(shipViewSO, "_normalLiquidSprite", normalSprite, log, "ShipView._normalLiquidSprite");
            }
            else
            {
                todo.Add("Movement_3 sprite not found; assign ShipView._normalLiquidSprite manually.");
            }

            shipViewSO.ApplyModifiedProperties();
        }

        private static void WireField(SerializedObject so, string propertyPath, Object value, List<string> log, string label)
        {
            var prop = so.FindProperty(propertyPath);
            if (prop == null)
                return;

            if (prop.objectReferenceValue == value)
            {
                log.Add($"✓ {label} already wired");
                return;
            }

            prop.objectReferenceValue = value;
            log.Add($"✓ {label} wired");
        }

        private static void ClearField(SerializedObject so, string propertyPath, List<string> log, string label)
        {
            var prop = so.FindProperty(propertyPath);
            if (prop == null || prop.objectReferenceValue == null)
                return;

            prop.objectReferenceValue = null;
            log.Add($"✓ {label} cleared");
        }

        private static Sprite FindSprite(string nameFilter)
        {
            var guids = AssetDatabase.FindAssets($"{nameFilter} t:Sprite");
            if (guids.Length == 0)
                return null;

            return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static string BuildSummary(bool forceReplace, List<string> log, List<string> todo)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(forceReplace
                ? "── FORCE BOOST TRAIL REPLACE COMPLETED ──"
                : "── BOOST TRAIL REPLACE COMPLETED ───────");

            foreach (var entry in log)
                sb.AppendLine(entry);

            if (todo.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("── MANUAL STEPS REQUIRED ──");
                for (int i = 0; i < todo.Count; i++)
                    sb.AppendLine($"{i + 1}. {todo[i]}");
            }

            return sb.ToString();
        }
    }
}
#endif
