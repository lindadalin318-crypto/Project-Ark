#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Editor utility to auto-build all Map UI prefabs and scene hierarchy.
    /// 
    /// Menu Item 1: ProjectArk > Map > Build Map UI Prefabs
    ///   Creates MapRoomWidget, MapConnectionLine, FloorTabButton prefabs.
    ///
    /// Menu Item 2: ProjectArk > Map > Build Map UI Scene
    ///   Creates MapPanel (full-screen) + MinimapHUD (corner) on the Canvas,
    ///   wires all prefab references and SerializeFields.
    ///
    /// Both are idempotent — running multiple times will NOT create duplicates.
    /// </summary>
    public static class MapUIBuilder
    {
        // =====================================================================
        // Constants
        // =====================================================================

        private const string PREFAB_DIR = "Assets/_Prefabs/UI/Map";
        private const string WIDGET_PREFAB_PATH = PREFAB_DIR + "/MapRoomWidget.prefab";
        private const string LINE_PREFAB_PATH = PREFAB_DIR + "/MapConnectionLine.prefab";
        private const string TAB_PREFAB_PATH = PREFAB_DIR + "/FloorTabButton.prefab";

        // =====================================================================
        // Menu Item 1: Build Prefabs
        // =====================================================================

        [MenuItem("ProjectArk/Map/Build Map UI Prefabs")]
        public static void BuildMapUIPrefabs()
        {
            EnsureFolder("Assets", "_Prefabs");
            EnsureFolder("Assets/_Prefabs", "UI");
            EnsureFolder("Assets/_Prefabs/UI", "Map");

            bool anyCreated = false;

            // ── MapRoomWidget ──
            if (AssetDatabase.LoadAssetAtPath<GameObject>(WIDGET_PREFAB_PATH) != null)
            {
                Debug.Log($"[MapUIBuilder] MapRoomWidget prefab already exists at {WIDGET_PREFAB_PATH}");
            }
            else
            {
                BuildMapRoomWidgetPrefab();
                anyCreated = true;
            }

            // ── MapConnectionLine ──
            if (AssetDatabase.LoadAssetAtPath<GameObject>(LINE_PREFAB_PATH) != null)
            {
                Debug.Log($"[MapUIBuilder] MapConnectionLine prefab already exists at {LINE_PREFAB_PATH}");
            }
            else
            {
                BuildMapConnectionLinePrefab();
                anyCreated = true;
            }

            // ── FloorTabButton ──
            if (AssetDatabase.LoadAssetAtPath<GameObject>(TAB_PREFAB_PATH) != null)
            {
                Debug.Log($"[MapUIBuilder] FloorTabButton prefab already exists at {TAB_PREFAB_PATH}");
            }
            else
            {
                BuildFloorTabButtonPrefab();
                anyCreated = true;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (anyCreated)
                Debug.Log("[MapUIBuilder] Map UI Prefabs created successfully.");
            else
                Debug.Log("[MapUIBuilder] All Map UI Prefabs already exist. Nothing to create.");
        }

        // =====================================================================
        // Menu Item 2: Build Scene Hierarchy
        // =====================================================================

        [MenuItem("ProjectArk/Map/Build Map UI Scene")]
        public static void BuildMapUIScene()
        {
            // ── Load prefabs (required) ──
            var widgetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WIDGET_PREFAB_PATH);
            var linePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LINE_PREFAB_PATH);
            var tabPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TAB_PREFAB_PATH);

            if (widgetPrefab == null || linePrefab == null || tabPrefab == null)
            {
                Debug.LogError("[MapUIBuilder] Prefabs not found! Run 'ProjectArk > Map > Build Map UI Prefabs' first.");
                return;
            }

            // ── Find or create Canvas ──
            var canvasGo = FindOrCreateCanvas();

            // ── MapPanel ──
            var existingMapPanel = Object.FindAnyObjectByType<MapPanel>(FindObjectsInactive.Include);
            if (existingMapPanel != null)
            {
                Debug.Log("[MapUIBuilder] MapPanel already exists in scene, skipping.");
            }
            else
            {
                BuildMapPanelSection(canvasGo, widgetPrefab, linePrefab, tabPrefab);
            }

            // ── MinimapHUD ──
            var existingMinimap = Object.FindAnyObjectByType<MinimapHUD>(FindObjectsInactive.Include);
            if (existingMinimap != null)
            {
                Debug.Log("[MapUIBuilder] MinimapHUD already exists in scene, skipping.");
            }
            else
            {
                BuildMinimapHUDSection(canvasGo, widgetPrefab, linePrefab);
            }

            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("  MAP UI SCENE BUILD COMPLETE");
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("  MapPanel: Full-screen map (M key / Gamepad Select)");
            Debug.Log("  MinimapHUD: Corner overlay (bottom-right, 200x200)");
            Debug.Log("───────────────────────────────────────────────────────────");
            Debug.Log("  Post-setup:");
            Debug.Log("  1. Verify InputActionAsset is wired on MapPanel");
            Debug.Log("  2. Adjust MinimapHUD anchor/size to taste");
            Debug.Log("  3. Optional: create a smaller variant of MapRoomWidget for MinimapHUD");
            Debug.Log("═══════════════════════════════════════════════════════════");
        }

        // =====================================================================
        // Prefab Builders
        // =====================================================================

        private static void BuildMapRoomWidgetPrefab()
        {
            var root = new GameObject("MapRoomWidget");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(60, 40);

            var widget = root.AddComponent<MapRoomWidget>();

            // Background — fills entire widget
            var bgGo = CreateChild("Background", root.transform);
            SetStretch(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.35f, 0.55f, 0.75f, 0.85f);
            bgImg.raycastTarget = false;

            // Icon Overlay — centered, slightly smaller
            var iconGo = CreateChild("IconOverlay", root.transform);
            SetAnchors(iconGo, new Vector2(0.2f, 0.25f), new Vector2(0.8f, 0.85f));
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;
            iconImg.enabled = false;

            // Current Highlight — border ring (stretch with slight padding)
            var highlightGo = CreateChild("CurrentHighlight", root.transform);
            SetStretch(highlightGo);
            var highlightImg = highlightGo.AddComponent<Image>();
            highlightImg.color = new Color(1f, 0.85f, 0.25f, 1f);
            highlightImg.type = Image.Type.Sliced;
            highlightImg.raycastTarget = false;
            highlightImg.enabled = false;
            // Move highlight behind background won't work; highlight should be on top
            highlightGo.transform.SetAsLastSibling();

            // Fog Overlay — dark cover for unvisited rooms
            var fogGo = CreateChild("FogOverlay", root.transform);
            SetStretch(fogGo);
            var fogImg = fogGo.AddComponent<Image>();
            fogImg.color = new Color(0.15f, 0.15f, 0.20f, 0.90f);
            fogImg.raycastTarget = false;

            // Label — room name at bottom
            var labelGo = CreateChild("Label", root.transform);
            SetAnchors(labelGo, new Vector2(0f, -0.4f), new Vector2(1f, 0f));
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "";
            labelTmp.fontSize = 8;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = Color.white;
            labelTmp.raycastTarget = false;
            labelTmp.enabled = false;

            // Reorder: Background → IconOverlay → FogOverlay → CurrentHighlight → Label
            bgGo.transform.SetSiblingIndex(0);
            iconGo.transform.SetSiblingIndex(1);
            fogGo.transform.SetSiblingIndex(2);
            highlightGo.transform.SetSiblingIndex(3);
            labelGo.transform.SetSiblingIndex(4);

            // Wire fields
            WireField(widget, "_background", bgImg);
            WireField(widget, "_iconOverlay", iconImg);
            WireField(widget, "_currentHighlight", highlightImg);
            WireField(widget, "_fogOverlay", fogImg);
            WireField(widget, "_labelText", labelTmp);

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(root, WIDGET_PREFAB_PATH);
            Object.DestroyImmediate(root);

            Debug.Log($"[MapUIBuilder] Created MapRoomWidget prefab at {WIDGET_PREFAB_PATH}");
        }

        private static void BuildMapConnectionLinePrefab()
        {
            var root = new GameObject("MapConnectionLine");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(100, 2);
            rootRect.pivot = new Vector2(0f, 0.5f);

            var img = root.AddComponent<Image>();
            img.color = new Color(0.5f, 0.6f, 0.7f, 0.6f);
            img.raycastTarget = false;

            root.AddComponent<MapConnectionLine>();

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(root, LINE_PREFAB_PATH);
            Object.DestroyImmediate(root);

            Debug.Log($"[MapUIBuilder] Created MapConnectionLine prefab at {LINE_PREFAB_PATH}");
        }

        private static void BuildFloorTabButtonPrefab()
        {
            var root = new GameObject("FloorTabButton");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(80, 30);

            var btnImg = root.AddComponent<Image>();
            btnImg.color = new Color(0.25f, 0.25f, 0.3f, 1f);

            var btn = root.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            // Label child
            var labelGo = CreateChild("Label", root.transform);
            SetStretch(labelGo);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "F0";
            labelTmp.fontSize = 14;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = Color.white;
            labelTmp.raycastTarget = false;

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(root, TAB_PREFAB_PATH);
            Object.DestroyImmediate(root);

            Debug.Log($"[MapUIBuilder] Created FloorTabButton prefab at {TAB_PREFAB_PATH}");
        }

        // =====================================================================
        // Scene Section Builders
        // =====================================================================

        private static void BuildMapPanelSection(
            GameObject canvasGo,
            GameObject widgetPrefab,
            GameObject linePrefab,
            GameObject tabPrefab)
        {
            // ── Root: MapPanel ──
            var panelGo = CreateChild("MapPanel", canvasGo.transform);
            SetStretch(panelGo);
            panelGo.AddComponent<CanvasGroup>();
            var mapPanel = panelGo.AddComponent<MapPanel>();
            Undo.RegisterCreatedObjectUndo(panelGo, "Create MapPanel");

            // ── Dark Background ──
            var bgGo = CreateChild("Background", panelGo.transform);
            SetStretch(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.10f, 0.85f);
            bgImg.raycastTarget = true;

            // ── ScrollView ──
            var scrollGo = CreateChild("ScrollView", panelGo.transform);
            SetAnchors(scrollGo, new Vector2(0f, 0.05f), new Vector2(1f, 0.92f));
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 1f;

            // Viewport
            var viewportGo = CreateChild("Viewport", scrollGo.transform);
            SetStretch(viewportGo);
            var viewportImg = viewportGo.AddComponent<Image>();
            viewportImg.color = new Color(0, 0, 0, 0.01f); // Nearly invisible but needed for Mask
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;
            var viewportRect = viewportGo.GetComponent<RectTransform>();

            // MapContent
            var contentGo = CreateChild("MapContent", viewportGo.transform);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(2000, 2000);

            // Wire ScrollRect
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            // ── Floor Tab Bar ──
            var tabBarGo = CreateChild("FloorTabBar", panelGo.transform);
            SetAnchors(tabBarGo, new Vector2(0.1f, 0.93f), new Vector2(0.9f, 1f));
            var hlg = tabBarGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // ── Player Icon ──
            var iconGo = CreateChild("PlayerIcon", contentGo.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(12, 12);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(1f, 0.85f, 0.25f, 1f);
            iconImg.raycastTarget = false;
            // Rotate 45 degrees for diamond shape
            iconRect.localRotation = Quaternion.Euler(0, 0, 45);

            // ── Wire MapPanel fields ──
            WireField(mapPanel, "_scrollRect", scrollRect);
            WireField(mapPanel, "_mapContent", contentRect);
            WireField(mapPanel, "_floorTabContainer", tabBarGo.GetComponent<RectTransform>());
            WireField(mapPanel, "_playerIcon", iconRect);

            // Wire prefab references
            WireField(mapPanel, "_roomWidgetPrefab", widgetPrefab.GetComponent<MapRoomWidget>());
            WireField(mapPanel, "_connectionLinePrefab", widgetPrefab.GetComponent<MapConnectionLine>() ??
                linePrefab.GetComponent<MapConnectionLine>());
            WireField(mapPanel, "_connectionLinePrefab", linePrefab.GetComponent<MapConnectionLine>());
            WireField(mapPanel, "_floorTabPrefab", tabPrefab.GetComponent<Button>());

            // Wire InputActionAsset
            var inputAsset = FindInputActionAsset();
            if (inputAsset != null)
            {
                WireField(mapPanel, "_inputActions", inputAsset);
            }

            // Start inactive (MapPanel.Awake will call SetActive(false) anyway)
            panelGo.SetActive(false);

            Debug.Log("[MapUIBuilder] Created MapPanel on Canvas.");
        }

        private static void BuildMinimapHUDSection(
            GameObject canvasGo,
            GameObject widgetPrefab,
            GameObject linePrefab)
        {
            // ── Root: MinimapHUD (anchored bottom-right) ──
            var hudGo = CreateChild("MinimapHUD", canvasGo.transform);
            var hudRect = hudGo.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(1f, 0f);
            hudRect.anchorMax = new Vector2(1f, 0f);
            hudRect.pivot = new Vector2(1f, 0f);
            hudRect.anchoredPosition = new Vector2(-10f, 10f);
            hudRect.sizeDelta = new Vector2(200, 200);

            var minimapHUD = hudGo.AddComponent<MinimapHUD>();
            Undo.RegisterCreatedObjectUndo(hudGo, "Create MinimapHUD");

            // ── Background ──
            var bgGo = CreateChild("Background", hudGo.transform);
            SetStretch(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.80f);
            bgImg.raycastTarget = false;

            // ── Border (outline effect) ──
            var borderGo = CreateChild("Border", hudGo.transform);
            SetStretch(borderGo);
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(0.3f, 0.4f, 0.6f, 0.6f);
            borderImg.type = Image.Type.Sliced;
            borderImg.raycastTarget = false;
            // Border behind content
            borderGo.transform.SetSiblingIndex(0);
            bgGo.transform.SetSiblingIndex(1);

            // ── Content (masked area for room widgets) ──
            var contentGo = CreateChild("Content", hudGo.transform);
            SetAnchors(contentGo, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.90f));
            var contentMask = contentGo.AddComponent<Image>();
            contentMask.color = new Color(0, 0, 0, 0.01f);
            contentGo.AddComponent<Mask>().showMaskGraphic = false;
            var contentRect = contentGo.GetComponent<RectTransform>();

            // ── Floor Label (top area) ──
            var labelGo = CreateChild("FloorLabel", hudGo.transform);
            SetAnchors(labelGo, new Vector2(0.02f, 0.90f), new Vector2(0.40f, 1f));
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "F0";
            labelTmp.fontSize = 12;
            labelTmp.alignment = TextAlignmentOptions.TopLeft;
            labelTmp.color = new Color(0.7f, 0.8f, 0.9f, 1f);
            labelTmp.raycastTarget = false;

            // ── Wire MinimapHUD fields ──
            WireField(minimapHUD, "_content", contentRect);
            WireField(minimapHUD, "_background", bgImg);
            WireField(minimapHUD, "_floorLabel", labelTmp);

            // Wire prefab references
            WireField(minimapHUD, "_miniRoomWidgetPrefab", widgetPrefab.GetComponent<MapRoomWidget>());
            WireField(minimapHUD, "_miniConnectionLinePrefab", linePrefab.GetComponent<MapConnectionLine>());

            Debug.Log("[MapUIBuilder] Created MinimapHUD on Canvas (bottom-right corner).");
        }

        // =====================================================================
        // Helpers — UI Construction
        // =====================================================================

        private static GameObject CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;
            return go;
        }

        private static void SetStretch(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // =====================================================================
        // Helpers — Field Wiring
        // =====================================================================

        private static void WireField(Component target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"[MapUIBuilder] Field '{fieldName}' not found on {target.GetType().Name}");
            }
        }

        // =====================================================================
        // Helpers — Asset Lookup
        // =====================================================================

        private static UnityEngine.InputSystem.InputActionAsset FindInputActionAsset()
        {
            var guids = AssetDatabase.FindAssets("ShipActions t:InputActionAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(path);
                if (asset != null)
                {
                    Debug.Log($"[MapUIBuilder] Auto-found InputActionAsset: {path}");
                    return asset;
                }
            }

            // Fallback: find any InputActionAsset
            guids = AssetDatabase.FindAssets("t:InputActionAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(path);
                if (asset != null)
                {
                    Debug.Log($"[MapUIBuilder] Auto-found InputActionAsset (fallback): {path}");
                    return asset;
                }
            }

            Debug.LogWarning("[MapUIBuilder] No InputActionAsset found. Assign _inputActions on MapPanel manually.");
            return null;
        }

        private static GameObject FindOrCreateCanvas()
        {
            // Strategy 1: Find Canvas containing MapPanel or MinimapHUD
            var existingMapPanel = Object.FindAnyObjectByType<MapPanel>(FindObjectsInactive.Include);
            if (existingMapPanel != null)
            {
                var parentCanvas = existingMapPanel.GetComponentInParent<Canvas>();
                if (parentCanvas != null) return parentCanvas.gameObject;
            }

            // Strategy 2: Find the main UI Canvas (via UIManager pattern)
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    Debug.Log($"[MapUIBuilder] Found existing Canvas: {canvas.gameObject.name}");
                    return canvas.gameObject;
                }
            }

            // Strategy 3: Create a new Canvas
            var canvasGo = new GameObject("MapCanvas");
            var c = canvasGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 15; // Above game, below menus

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGo.AddComponent<GraphicRaycaster>();

            Undo.RegisterCreatedObjectUndo(canvasGo, "Create MapCanvas");
            Debug.Log("[MapUIBuilder] Created new Canvas for Map UI.");

            return canvasGo;
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string fullPath = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(fullPath))
                AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
