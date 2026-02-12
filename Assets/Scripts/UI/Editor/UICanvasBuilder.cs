
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectArk.Level;

namespace ProjectArk.UI.Editor
{
    /// <summary>
    /// Editor utility to auto-build the entire UI Canvas hierarchy.
    /// Menu: ProjectArk > Build UI Canvas
    ///
    /// Creates (idempotent — skips already-existing sections):
    /// - Canvas (Screen Space - Overlay) with CanvasScaler + GraphicRaycaster
    /// - EventSystem (if missing)
    /// - HeatBarHUD with fill image, overheat flash, and label
    /// - HealthBarHUD with fill image, damage flash, and label
    /// - StarChartPanel with TrackViews, InventoryView, ItemDetailView
    /// - UIManager with all references wired
    /// - WeavingStateTransition on UIManager
    /// - UIParallaxEffect on StarChartPanel
    /// - DoorTransitionController with full-screen FadeOverlay
    ///
    /// All SerializeField references are auto-wired via SerializedObject.
    /// Running this tool multiple times will NOT create duplicate objects.
    /// </summary>
    public static class UICanvasBuilder
    {
        [MenuItem("ProjectArk/Build UI Canvas")]
        public static void BuildUICanvas()
        {
            // ── Step 0: Ensure EventSystem exists ──────────────────
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemGo, "Create EventSystem");
                Debug.Log("[UICanvasBuilder] Created EventSystem");
            }

            // ── Step 1: Find or create Canvas ──────────────────────
            var canvasGo = FindOrCreateCanvas();

            // ── Step 2: HeatBarHUD ─────────────────────────────────
            var heatBarHud = canvasGo.GetComponentInChildren<HeatBarHUD>(true);
            if (heatBarHud == null)
                heatBarHud = BuildHeatBarSection(canvasGo);
            else
                Debug.Log("[UICanvasBuilder] HeatBarHUD already exists, skipping");

            // ── Step 2b: HealthBarHUD ──────────────────────────────
            var healthBarHud = canvasGo.GetComponentInChildren<HealthBarHUD>(true);
            if (healthBarHud == null)
                healthBarHud = BuildHealthBarSection(canvasGo);
            else
                Debug.Log("[UICanvasBuilder] HealthBarHUD already exists, skipping");

            // ── Step 3: StarChartPanel ─────────────────────────────
            var starChartPanel = canvasGo.GetComponentInChildren<StarChartPanel>(true);
            if (starChartPanel == null)
                starChartPanel = BuildStarChartSection(canvasGo);
            else
                Debug.Log("[UICanvasBuilder] StarChartPanel already exists, skipping");

            // ── Step 4: UIManager ──────────────────────────────────
            var uiManager = canvasGo.GetComponentInChildren<UIManager>(true);
            if (uiManager == null)
                uiManager = BuildUIManagerSection(canvasGo, heatBarHud, healthBarHud, starChartPanel);
            else
                Debug.Log("[UICanvasBuilder] UIManager already exists, skipping");

            // ── Step 5: Deactivate StarChartPanel (starts hidden) ──
            if (starChartPanel != null)
                starChartPanel.gameObject.SetActive(false);

            // ── Step 6: DoorTransitionController + FadeOverlay ─────
            var doorTransition = canvasGo.GetComponentInChildren<DoorTransitionController>(true);
            if (doorTransition == null)
                doorTransition = BuildDoorTransitionSection(canvasGo);
            else
                Debug.Log("[UICanvasBuilder] DoorTransitionController already exists, skipping");

            // ── Done ───────────────────────────────────────────────
            Selection.activeGameObject = canvasGo;
            Debug.Log("[UICanvasBuilder] ✅ UI Canvas hierarchy built/verified successfully!");
            Debug.Log("  ├─ Canvas (Screen Space Overlay, 1920x1080 ref)");
            Debug.Log("  ├─ HeatBarHUD (fill + flash + label wired)");
            Debug.Log("  ├─ HealthBarHUD (fill + damage flash + label wired)");
            Debug.Log("  ├─ StarChartPanel (+ UIParallaxEffect)");
            Debug.Log("  │  ├─ PrimaryTrackView (3 prism + 3 core cells)");
            Debug.Log("  │  ├─ SecondaryTrackView (3 prism + 3 core cells)");
            Debug.Log("  │  ├─ InventoryView (filters + scroll grid)");
            Debug.Log("  │  └─ ItemDetailView (icon + name + desc + stats + button)");
            Debug.Log("  ├─ UIManager (+ WeavingStateTransition)");
            Debug.Log("  └─ DoorTransitionController (FadeOverlay)");
            Debug.Log("");
            Debug.Log("[UICanvasBuilder] ⚠️ Manual steps remaining:");
            Debug.Log("  1. Assign _inputActions on UIManager if not auto-found");
            Debug.Log("  2. Assign _playerInventory on UIManager if not auto-found");
            Debug.Log("  3. Create InventoryItemView prefab and assign to InventoryView._itemPrefab");
            Debug.Log("  4. Assign PostProcess Volume to WeavingStateTransition._postProcessVolume");
            Debug.Log("  5. Assign AudioSource to WeavingStateTransition._sfxSource");
        }

        // =====================================================================
        // Section Builders (idempotent — only called when component is missing)
        // =====================================================================

        /// <summary>
        /// Find existing UI Canvas or create a new one.
        /// Looks for an existing Canvas via UIManager, or by matching sortingOrder=10.
        /// </summary>
        private static GameObject FindOrCreateCanvas()
        {
            // Strategy 1: Find via existing UIManager (most reliable indicator of "our" Canvas)
            var existingUIManager = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (existingUIManager != null)
            {
                var parentCanvas = existingUIManager.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    Debug.Log("[UICanvasBuilder] Found existing Canvas (via UIManager)");
                    return parentCanvas.gameObject;
                }
            }

            // Strategy 2: Find a ScreenSpaceOverlay Canvas with sortingOrder=10
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.sortingOrder == 10)
                {
                    Debug.Log("[UICanvasBuilder] Found existing Canvas (sortingOrder=10)");
                    return c.gameObject;
                }
            }

            // Strategy 3: Create new Canvas
            var canvasGo = new GameObject("Canvas");
            Undo.RegisterCreatedObjectUndo(canvasGo, "Build UI Canvas");

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            Debug.Log("[UICanvasBuilder] Created new Canvas");
            return canvasGo;
        }

        private static HeatBarHUD BuildHeatBarSection(GameObject canvasGo)
        {
            var heatBarGo = CreateUIObject("HeatBarHUD", canvasGo.transform);
            SetAnchors(heatBarGo, new Vector2(0.3f, 0f), new Vector2(0.7f, 0.05f));
            var heatBarHud = heatBarGo.AddComponent<HeatBarHUD>();

            // HeatBar background
            var heatBg = CreateUIObject("Background", heatBarGo.transform);
            SetStretch(heatBg);
            var heatBgImg = heatBg.AddComponent<Image>();
            heatBgImg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

            // HeatBar fill
            var heatFill = CreateUIObject("FillImage", heatBarGo.transform);
            SetStretch(heatFill);
            var fillImg = heatFill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.8f, 0.3f, 1f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;

            // Overheat flash overlay
            var heatFlash = CreateUIObject("OverheatFlash", heatBarGo.transform);
            SetStretch(heatFlash);
            var flashImg = heatFlash.AddComponent<Image>();
            flashImg.color = new Color(1f, 0.2f, 0.1f, 0f);
            flashImg.raycastTarget = false;

            // Heat label
            var heatLabel = CreateUIObject("Label", heatBarGo.transform);
            SetStretch(heatLabel);
            var heatTmp = heatLabel.AddComponent<TextMeshProUGUI>();
            heatTmp.text = "HEAT";
            heatTmp.alignment = TextAlignmentOptions.Center;
            heatTmp.fontSize = 16;
            heatTmp.color = Color.white;
            heatTmp.raycastTarget = false;

            // Wire HeatBarHUD fields
            WireField(heatBarHud, "_fillImage", fillImg);
            WireField(heatBarHud, "_overheatFlash", flashImg);
            WireField(heatBarHud, "_label", heatTmp);
            WireGradient(heatBarHud, "_heatGradient", CreateHeatGradient());

            Debug.Log("[UICanvasBuilder] Created HeatBarHUD");
            return heatBarHud;
        }

        private static HealthBarHUD BuildHealthBarSection(GameObject canvasGo)
        {
            var healthBarGo = CreateUIObject("HealthBarHUD", canvasGo.transform);
            SetAnchors(healthBarGo, new Vector2(0.02f, 0.92f), new Vector2(0.28f, 0.97f));
            var healthBarHud = healthBarGo.AddComponent<HealthBarHUD>();

            // HealthBar background
            var healthBg = CreateUIObject("Background", healthBarGo.transform);
            SetStretch(healthBg);
            var healthBgImg = healthBg.AddComponent<Image>();
            healthBgImg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

            // HealthBar fill
            var healthFill = CreateUIObject("FillImage", healthBarGo.transform);
            SetStretch(healthFill);
            var healthFillImg = healthFill.AddComponent<Image>();
            healthFillImg.color = new Color(0.2f, 0.8f, 0.3f, 1f);
            healthFillImg.type = Image.Type.Filled;
            healthFillImg.fillMethod = Image.FillMethod.Horizontal;
            healthFillImg.fillAmount = 1f;

            // Damage flash overlay
            var healthFlash = CreateUIObject("DamageFlash", healthBarGo.transform);
            SetStretch(healthFlash);
            var healthFlashImg = healthFlash.AddComponent<Image>();
            healthFlashImg.color = new Color(1f, 0.2f, 0.1f, 0f);
            healthFlashImg.raycastTarget = false;

            // Health label
            var healthLabel = CreateUIObject("Label", healthBarGo.transform);
            SetStretch(healthLabel);
            var healthTmp = healthLabel.AddComponent<TextMeshProUGUI>();
            healthTmp.text = "HP";
            healthTmp.alignment = TextAlignmentOptions.Center;
            healthTmp.fontSize = 14;
            healthTmp.color = Color.white;
            healthTmp.raycastTarget = false;

            // Wire HealthBarHUD fields
            WireField(healthBarHud, "_fillImage", healthFillImg);
            WireField(healthBarHud, "_damageFlash", healthFlashImg);
            WireField(healthBarHud, "_label", healthTmp);
            WireGradient(healthBarHud, "_healthGradient", CreateHealthGradient());

            Debug.Log("[UICanvasBuilder] Created HealthBarHUD");
            return healthBarHud;
        }

        private static StarChartPanel BuildStarChartSection(GameObject canvasGo)
        {
            var panelGo = CreateUIObject("StarChartPanel", canvasGo.transform);
            SetStretch(panelGo);
            var starChartPanel = panelGo.AddComponent<StarChartPanel>();
            panelGo.AddComponent<UIParallaxEffect>();

            // Panel background
            var panelBg = CreateUIObject("PanelBackground", panelGo.transform);
            SetStretch(panelBg);
            var panelBgImg = panelBg.AddComponent<Image>();
            panelBgImg.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);
            panelBgImg.raycastTarget = true;

            // ── PrimaryTrackView ──
            var primaryTrackGo = CreateUIObject("PrimaryTrackView", panelGo.transform);
            SetAnchors(primaryTrackGo, new Vector2(0.02f, 0.5f), new Vector2(0.48f, 0.95f));
            var primaryTrack = BuildTrackView(primaryTrackGo, "PRIMARY");

            // ── SecondaryTrackView ──
            var secondaryTrackGo = CreateUIObject("SecondaryTrackView", panelGo.transform);
            SetAnchors(secondaryTrackGo, new Vector2(0.52f, 0.5f), new Vector2(0.98f, 0.95f));
            var secondaryTrack = BuildTrackView(secondaryTrackGo, "SECONDARY");

            // ── InventoryView ──
            var inventoryGo = CreateUIObject("InventoryView", panelGo.transform);
            SetAnchors(inventoryGo, new Vector2(0.02f, 0.05f), new Vector2(0.58f, 0.48f));
            var inventoryView = inventoryGo.AddComponent<InventoryView>();

            // Inventory background
            var invBg = CreateUIObject("InvBackground", inventoryGo.transform);
            SetStretch(invBg);
            var invBgImg = invBg.AddComponent<Image>();
            invBgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);

            // Filter buttons bar
            var filterBar = CreateUIObject("FilterBar", inventoryGo.transform);
            SetAnchors(filterBar, new Vector2(0f, 0.88f), new Vector2(1f, 1f));
            filterBar.AddComponent<HorizontalLayoutGroup>().spacing = 4;

            var filterAll = CreateButton("FilterAll", filterBar.transform, "ALL");
            var filterCores = CreateButton("FilterCores", filterBar.transform, "CORES");
            var filterPrisms = CreateButton("FilterPrisms", filterBar.transform, "PRISMS");
            var filterSails = CreateButton("FilterSails", filterBar.transform, "SAILS");
            var filterSats = CreateButton("FilterSatellites", filterBar.transform, "SATS");

            // Inventory scroll area
            var scrollGo = CreateUIObject("ScrollArea", inventoryGo.transform);
            SetAnchors(scrollGo, new Vector2(0f, 0f), new Vector2(1f, 0.86f));
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollGo.AddComponent<Image>().color = Color.clear; // Mask needs image
            scrollGo.AddComponent<Mask>().showMaskGraphic = false;

            var contentGo = CreateUIObject("ContentParent", scrollGo.transform);
            SetStretch(contentGo);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);

            var gridLayout = contentGo.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(100, 120);
            gridLayout.spacing = new Vector2(8, 8);
            gridLayout.padding = new RectOffset(8, 8, 8, 8);
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;

            var contentSizeFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Wire InventoryView fields
            WireField(inventoryView, "_contentParent", contentGo.transform);
            WireField(inventoryView, "_filterAll", filterAll);
            WireField(inventoryView, "_filterCores", filterCores);
            WireField(inventoryView, "_filterPrisms", filterPrisms);
            WireField(inventoryView, "_filterSails", filterSails);
            WireField(inventoryView, "_filterSatellites", filterSats);

            // ── ItemDetailView ──
            var detailGo = CreateUIObject("ItemDetailView", panelGo.transform);
            SetAnchors(detailGo, new Vector2(0.60f, 0.05f), new Vector2(0.98f, 0.48f));
            var itemDetailView = detailGo.AddComponent<ItemDetailView>();

            // Detail content root (toggled by Clear/ShowItem)
            var detailContent = CreateUIObject("ContentRoot", detailGo.transform);
            SetStretch(detailContent);

            // Detail background
            var detailBg = CreateUIObject("DetailBackground", detailContent.transform);
            SetStretch(detailBg);
            var detailBgImg = detailBg.AddComponent<Image>();
            detailBgImg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            // Vertical layout for detail content
            var detailLayout = CreateUIObject("DetailLayout", detailContent.transform);
            SetAnchors(detailLayout, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
            var vlg = detailLayout.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Detail icon
            var detailIcon = CreateUIObject("Icon", detailLayout.transform);
            var detailIconImg = detailIcon.AddComponent<Image>();
            detailIconImg.color = Color.clear;
            var iconLE = detailIcon.AddComponent<LayoutElement>();
            iconLE.preferredHeight = 64;
            iconLE.preferredWidth = 64;

            // Detail name
            var detailName = CreateUIObject("NameText", detailLayout.transform);
            var detailNameTmp = detailName.AddComponent<TextMeshProUGUI>();
            detailNameTmp.text = "Item Name";
            detailNameTmp.fontSize = 22;
            detailNameTmp.fontStyle = FontStyles.Bold;
            detailNameTmp.color = Color.white;
            var nameLE = detailName.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 30;

            // Detail description
            var detailDesc = CreateUIObject("DescriptionText", detailLayout.transform);
            var detailDescTmp = detailDesc.AddComponent<TextMeshProUGUI>();
            detailDescTmp.text = "Item description goes here...";
            detailDescTmp.fontSize = 14;
            detailDescTmp.color = new Color(0.7f, 0.7f, 0.8f, 1f);
            var descLE = detailDesc.AddComponent<LayoutElement>();
            descLE.preferredHeight = 60;

            // Detail stats
            var detailStats = CreateUIObject("StatsText", detailLayout.transform);
            var detailStatsTmp = detailStats.AddComponent<TextMeshProUGUI>();
            detailStatsTmp.text = "Stats...";
            detailStatsTmp.fontSize = 13;
            detailStatsTmp.color = new Color(0.5f, 0.9f, 0.5f, 1f);
            var statsLE = detailStats.AddComponent<LayoutElement>();
            statsLE.preferredHeight = 80;
            statsLE.flexibleHeight = 1;

            // Action button (Equip/Unequip)
            var actionBtnGo = CreateUIObject("ActionButton", detailLayout.transform);
            var actionBtn = actionBtnGo.AddComponent<Button>();
            var actionBtnImg = actionBtnGo.AddComponent<Image>();
            actionBtnImg.color = new Color(0.2f, 0.5f, 0.8f, 1f);
            var actionBtnLE = actionBtnGo.AddComponent<LayoutElement>();
            actionBtnLE.preferredHeight = 40;

            var actionLabel = CreateUIObject("ActionLabel", actionBtnGo.transform);
            SetStretch(actionLabel);
            var actionLabelTmp = actionLabel.AddComponent<TextMeshProUGUI>();
            actionLabelTmp.text = "EQUIP";
            actionLabelTmp.alignment = TextAlignmentOptions.Center;
            actionLabelTmp.fontSize = 18;
            actionLabelTmp.fontStyle = FontStyles.Bold;
            actionLabelTmp.color = Color.white;
            actionLabelTmp.raycastTarget = false;

            // Wire ItemDetailView fields
            WireField(itemDetailView, "_contentRoot", detailContent);
            WireField(itemDetailView, "_icon", detailIconImg);
            WireField(itemDetailView, "_nameText", detailNameTmp);
            WireField(itemDetailView, "_descriptionText", detailDescTmp);
            WireField(itemDetailView, "_statsText", detailStatsTmp);
            WireField(itemDetailView, "_actionButton", actionBtn);
            WireField(itemDetailView, "_actionButtonLabel", actionLabelTmp);

            // Wire StarChartPanel fields
            WireField(starChartPanel, "_primaryTrackView", primaryTrack);
            WireField(starChartPanel, "_secondaryTrackView", secondaryTrack);
            WireField(starChartPanel, "_inventoryView", inventoryView);
            WireField(starChartPanel, "_itemDetailView", itemDetailView);

            Debug.Log("[UICanvasBuilder] Created StarChartPanel (with tracks, inventory, detail)");
            return starChartPanel;
        }

        private static UIManager BuildUIManagerSection(
            GameObject canvasGo, HeatBarHUD heatBarHud, HealthBarHUD healthBarHud, StarChartPanel starChartPanel)
        {
            var uiManagerGo = CreateUIObject("UIManager", canvasGo.transform);
            SetStretch(uiManagerGo);
            uiManagerGo.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            var uiManager = uiManagerGo.AddComponent<UIManager>();
            var weavingTransition = uiManagerGo.AddComponent<WeavingStateTransition>();

            WireField(uiManager, "_starChartPanel", starChartPanel);
            WireField(uiManager, "_heatBarHUD", heatBarHud);
            WireField(uiManager, "_healthBarHUD", healthBarHud);
            WireField(uiManager, "_weavingTransition", weavingTransition);

            // Auto-find InputActionAsset if available
            var inputAsset = FindInputActionAsset();
            if (inputAsset != null)
                WireField(uiManager, "_inputActions", inputAsset);

            // Auto-find StarChartInventorySO if available
            var inventorySO = FindOrCreateInventorySO();
            if (inventorySO != null)
                WireField(uiManager, "_playerInventory", inventorySO);

            // Wire WeavingStateTransition — camera and ship will be runtime references,
            // but try to auto-find the main camera
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                WireField(weavingTransition, "_mainCamera", mainCam);
                // Try to find a ship transform
                var shipMotor = Object.FindAnyObjectByType<ProjectArk.Ship.ShipMotor>();
                if (shipMotor != null)
                    WireField(weavingTransition, "_shipTransform", shipMotor.transform);
            }

            Debug.Log("[UICanvasBuilder] Created UIManager (+ WeavingStateTransition)");
            return uiManager;
        }

        /// <summary>
        /// Creates a full-screen FadeOverlay Image + DoorTransitionController.
        /// The FadeOverlay is placed as the last sibling so it renders on top of all UI.
        /// </summary>
        private static DoorTransitionController BuildDoorTransitionSection(GameObject canvasGo)
        {
            var fadeGo = CreateUIObject("FadeOverlay", canvasGo.transform);
            SetStretch(fadeGo);
            fadeGo.transform.SetAsLastSibling(); // 确保渲染在最顶层

            var fadeImg = fadeGo.AddComponent<Image>();
            fadeImg.color = new Color(0f, 0f, 0f, 0f); // 完全透明
            fadeImg.raycastTarget = false; // 默认不阻挡点击

            var doorTransition = fadeGo.AddComponent<DoorTransitionController>();
            WireField(doorTransition, "_fadeImage", fadeImg);

            Debug.Log("[UICanvasBuilder] Created FadeOverlay + DoorTransitionController");
            return doorTransition;
        }

        // =====================================================================
        // TrackView Builder
        // =====================================================================

        private static TrackView BuildTrackView(GameObject root, string label)
        {
            var trackView = root.AddComponent<TrackView>();

            // Track background
            var trackBg = CreateUIObject("TrackBackground", root.transform);
            SetStretch(trackBg);
            var trackBgImg = trackBg.AddComponent<Image>();
            trackBgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

            // Track label
            var labelGo = CreateUIObject("TrackLabel", root.transform);
            SetAnchors(labelGo, new Vector2(0.05f, 0.85f), new Vector2(0.6f, 0.98f));
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.fontSize = 18;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.color = Color.white;

            // Selection border
            var borderGo = CreateUIObject("SelectionBorder", root.transform);
            SetStretch(borderGo);
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(0.3f, 0.3f, 0.4f, 0.5f);
            borderImg.type = Image.Type.Sliced;
            borderImg.raycastTarget = false;

            // Select button (covers entire track for easy click)
            var selectBtnGo = CreateUIObject("SelectButton", root.transform);
            SetStretch(selectBtnGo);
            var selectBtn = selectBtnGo.AddComponent<Button>();
            var selectBtnImg = selectBtnGo.AddComponent<Image>();
            selectBtnImg.color = Color.clear; // Invisible clickable area

            // Prism cells row (upper)
            var prismRow = CreateUIObject("PrismRow", root.transform);
            SetAnchors(prismRow, new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.82f));
            var prismHLG = prismRow.AddComponent<HorizontalLayoutGroup>();
            prismHLG.spacing = 6;
            prismHLG.childForceExpandWidth = true;
            prismHLG.childForceExpandHeight = true;

            var prismCells = new SlotCellView[3];
            for (int i = 0; i < 3; i++)
                prismCells[i] = BuildSlotCell($"PrismCell_{i}", prismRow.transform,
                    new Color(0.3f, 0.2f, 0.5f, 0.6f));

            // Core cells row (lower)
            var coreRow = CreateUIObject("CoreRow", root.transform);
            SetAnchors(coreRow, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.42f));
            var coreHLG = coreRow.AddComponent<HorizontalLayoutGroup>();
            coreHLG.spacing = 6;
            coreHLG.childForceExpandWidth = true;
            coreHLG.childForceExpandHeight = true;

            var coreCells = new SlotCellView[3];
            for (int i = 0; i < 3; i++)
                coreCells[i] = BuildSlotCell($"CoreCell_{i}", coreRow.transform,
                    new Color(0.2f, 0.3f, 0.5f, 0.6f));

            // Wire TrackView fields
            WireField(trackView, "_trackLabel", labelTmp);
            WireField(trackView, "_selectButton", selectBtn);
            WireField(trackView, "_selectionBorder", borderImg);
            WireArrayField(trackView, "_prismCells", prismCells);
            WireArrayField(trackView, "_coreCells", coreCells);

            return trackView;
        }

        // =====================================================================
        // SlotCellView Builder
        // =====================================================================

        private static SlotCellView BuildSlotCell(string name, Transform parent, Color bgColor)
        {
            var cellGo = CreateUIObject(name, parent);
            var cellView = cellGo.AddComponent<SlotCellView>();

            // Background
            var bgGo = CreateUIObject("Background", cellGo.transform);
            SetStretch(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = bgColor;

            // Icon
            var iconGo = CreateUIObject("Icon", cellGo.transform);
            SetAnchors(iconGo, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.clear;
            iconImg.enabled = false;
            iconImg.raycastTarget = false;

            // Button
            var btnImg = cellGo.AddComponent<Image>();
            btnImg.color = Color.clear; // Invisible button backing
            var btn = cellGo.AddComponent<Button>();

            // Wire SlotCellView fields
            WireField(cellView, "_backgroundImage", bgImg);
            WireField(cellView, "_iconImage", iconImg);
            WireField(cellView, "_button", btn);

            return cellView;
        }

        // =====================================================================
        // InventoryItemView Prefab Builder
        // =====================================================================

        [MenuItem("ProjectArk/Create InventoryItemView Prefab")]
        public static void CreateInventoryItemViewPrefab()
        {
            const string prefabDir = "Assets/_Prefabs/UI";
            const string prefabPath = prefabDir + "/InventoryItemView.prefab";

            // Check if already exists
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[UICanvasBuilder] InventoryItemView prefab already exists at {prefabPath}");
                return;
            }

            EnsureFolder("Assets", "_Prefabs");
            EnsureFolder("Assets/_Prefabs", "UI");

            var root = new GameObject("InventoryItemView");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(100, 120);
            root.AddComponent<CanvasGroup>(); // Required by InventoryItemView for drag alpha control
            var itemView = root.AddComponent<InventoryItemView>();

            // Selection border (background)
            var selBorder = CreateUIObjectStandalone("SelectionBorder", root.transform);
            SetStretch(selBorder);
            var selBorderImg = selBorder.AddComponent<Image>();
            selBorderImg.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);

            // Icon
            var iconGo = CreateUIObjectStandalone("Icon", root.transform);
            SetAnchors(iconGo, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.95f));
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.clear;
            iconImg.raycastTarget = false;

            // Name label
            var nameGo = CreateUIObjectStandalone("NameLabel", root.transform);
            SetAnchors(nameGo, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.35f));
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "Item";
            nameTmp.fontSize = 11;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = Color.white;
            nameTmp.raycastTarget = false;

            // Slot size label
            var sizeGo = CreateUIObjectStandalone("SlotSizeLabel", root.transform);
            SetAnchors(sizeGo, new Vector2(0.7f, 0.0f), new Vector2(1f, 0.15f));
            var sizeTmp = sizeGo.AddComponent<TextMeshProUGUI>();
            sizeTmp.text = "[1]";
            sizeTmp.fontSize = 10;
            sizeTmp.alignment = TextAlignmentOptions.Center;
            sizeTmp.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            sizeTmp.raycastTarget = false;

            // Equipped badge
            var badgeGo = CreateUIObjectStandalone("EquippedBadge", root.transform);
            SetAnchors(badgeGo, new Vector2(0f, 0f), new Vector2(0.3f, 0.15f));
            var badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.color = new Color(0.3f, 0.8f, 0.4f, 1f);
            badgeImg.enabled = false;
            badgeImg.raycastTarget = false;

            // Button
            var btnImg = root.AddComponent<Image>();
            btnImg.color = Color.clear;
            var btn = root.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            // Wire InventoryItemView fields
            WireField(itemView, "_iconImage", iconImg);
            WireField(itemView, "_nameLabel", nameTmp);
            WireField(itemView, "_slotSizeLabel", sizeTmp);
            WireField(itemView, "_button", btn);
            WireField(itemView, "_equippedBadge", badgeImg);
            WireField(itemView, "_selectionBorder", selBorderImg);

            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[UICanvasBuilder] ✅ Created InventoryItemView prefab at {prefabPath}");

            // If there's already an InventoryView in the scene, wire the prefab reference
            var sceneInventoryView = Object.FindAnyObjectByType<InventoryView>();
            if (sceneInventoryView != null)
            {
                WireField(sceneInventoryView, "_itemPrefab", prefab.GetComponent<InventoryItemView>());
                Debug.Log("[UICanvasBuilder] Auto-wired _itemPrefab on scene InventoryView");
            }
        }

        // =====================================================================
        // Helpers — UI Construction
        // =====================================================================

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;
            return go;
        }

        /// <summary>
        /// Same as CreateUIObject but for standalone objects (not parented to Canvas during build).
        /// </summary>
        private static GameObject CreateUIObjectStandalone(string name, Transform parent)
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

        private static Button CreateButton(string name, Transform parent, string label)
        {
            var btnGo = CreateUIObject(name, parent);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.15f, 0.15f, 0.25f, 0.9f);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            var btnLabel = CreateUIObject("Label", btnGo.transform);
            SetStretch(btnLabel);
            var btnTmp = btnLabel.AddComponent<TextMeshProUGUI>();
            btnTmp.text = label;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.fontSize = 12;
            btnTmp.color = Color.white;
            btnTmp.raycastTarget = false;

            var le = btnGo.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;

            return btn;
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
                Debug.LogWarning($"[UICanvasBuilder] Field '{fieldName}' not found on {target.GetType().Name}");
            }
        }

        private static void WireGradient(Component target, string fieldName, Gradient gradient)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.gradientValue = gradient;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"[UICanvasBuilder] Gradient field '{fieldName}' not found on {target.GetType().Name}");
            }
        }

        private static void WireArrayField<T>(Component target, string fieldName, T[] items)
            where T : Component
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null && prop.isArray)
            {
                prop.arraySize = items.Length;
                for (int i = 0; i < items.Length; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"[UICanvasBuilder] Array field '{fieldName}' not found on {target.GetType().Name}");
            }
        }

        // =====================================================================
        // Helpers — Gradient Presets
        // =====================================================================

        /// <summary>
        /// Heat gradient: green(cool) -> yellow(warm) -> red(hot).
        /// Used by HeatBarHUD to visualize current heat level.
        /// </summary>
        private static Gradient CreateHeatGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.2f, 0.8f, 0.3f), 0f),    // Green at 0%
                    new GradientColorKey(new Color(1f, 0.85f, 0.1f), 0.5f),    // Yellow at 50%
                    new GradientColorKey(new Color(1f, 0.2f, 0.1f), 1f)        // Red at 100%
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            return gradient;
        }

        /// <summary>
        /// Health gradient: red(critical) -> yellow(wounded) -> green(healthy).
        /// Used by HealthBarHUD to visualize remaining HP.
        /// </summary>
        private static Gradient CreateHealthGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.9f, 0.1f, 0.1f), 0f),    // Red at 0%
                    new GradientColorKey(new Color(1f, 0.85f, 0.1f), 0.4f),    // Yellow at 40%
                    new GradientColorKey(new Color(0.2f, 0.85f, 0.3f), 1f)     // Green at 100%
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            return gradient;
        }

        // =====================================================================
        // Helpers — Asset Lookup
        // =====================================================================

        private static UnityEngine.InputSystem.InputActionAsset FindInputActionAsset()
        {
            var guids = AssetDatabase.FindAssets("t:InputActionAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(path);
                if (asset != null)
                {
                    Debug.Log($"[UICanvasBuilder] Auto-found InputActionAsset: {path}");
                    return asset;
                }
            }
            Debug.LogWarning("[UICanvasBuilder] No InputActionAsset found. Please assign manually on UIManager.");
            return null;
        }

        private static StarChartInventorySO FindOrCreateInventorySO()
        {
            // Try to find existing
            var guids = AssetDatabase.FindAssets("t:StarChartInventorySO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<StarChartInventorySO>(path);
                if (asset != null)
                {
                    Debug.Log($"[UICanvasBuilder] Auto-found StarChartInventorySO: {path}");
                    return asset;
                }
            }

            // Create one
            const string soDir = "Assets/_Data/StarChart";
            const string soPath = soDir + "/PlayerInventory.asset";

            EnsureFolder("Assets", "_Data");
            EnsureFolder("Assets/_Data", "StarChart");

            var so = ScriptableObject.CreateInstance<StarChartInventorySO>();
            AssetDatabase.CreateAsset(so, soPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UICanvasBuilder] Created StarChartInventorySO: {soPath}");

            // Auto-populate with existing StarCoreSO/PrismSO/LightSailSO assets
            PopulateInventory(so);

            return so;
        }

        private static void PopulateInventory(StarChartInventorySO inventory)
        {
            var serialized = new SerializedObject(inventory);

            // Find and add all StarCoreSO
            AddAssetsToList<Combat.StarCoreSO>(serialized, "_ownedItems", "StarCoreSO");
            // Find and add all PrismSO
            AddAssetsToList<Combat.PrismSO>(serialized, "_ownedItems", "PrismSO");
            // Find and add all LightSailSO
            AddAssetsToList<Combat.LightSailSO>(serialized, "_ownedItems", "LightSailSO");
            // Find and add all SatelliteSO
            AddAssetsToList<Combat.SatelliteSO>(serialized, "_ownedItems", "SatelliteSO");

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private static void AddAssetsToList<T>(SerializedObject so, string listField, string typeName)
            where T : ScriptableObject
        {
            var prop = so.FindProperty(listField);
            if (prop == null || !prop.isArray)
            {
                Debug.LogWarning($"[UICanvasBuilder] Cannot find array field '{listField}' on StarChartInventorySO");
                return;
            }

            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    int index = prop.arraySize;
                    prop.arraySize = index + 1;
                    prop.GetArrayElementAtIndex(index).objectReferenceValue = asset;
                    Debug.Log($"[UICanvasBuilder] Added {typeName} to inventory: {asset.name}");
                }
            }
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
