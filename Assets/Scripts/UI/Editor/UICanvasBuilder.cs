
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
            Debug.Log("  │  ├─ PanelBackground (#0b1120)");
            Debug.Log("  │  ├─ CornerBrackets (4 L-shaped cyan #22d3ee decorations)");
            Debug.Log("  │  ├─ Header (CANARY — STAR CHART CALIBRATION SYSTEM + SYSTEM ONLINE badge + close)");
            Debug.Log("  │  ├─ LoadoutSection (55%)");
            Debug.Log("  │  │  ├─ GatlingCol (▲ DrumCounter ▼ LOADOUT label, LoadoutSwitcher)");
            Debug.Log("  │  │  └─ LoadoutCard (CardHeader + PrimaryTrackView + SecondaryTrackView)");
            Debug.Log("  │  │     ├─ PrimaryTrackView   (SAIL 2×2 | PRISM 2×2 | CORE 2×2 | SAT 2×2)");
            Debug.Log("  │  │     └─ SecondaryTrackView (SAIL 2×2 | PRISM 2×2 | CORE 2×2 | SAT 2×2)");
            Debug.Log("  │  ├─ SectionDivider");
            Debug.Log("  │  ├─ InventorySection (40%)");
            Debug.Log("  │  │  ├─ InventoryView (filters + 64x64 grid)");
            Debug.Log("  │  │  └─ ItemDetailView (icon + name + desc + stats + button)");
            Debug.Log("  │  ├─ StatusBar (22px, EQUIPPED X/10 · INVENTORY X ITEMS · DRAG TO EQUIP)");
            Debug.Log("  │  └─ DragDropManager (ghost image)");
            Debug.Log("  ├─ UIManager (+ WeavingStateTransition)");
            Debug.Log("  └─ DoorTransitionController (FadeOverlay)");
            Debug.Log("");
            // ── Step 7: Auto-wire _itemPrefab if InventoryItemView prefab already exists ──
            const string itemPrefabPath = "Assets/_Prefabs/UI/InventoryItemView.prefab";
            var existingItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(itemPrefabPath);
            if (existingItemPrefab != null)
            {
                var sceneInventoryView = Object.FindAnyObjectByType<InventoryView>(FindObjectsInactive.Include);
                if (sceneInventoryView != null)
                {
                    WireField(sceneInventoryView, "_itemPrefab", existingItemPrefab.GetComponent<InventoryItemView>());
                    Debug.Log("[UICanvasBuilder] ✅ Auto-wired _itemPrefab on InventoryView");
                }
            }
            else
            {
                Debug.Log("[UICanvasBuilder] ℹ️ InventoryItemView prefab not found — run 'ProjectArk/Create InventoryItemView Prefab' to create it");
            }

            Debug.Log("[UICanvasBuilder] ⚠️ Manual steps remaining:");
            Debug.Log("  1. Assign _inputActions on UIManager if not auto-found");
            Debug.Log("  2. Assign _playerInventory on UIManager if not auto-found");
            Debug.Log("  3. (Auto) InventoryItemView prefab wired if it existed — otherwise run Create InventoryItemView Prefab");
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
            // Full-screen stretch — panel fills the entire Canvas
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin        = Vector2.zero;
            panelRect.anchorMax        = Vector2.one;
            panelRect.pivot            = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta        = Vector2.zero;
            var starChartPanel = panelGo.AddComponent<StarChartPanel>();
            panelGo.AddComponent<UIParallaxEffect>();

            // ── Panel background (#0b1120) ──
            var panelBg = CreateUIObject("PanelBackground", panelGo.transform);
            SetStretch(panelBg);
            var panelBgImg = panelBg.AddComponent<Image>();
            panelBgImg.color = StarChartTheme.BgPanel;
            panelBgImg.raycastTarget = true;

            // ── Corner bracket decorations (4 L-shaped cyan corners) ──
            BuildCornerBrackets(panelGo.transform);

            // ── Header bar (top 6%, 42px / 700px) ──
            var headerGo = CreateUIObject("Header", panelGo.transform);
            SetAnchors(headerGo, new Vector2(0f, 0.940f), new Vector2(1f, 1f));
            BuildHeader(headerGo.transform, starChartPanel);

            // ── Divider line between header and content ──
            var dividerH = CreateUIObject("DividerHorizontal", panelGo.transform);
            SetAnchors(dividerH, new Vector2(0.01f, 0.938f), new Vector2(0.99f, 0.940f));
            var dividerHImg = dividerH.AddComponent<Image>();
            dividerHImg.color = StarChartTheme.Border;
            dividerHImg.raycastTarget = false;

            // LoadoutSection (flex:1, ~45.4%): GatlingCol (left 6.5%) + LoadoutCard (right 93.5%) ──
            // HTML prototype: loadout-section flex:1, same height as inventory-section
            // 700px total - 42px header - 22px statusbar - 1px divider = 635px / 2 = 317.5px each
            // LoadoutSection: y 0.487 → 0.938 (317.5/700 ≈ 45.4%)
            var loadoutSection = CreateUIObject("LoadoutSection", panelGo.transform);
            SetAnchors(loadoutSection, new Vector2(0f, 0.487f), new Vector2(1f, 0.938f));
            // GatlingCol (56px wide, left side)
            var gatlingColGo = CreateUIObject("GatlingCol", loadoutSection.transform);
            SetAnchors(gatlingColGo, new Vector2(0f, 0f), new Vector2(0.065f, 1f));
            var loadoutSwitcher = BuildGatlingCol(gatlingColGo);

            // Vertical divider between GatlingCol and LoadoutCard
            var dividerGatling = CreateUIObject("DividerGatling", loadoutSection.transform);
            SetAnchors(dividerGatling, new Vector2(0.066f, 0.02f), new Vector2(0.068f, 0.98f));
            var dividerGatlingImg = dividerGatling.AddComponent<Image>();
            dividerGatlingImg.color = StarChartTheme.Border;
            dividerGatlingImg.raycastTarget = false;

            // LoadoutCard (right side)
            var loadoutCardGo = CreateUIObject("LoadoutCard", loadoutSection.transform);
            SetAnchors(loadoutCardGo, new Vector2(0.07f, 0f), new Vector2(1f, 1f));
            var loadoutCardRect = loadoutCardGo.GetComponent<RectTransform>();

            // LoadoutCard background (#0f1828)
            var cardBg = CreateUIObject("CardBackground", loadoutCardGo.transform);
            SetStretch(cardBg);
            var cardBgImg = cardBg.AddComponent<Image>();
            cardBgImg.color = StarChartTheme.BgTrack;
            cardBgImg.raycastTarget = false;

            // LoadoutCard header (◈ icon + name + subtitle)
            // HTML: lc-header ~28px / LoadoutCard ~317px ≈ 8.8%
            var cardHeader = CreateUIObject("CardHeader", loadoutCardGo.transform);
            SetAnchors(cardHeader, new Vector2(0f, 0.91f), new Vector2(1f, 1f));
            BuildLoadoutCardHeader(cardHeader.transform);

            // Track blocks area (PRIMARY on top, SECONDARY on bottom — matches HTML prototype)
            var tracksArea = CreateUIObject("TracksArea", loadoutCardGo.transform);
            SetAnchors(tracksArea, new Vector2(0f, 0f), new Vector2(1f, 0.90f));

            // PrimaryTrackView (top half: y 0.52 → 1.0)
            var primaryTrackGo = CreateUIObject("PrimaryTrackView", tracksArea.transform);
            SetAnchors(primaryTrackGo, new Vector2(0f, 0.52f), new Vector2(1f, 1f));
            var primaryTrack = BuildTrackView(primaryTrackGo, "PRIMARY");

            // Horizontal divider between Primary and Secondary
            var dividerTrack = CreateUIObject("DividerHorizontal", tracksArea.transform);
            SetAnchors(dividerTrack, new Vector2(0.01f, 0.505f), new Vector2(0.99f, 0.515f));
            var dividerTrackImg = dividerTrack.AddComponent<Image>();
            dividerTrackImg.color = StarChartTheme.Border;
            dividerTrackImg.raycastTarget = false;

            // SecondaryTrackView (bottom half: y 0.0 → 0.49)
            var secondaryTrackGo = CreateUIObject("SecondaryTrackView", tracksArea.transform);
            SetAnchors(secondaryTrackGo, new Vector2(0f, 0f), new Vector2(1f, 0.49f));
            var secondaryTrack = BuildTrackView(secondaryTrackGo, "SECONDARY");

            // Wire LoadoutSwitcher → LoadoutCard
            WireField(loadoutSwitcher, "_loadoutCard", loadoutCardRect);

            // ── SectionDivider (1px between LoadoutSection and InventorySection) ──
            var dividerMid = CreateUIObject("SectionDivider", panelGo.transform);
            SetAnchors(dividerMid, new Vector2(0.01f, 0.485f), new Vector2(0.99f, 0.487f));
            var dividerMidImg = dividerMid.AddComponent<Image>();
            dividerMidImg.color = StarChartTheme.Border;
            dividerMidImg.raycastTarget = false;

            // ── InventorySection (flex:1, ~45.4%): full-width inventory (no ItemDetailView split) ──
            // HTML prototype: inventory-section flex:1, only inv-bar + inventory-area (no right panel)
            // InventorySection: y 0.031 → 0.485 (317.5/700 ≈ 45.4%)
            var lowerSection = CreateUIObject("InventorySection", panelGo.transform);
            SetAnchors(lowerSection, new Vector2(0f, 0.031f), new Vector2(1f, 0.485f));

            // InventoryView (full width — HTML prototype has no ItemDetailView split)
            var inventoryGo = CreateUIObject("InventoryView", lowerSection.transform);
            SetAnchors(inventoryGo, new Vector2(0.01f, 0f), new Vector2(0.99f, 1f));
            var inventoryView = inventoryGo.AddComponent<InventoryView>();

            // Inventory background
            var invBg = CreateUIObject("InvBackground", inventoryGo.transform);
            SetStretch(invBg);
            var invBgImg = invBg.AddComponent<Image>();
            invBgImg.color = new Color(0.07f, 0.09f, 0.12f, 0.90f);

            // Filter buttons bar (inv-bar: 34px / InventorySection ~317px ≈ 10.7%, use 12%)
            var filterBar = CreateUIObject("FilterBar", inventoryGo.transform);
            SetAnchors(filterBar, new Vector2(0f, 0.88f), new Vector2(1f, 1f));
            var filterHLG = filterBar.AddComponent<HorizontalLayoutGroup>();
            filterHLG.spacing = 4;
            filterHLG.childForceExpandWidth = true;
            filterHLG.childForceExpandHeight = true;

            var filterAll   = CreateButton("FilterAll",        filterBar.transform, "ALL");
            var filterSails = CreateButton("FilterSails",       filterBar.transform, "SAILS");
            var filterPrisms = CreateButton("FilterPrisms",     filterBar.transform, "PRISMS");
            var filterCores = CreateButton("FilterCores",       filterBar.transform, "CORES");
            var filterSats  = CreateButton("FilterSatellites",  filterBar.transform, "SATS");

            // Inventory scroll area
            var scrollGo = CreateUIObject("ScrollArea", inventoryGo.transform);
            SetAnchors(scrollGo, new Vector2(0f, 0f), new Vector2(1f, 0.86f));
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            // IMPORTANT: Mask requires Image.color.a > 0 to clip children correctly.
            // uGUI Mask uses the Image alpha channel as the stencil mask — alpha=0 clips everything.
            // Use alpha=1 with a fully transparent-looking color, and hide via showMaskGraphic=false.
            var scrollMaskImg = scrollGo.AddComponent<Image>();
            scrollMaskImg.color = new Color(0f, 0f, 0f, 1f); // alpha MUST be 1 for Mask to work
            var scrollMask = scrollGo.AddComponent<Mask>();
            scrollMask.showMaskGraphic = false; // hides the black background visually

            var contentGo = CreateUIObject("ContentParent", scrollGo.transform);
            SetStretch(contentGo);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);

            var gridLayout = contentGo.AddComponent<GridLayoutGroup>();
            // HTML prototype: --inv-slot-size: 44px, --slot-gap: 2px, flex-wrap grid
            // Full-width InventoryView ~960px, (44+2)*12 = 552px fits comfortably
            gridLayout.cellSize = new Vector2(44, 44);
            gridLayout.spacing = new Vector2(2, 2);
            gridLayout.padding = new RectOffset(6, 6, 6, 6);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 12; // 12 columns, (44+2)*12+12 = 564px

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

            // ItemDetailView — hidden by default (HTML prototype has no right-panel split)
            // Kept in hierarchy for runtime use (e.g. tooltip/detail popup), but not visible in layout
            var detailGo = CreateUIObject("ItemDetailView", lowerSection.transform);
            SetAnchors(detailGo, new Vector2(0.62f, 0f), new Vector2(0.99f, 1f));
            detailGo.SetActive(false);
            var itemDetailView = detailGo.AddComponent<ItemDetailView>();

            var detailContent = CreateUIObject("ContentRoot", detailGo.transform);
            SetStretch(detailContent);

            var detailBg = CreateUIObject("DetailBackground", detailContent.transform);
            SetStretch(detailBg);
            var detailBgImg = detailBg.AddComponent<Image>();
            detailBgImg.color = new Color(0.07f, 0.09f, 0.12f, 0.90f);

            var detailLayout = CreateUIObject("DetailLayout", detailContent.transform);
            SetAnchors(detailLayout, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
            var vlg = detailLayout.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var detailIcon = CreateUIObject("Icon", detailLayout.transform);
            var detailIconImg = detailIcon.AddComponent<Image>();
            detailIconImg.color = Color.clear;
            var iconLE = detailIcon.AddComponent<LayoutElement>();
            iconLE.preferredHeight = 64;
            iconLE.preferredWidth = 64;

            var detailName = CreateUIObject("NameText", detailLayout.transform);
            var detailNameTmp = detailName.AddComponent<TextMeshProUGUI>();
            detailNameTmp.text = "Item Name";
            detailNameTmp.fontSize = 22;
            detailNameTmp.fontStyle = FontStyles.Bold;
            detailNameTmp.color = Color.white;
            var nameLE = detailName.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 30;

            // Type label (e.g. "◈  CORE" colored by type)
            var detailTypeLabel = CreateUIObject("TypeLabel", detailLayout.transform);
            var detailTypeLabelTmp = detailTypeLabel.AddComponent<TextMeshProUGUI>();
            detailTypeLabelTmp.text = "◈  CORE";
            detailTypeLabelTmp.fontSize = 13;
            detailTypeLabelTmp.fontStyle = FontStyles.Bold;
            detailTypeLabelTmp.color = StarChartTheme.CoreColor;
            detailTypeLabelTmp.raycastTarget = false;
            var typeLabelLE = detailTypeLabel.AddComponent<LayoutElement>();
            typeLabelLE.preferredHeight = 22;
            detailTypeLabel.SetActive(false); // hidden until ShowItem is called

            var detailDesc = CreateUIObject("DescriptionText", detailLayout.transform);
            var detailDescTmp = detailDesc.AddComponent<TextMeshProUGUI>();
            detailDescTmp.text = "Item description goes here...";
            detailDescTmp.fontSize = 14;
            detailDescTmp.color = new Color(0.7f, 0.7f, 0.8f, 1f);
            var descLE = detailDesc.AddComponent<LayoutElement>();
            descLE.preferredHeight = 60;

            var detailStats = CreateUIObject("StatsText", detailLayout.transform);
            var detailStatsTmp = detailStats.AddComponent<TextMeshProUGUI>();
            detailStatsTmp.text = "Stats...";
            detailStatsTmp.fontSize = 13;
            detailStatsTmp.color = StarChartTheme.CyanDim;
            var statsLE = detailStats.AddComponent<LayoutElement>();
            statsLE.preferredHeight = 80;
            statsLE.flexibleHeight = 1;

            // Equipped status label ("● EQUIPPED" / "○ NOT EQUIPPED")
            var detailEquippedLabel = CreateUIObject("EquippedLabel", detailLayout.transform);
            var detailEquippedLabelTmp = detailEquippedLabel.AddComponent<TextMeshProUGUI>();
            detailEquippedLabelTmp.text = "○ NOT EQUIPPED";
            detailEquippedLabelTmp.fontSize = 13;
            detailEquippedLabelTmp.fontStyle = FontStyles.Bold;
            detailEquippedLabelTmp.color = new Color(1f, 1f, 1f, 0.35f);
            detailEquippedLabelTmp.raycastTarget = false;
            var equippedLabelLE = detailEquippedLabel.AddComponent<LayoutElement>();
            equippedLabelLE.preferredHeight = 22;
            detailEquippedLabel.SetActive(false); // hidden until ShowItem is called

            var actionBtnGo = CreateUIObject("ActionButton", detailLayout.transform);
            var actionBtn = actionBtnGo.AddComponent<Button>();
            var actionBtnImg = actionBtnGo.AddComponent<Image>();
            actionBtnImg.color = new Color(0f, StarChartTheme.Cyan.g * 0.4f, StarChartTheme.Cyan.b * 0.5f, 1f);
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

            WireField(itemDetailView, "_contentRoot",      detailContent);
            WireField(itemDetailView, "_icon",             detailIconImg);
            WireField(itemDetailView, "_nameText",         detailNameTmp);
            WireField(itemDetailView, "_typeLabel",        detailTypeLabelTmp);
            WireField(itemDetailView, "_descriptionText",  detailDescTmp);
            WireField(itemDetailView, "_statsText",        detailStatsTmp);
            WireField(itemDetailView, "_equippedLabel",    detailEquippedLabelTmp);
            WireField(itemDetailView, "_actionButton",     actionBtn);
            WireField(itemDetailView, "_actionButtonLabel", actionLabelTmp);

            // ── Status Bar (bottom 3.1%, 22px / 700px) ──
            var statusBarGo = CreateUIObject("StatusBar", panelGo.transform);
            SetAnchors(statusBarGo, new Vector2(0f, 0f), new Vector2(1f, 0.031f));
            var statusBarView = statusBarGo.AddComponent<StatusBarView>();

            var statusBg = CreateUIObject("StatusBackground", statusBarGo.transform);
            SetStretch(statusBg);
            var statusBgImg = statusBg.AddComponent<Image>();
            statusBgImg.color = new Color(0.04f, 0.05f, 0.07f, 0.95f);
            statusBgImg.raycastTarget = false;

            var statusLabel = CreateUIObject("StatusLabel", statusBarGo.transform);
            SetAnchors(statusLabel, new Vector2(0.02f, 0f), new Vector2(0.98f, 1f));
            var statusTmp = statusLabel.AddComponent<TextMeshProUGUI>();
            statusTmp.text = "EQUIPPED 0/10  ·  INVENTORY 0 ITEMS  ·  HOVER TO INSPECT  ·  DRAG ITEM TO SLOT";
            statusTmp.fontSize = 12;
            statusTmp.alignment = TextAlignmentOptions.MidlineLeft;
            statusTmp.color = StarChartTheme.StatusIdle;
            statusTmp.raycastTarget = false;

            WireField(statusBarView, "_label", statusTmp);

            // ── DragDropManager ──
            var dragDropGo = CreateUIObject("DragDropManager", panelGo.transform);
            SetStretch(dragDropGo);
            var dragDropMgr = dragDropGo.AddComponent<DragDropManager>();

            // DragGhostView — rebuilt as a proper component
            var ghostGo = CreateUIObject("DragGhost", dragDropGo.transform);
            SetAnchors(ghostGo, Vector2.zero, Vector2.zero);
            var ghostRect = ghostGo.GetComponent<RectTransform>();
            ghostRect.sizeDelta = new Vector2(80, 80);
            ghostGo.AddComponent<CanvasGroup>(); // required by DragGhostView
            var ghostView = ghostGo.AddComponent<DragGhostView>();

            // Ghost icon
            var ghostIconGo = CreateUIObject("GhostIcon", ghostGo.transform);
            SetAnchors(ghostIconGo, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));
            var ghostIconImg = ghostIconGo.AddComponent<Image>();
            ghostIconImg.color = new Color(1f, 1f, 1f, 0.7f);
            ghostIconImg.raycastTarget = false;

            // Ghost border (colored by drop state)
            var ghostBorderGo = CreateUIObject("GhostBorder", ghostGo.transform);
            SetStretch(ghostBorderGo);
            var ghostBorderImg = ghostBorderGo.AddComponent<Image>();
            ghostBorderImg.color = Color.clear;
            ghostBorderImg.type = Image.Type.Sliced;
            ghostBorderImg.raycastTarget = false;

            // Replace hint label
            var replaceHintGo = CreateUIObject("ReplaceHint", ghostGo.transform);
            SetAnchors(replaceHintGo, new Vector2(0f, -0.4f), new Vector2(1f, 0f));
            var replaceHintTmp = replaceHintGo.AddComponent<TextMeshProUGUI>();
            replaceHintTmp.text = "↺ REPLACE";
            replaceHintTmp.fontSize = 10;
            replaceHintTmp.alignment = TextAlignmentOptions.Center;
            replaceHintTmp.color = StarChartTheme.HighlightReplace;
            replaceHintTmp.raycastTarget = false;
            replaceHintGo.SetActive(false);

            ghostGo.SetActive(false);

            WireField(ghostView, "_iconImage",        ghostIconImg);
            WireField(ghostView, "_borderImage",      ghostBorderImg);
            WireField(ghostView, "_replaceHintLabel", replaceHintTmp);
            WireField(dragDropMgr, "_ghostView",   ghostView);
            WireField(dragDropMgr, "_rootCanvas",  canvasGo.GetComponent<Canvas>());

            // ── Panel CanvasGroup (for open/close animation) ──
            var panelCG = panelGo.AddComponent<CanvasGroup>();
            panelCG.alpha = 0f;
            panelCG.interactable = false;
            panelCG.blocksRaycasts = false;

            // ── Wire StarChartPanel fields ──
            WireField(starChartPanel, "_primaryTrackView",   primaryTrack);
            WireField(starChartPanel, "_secondaryTrackView", secondaryTrack);
            WireField(starChartPanel, "_inventoryView",      inventoryView);
            WireField(starChartPanel, "_itemDetailView",     itemDetailView);
            WireField(starChartPanel, "_dragDropManager",    dragDropMgr);
            WireField(starChartPanel, "_statusBar",          statusBarView);
            WireField(starChartPanel, "_panelCanvasGroup",   panelCG);
            WireField(starChartPanel, "_loadoutSwitcher",    loadoutSwitcher);
            WireField(starChartPanel, "_loadoutCard",        loadoutCardRect);

            Debug.Log("[UICanvasBuilder] Created StarChartPanel (header + GatlingCol + LoadoutCard + inventory + detail + statusbar)");
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

        /// <summary>
        /// Build 4 L-shaped corner bracket decorations on the panel.
        /// </summary>
        private static void BuildCornerBrackets(Transform parent)
        {
            // Each bracket is a thin L-shape approximated by two overlapping Images
            // We use a single Image per corner with a small fixed size
            float size = 20f;
            float thick = 2f;

            // Bottom-Left
            BuildBracket("BracketBL", parent, new Vector2(0f, 0f), new Vector2(0f, 0f), size, thick);
            // Bottom-Right
            BuildBracket("BracketBR", parent, new Vector2(1f, 0f), new Vector2(1f, 0f), size, thick);
            // Top-Left
            BuildBracket("BracketTL", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), size, thick);
            // Top-Right
            BuildBracket("BracketTR", parent, new Vector2(1f, 1f), new Vector2(1f, 1f), size, thick);
        }

        private static void BuildBracket(string name, Transform parent,
            Vector2 anchor, Vector2 pivot, float size, float thick)
        {
            var go = CreateUIObject(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = Vector2.zero;

            // Horizontal bar
            var hBar = CreateUIObject("H", go.transform);
            var hRect = hBar.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, pivot.y > 0.5f ? 1f - thick / size : 0f);
            hRect.anchorMax = new Vector2(1f, pivot.y > 0.5f ? 1f : thick / size);
            hRect.offsetMin = Vector2.zero;
            hRect.offsetMax = Vector2.zero;
            var hImg = hBar.AddComponent<Image>();
            hImg.color = StarChartTheme.CornerBracket;
            hImg.raycastTarget = false;

            // Vertical bar
            var vBar = CreateUIObject("V", go.transform);
            var vRect = vBar.GetComponent<RectTransform>();
            vRect.anchorMin = new Vector2(pivot.x > 0.5f ? 1f - thick / size : 0f, 0f);
            vRect.anchorMax = new Vector2(pivot.x > 0.5f ? 1f : thick / size, 1f);
            vRect.offsetMin = Vector2.zero;
            vRect.offsetMax = Vector2.zero;
            var vImg = vBar.AddComponent<Image>();
            vImg.color = StarChartTheme.CornerBracket;
            vImg.raycastTarget = false;
        }

        /// <summary>
        /// Build the header bar: title + status dot + close button.
        /// </summary>
        private static void BuildHeader(Transform parent, StarChartPanel panel)
        {
            // Background
            var headerBg = CreateUIObject("HeaderBg", parent);
            SetStretch(headerBg);
            var headerBgImg = headerBg.AddComponent<Image>();
            headerBgImg.color = new Color(0.04f, 0.06f, 0.09f, 0.95f);
            headerBgImg.raycastTarget = false;

            // Status pulse dot (cyan)
            var dotGo = CreateUIObject("StatusDot", parent);
            var dotRect = dotGo.GetComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0.01f, 0.25f);
            dotRect.anchorMax = new Vector2(0.01f, 0.75f);
            dotRect.anchoredPosition = Vector2.zero;
            dotRect.sizeDelta = new Vector2(8f, 0f);
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.color = StarChartTheme.Cyan;
            dotImg.raycastTarget = false;
            dotGo.AddComponent<StatusDotPulse>(); // looping alpha pulse

            // Title: "CANARY — STAR CHART CALIBRATION SYSTEM"
            var titleGo = CreateUIObject("Title", parent);
            SetAnchors(titleGo, new Vector2(0.025f, 0f), new Vector2(0.65f, 1f));
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "CANARY  —  STAR CHART CALIBRATION SYSTEM";
            titleTmp.fontSize = 13;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = StarChartTheme.Cyan;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.raycastTarget = false;

            // "SYSTEM ONLINE" badge
            var badgeGo = CreateUIObject("SystemOnlineBadge", parent);
            SetAnchors(badgeGo, new Vector2(0.66f, 0.15f), new Vector2(0.88f, 0.85f));
            var badgeBgImg = badgeGo.AddComponent<Image>();
            badgeBgImg.color = new Color(StarChartTheme.Cyan.r * 0.15f, StarChartTheme.Cyan.g * 0.15f, StarChartTheme.Cyan.b * 0.15f, 0.9f);
            badgeBgImg.raycastTarget = false;

            var badgeLabelGo = CreateUIObject("BadgeLabel", badgeGo.transform);
            SetStretch(badgeLabelGo);
            var badgeTmp = badgeLabelGo.AddComponent<TextMeshProUGUI>();
            badgeTmp.text = "● SYSTEM ONLINE";
            badgeTmp.fontSize = 9;
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.color = StarChartTheme.StatusGreen;
            badgeTmp.raycastTarget = false;

            // Close button [×]
            var closeBtnGo = CreateUIObject("CloseButton", parent);
            SetAnchors(closeBtnGo, new Vector2(0.94f, 0.1f), new Vector2(0.99f, 0.9f));
            var closeBtnImg = closeBtnGo.AddComponent<Image>();
            closeBtnImg.color = new Color(0.6f, 0.15f, 0.15f, 0.8f);
            var closeBtn = closeBtnGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeBtnImg;

            var closeLabelGo = CreateUIObject("CloseLabel", closeBtnGo.transform);
            SetStretch(closeLabelGo);
            var closeTmp = closeLabelGo.AddComponent<TextMeshProUGUI>();
            closeTmp.text = "×";
            closeTmp.fontSize = 18;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color = Color.white;
            closeTmp.raycastTarget = false;

            // Wire close button to panel.Close()
            closeBtn.onClick.AddListener(() => panel.Close());
        }

        private static TrackView BuildTrackView(GameObject root, string label)
        {
            var trackView = root.AddComponent<TrackView>();

            // Track background (#0f1828)
            var trackBg = CreateUIObject("TrackBackground", root.transform);
            SetStretch(trackBg);
            var trackBgImg = trackBg.AddComponent<Image>();
            trackBgImg.color = StarChartTheme.BgTrack;

            // Selection border (full track)
            var borderGo = CreateUIObject("SelectionBorder", root.transform);
            SetStretch(borderGo);
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = StarChartTheme.Border;
            borderImg.type = Image.Type.Sliced;
            borderImg.raycastTarget = false;

            // Select button (covers entire track for easy click)
            var selectBtnGo = CreateUIObject("SelectButton", root.transform);
            SetStretch(selectBtnGo);
            var selectBtn = selectBtnGo.AddComponent<Button>();
            var selectBtnImg = selectBtnGo.AddComponent<Image>();
            selectBtnImg.color = Color.clear;

            // ── Left label column (matches HTML .track-block-label: 64px fixed width) ──
            // LoadoutCard width ≈ 980 * 0.93 = 911px, 64/911 ≈ 7%
            // Shows "PRIMARY [LMB]" or "SECONDARY [RMB]" vertically centered
            var labelGo = CreateUIObject("TrackLabel", root.transform);
            SetAnchors(labelGo, new Vector2(0f, 0f), new Vector2(0.07f, 1f));
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = label == "PRIMARY" ? "PRIMARY\n<size=80%>[LMB]</size>" : "SECONDARY\n<size=80%>[RMB]</size>";
            labelTmp.fontSize = 9;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.color = StarChartTheme.Cyan;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.raycastTarget = false;

            // Vertical divider between label column and type columns
            var dividerLabel = CreateUIObject("DividerLabel", root.transform);
            SetAnchors(dividerLabel, new Vector2(0.069f, 0.05f), new Vector2(0.071f, 0.95f));
            var dividerLabelImg = dividerLabel.AddComponent<Image>();
            dividerLabelImg.color = StarChartTheme.Border;
            dividerLabelImg.raycastTarget = false;

            // ── 4-column layout: SAIL | PRISM | CORE | SAT (right 93%) ──────────────
            // TrackLabel occupies 0-7%, columns start at x=0.08
            // Each column occupies ~22.75% of the right area (0.92 / 4 ≈ 0.23)
            // Mapping: SAIL 0.08-0.29, PRISM 0.30-0.51, CORE 0.52-0.73, SAT 0.74-0.99
            var sailColumn  = BuildTypeColumn("SailColumn",  root.transform,
                new Vector2(0.08f, 0f), new Vector2(0.29f, 1f),
                "SAIL",  StarChartTheme.SailColor,  trackView);

            var prismColumn = BuildTypeColumn("PrismColumn", root.transform,
                new Vector2(0.30f, 0f), new Vector2(0.51f, 1f),
                "PRISM", StarChartTheme.PrismColor, trackView);

            var coreColumn  = BuildTypeColumn("CoreColumn",  root.transform,
                new Vector2(0.52f, 0f), new Vector2(0.73f, 1f),
                "CORE",  StarChartTheme.CoreColor,  trackView);

            var satColumn   = BuildTypeColumn("SatColumn",   root.transform,
                new Vector2(0.74f, 0f), new Vector2(0.99f, 1f),
                "SAT",   StarChartTheme.SatColor,   trackView);

            // Note: TypeColumn is a top-level class (TypeColumn.cs), not a nested class of TrackView.
            // This ensures stable Unity GUID serialization across scene rebuilds.

            // ── Column dividers ──
            BuildColumnDivider("DivSailPrism", root.transform, 0.295f);
            BuildColumnDivider("DivPrismCore", root.transform, 0.515f);
            BuildColumnDivider("DivCoreSat",   root.transform, 0.735f);

            // Wire TrackView fields
            WireField(trackView, "_trackLabel",      labelTmp);
            WireField(trackView, "_selectButton",    selectBtn);
            WireField(trackView, "_selectionBorder", borderImg);
            WireField(trackView, "_sailColumn",      sailColumn);
            WireField(trackView, "_prismColumn",     prismColumn);
            WireField(trackView, "_coreColumn",      coreColumn);
            WireField(trackView, "_satColumn",       satColumn);

            return trackView;
        }

        /// <summary>
        /// Build a single TypeColumn: column header (label + dot) + 2×2 GridContainer (4 SlotCellViews).
        /// Returns the TypeColumn MonoBehaviour.
        /// </summary>
        private static TypeColumn BuildTypeColumn(
            string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax,
            string typeName, Color typeColor,
            TrackView ownerTrack)
        {
            var colGo = CreateUIObject(name, parent);
            SetAnchors(colGo, anchorMin, anchorMax);
            var typeColumn = colGo.AddComponent<TypeColumn>();

            // Column border (dim by default, brightens on hover)
            var borderGo = CreateUIObject("ColumnBorder", colGo.transform);
            SetStretch(borderGo);
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(typeColor.r, typeColor.g, typeColor.b, 0.18f);
            borderImg.type = Image.Type.Sliced;
            borderImg.raycastTarget = false;

            // Column header area (top 14%)
            var headerGo = CreateUIObject("ColumnHeader", colGo.transform);
            SetAnchors(headerGo, new Vector2(0f, 0.86f), new Vector2(1f, 1f));
            var headerHLG = headerGo.AddComponent<HorizontalLayoutGroup>();
            headerHLG.spacing = 3;
            headerHLG.padding = new RectOffset(4, 4, 2, 2);
            headerHLG.childAlignment = TextAnchor.MiddleLeft;
            headerHLG.childForceExpandWidth = false;
            headerHLG.childForceExpandHeight = true;

            // Type dot
            var dotGo = CreateUIObject("TypeDot", headerGo.transform);
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.color = typeColor;
            dotImg.raycastTarget = false;
            var dotLE = dotGo.AddComponent<LayoutElement>();
            dotLE.preferredWidth = 7;
            dotLE.preferredHeight = 7;

            // Type label
            var colLabelGo = CreateUIObject("TypeLabel", headerGo.transform);
            var colLabelTmp = colLabelGo.AddComponent<TextMeshProUGUI>();
            colLabelTmp.text = typeName;
            colLabelTmp.fontSize = 9;
            colLabelTmp.fontStyle = FontStyles.Bold;
            colLabelTmp.color = typeColor;
            colLabelTmp.raycastTarget = false;
            var labelLE = colLabelGo.AddComponent<LayoutElement>();
            labelLE.flexibleWidth = 1;

            // 2×2 GridContainer (bottom 84%)
            var gridGo = CreateUIObject("GridContainer", colGo.transform);
            SetAnchors(gridGo, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.84f));
            var gridLayout = gridGo.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(40, 40);
            gridLayout.spacing = new Vector2(2, 2);
            gridLayout.padding = new RectOffset(2, 2, 2, 2);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            // 4 SlotCellViews (row-major: [0]=TL, [1]=TR, [2]=BL, [3]=BR)
            var cells = new SlotCellView[4];
            for (int i = 0; i < 4; i++)
                cells[i] = BuildSlotCell($"Cell_{i}", gridGo.transform);

            // Wire TypeColumn fields
            WireField(typeColumn, "_columnLabel",  colLabelTmp);
            WireField(typeColumn, "_columnDot",    dotImg);
            WireField(typeColumn, "_columnBorder", borderImg);
            WireArrayField(typeColumn, "_cells", cells);

            return typeColumn;
        }

        // =====================================================================
        // GatlingCol Builder
        // =====================================================================

        /// <summary>
        /// Build the Gatling column: ▲ button, DrumCounter TMP_Text, ▼ button, LOADOUT label.
        /// Attaches and returns a <see cref="LoadoutSwitcher"/> component.
        /// </summary>
        private static LoadoutSwitcher BuildGatlingCol(GameObject root)
        {
            var loadoutSwitcher = root.AddComponent<LoadoutSwitcher>();

            // Background
            var bg = CreateUIObject("GatlingBg", root.transform);
            SetStretch(bg);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.06f, 0.10f, 0.90f);
            bgImg.raycastTarget = false;

            // Vertical layout
            var vlg = root.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.padding = new RectOffset(4, 4, 6, 6);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // ▲ Up button
            var btnUpGo = CreateUIObject("BtnUp", root.transform);
            var btnUpImg = btnUpGo.AddComponent<Image>();
            btnUpImg.color = new Color(0.1f, 0.15f, 0.25f, 0.9f);
            var btnUp = btnUpGo.AddComponent<Button>();
            btnUp.targetGraphic = btnUpImg;
            var btnUpLE = btnUpGo.AddComponent<LayoutElement>();
            btnUpLE.preferredHeight = 24;
            btnUpLE.flexibleWidth = 1;

            var btnUpLabel = CreateUIObject("Label", btnUpGo.transform);
            SetStretch(btnUpLabel);
            var btnUpTmp = btnUpLabel.AddComponent<TextMeshProUGUI>();
            btnUpTmp.text = "▲";
            btnUpTmp.fontSize = 12;
            btnUpTmp.alignment = TextAlignmentOptions.Center;
            btnUpTmp.color = StarChartTheme.CyanDim;
            btnUpTmp.raycastTarget = false;

            // DrumCounter
            var drumGo = CreateUIObject("DrumCounter", root.transform);
            var drumLE = drumGo.AddComponent<LayoutElement>();
            drumLE.preferredHeight = 40;
            drumLE.flexibleWidth = 1;
            var drumBg = drumGo.AddComponent<Image>();
            drumBg.color = new Color(0.06f, 0.09f, 0.15f, 0.95f);

            // Text must be on a child object because Image and TMP both inherit from Graphic
            var drumLabel = CreateUIObject("Label", drumGo.transform);
            SetStretch(drumLabel);
            var drumTmp = drumLabel.AddComponent<TextMeshProUGUI>();
            drumTmp.text = "LOADOUT #1\n<size=70%>1/1</size>";
            drumTmp.fontSize = 9;
            drumTmp.alignment = TextAlignmentOptions.Center;
            drumTmp.color = StarChartTheme.Cyan;
            drumTmp.raycastTarget = false;

            // ▼ Down button
            var btnDownGo = CreateUIObject("BtnDown", root.transform);
            var btnDownImg = btnDownGo.AddComponent<Image>();
            btnDownImg.color = new Color(0.1f, 0.15f, 0.25f, 0.9f);
            var btnDown = btnDownGo.AddComponent<Button>();
            btnDown.targetGraphic = btnDownImg;
            var btnDownLE = btnDownGo.AddComponent<LayoutElement>();
            btnDownLE.preferredHeight = 24;
            btnDownLE.flexibleWidth = 1;

            var btnDownLabel = CreateUIObject("Label", btnDownGo.transform);
            SetStretch(btnDownLabel);
            var btnDownTmp = btnDownLabel.AddComponent<TextMeshProUGUI>();
            btnDownTmp.text = "▼";
            btnDownTmp.fontSize = 12;
            btnDownTmp.alignment = TextAlignmentOptions.Center;
            btnDownTmp.color = StarChartTheme.CyanDim;
            btnDownTmp.raycastTarget = false;

            // LOADOUT label (bottom)
            var loadoutLabelGo = CreateUIObject("LoadoutLabel", root.transform);
            var loadoutLabelLE = loadoutLabelGo.AddComponent<LayoutElement>();
            loadoutLabelLE.preferredHeight = 16;
            loadoutLabelLE.flexibleWidth = 1;
            var loadoutLabelTmp = loadoutLabelGo.AddComponent<TextMeshProUGUI>();
            loadoutLabelTmp.text = "LOADOUT";
            loadoutLabelTmp.fontSize = 7;
            loadoutLabelTmp.fontStyle = FontStyles.Bold;
            loadoutLabelTmp.alignment = TextAlignmentOptions.Center;
            loadoutLabelTmp.color = StarChartTheme.CyanDim;
            loadoutLabelTmp.characterSpacing = 2;
            loadoutLabelTmp.raycastTarget = false;

            // Wire LoadoutSwitcher fields
            WireField(loadoutSwitcher, "_prevButton",       btnUp);
            WireField(loadoutSwitcher, "_nextButton",       btnDown);
            WireField(loadoutSwitcher, "_drumCounterLabel", drumTmp);

            return loadoutSwitcher;
        }

        /// <summary>
        /// Build the LoadoutCard header: ◈ icon + Loadout name + "LOADOUT #N" subtitle.
        /// </summary>
        private static void BuildLoadoutCardHeader(Transform parent)
        {
            var hlg = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(10, 10, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // ◈ icon
            var iconGo = CreateUIObject("CardIcon", parent);
            var iconTmp = iconGo.AddComponent<TextMeshProUGUI>();
            iconTmp.text = "◈";
            iconTmp.fontSize = 16;
            iconTmp.color = StarChartTheme.Cyan;
            iconTmp.raycastTarget = false;
            var iconLE = iconGo.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 20;

            // Name + subtitle (vertical stack)
            var nameStackGo = CreateUIObject("NameStack", parent);
            var nameStackVLG = nameStackGo.AddComponent<VerticalLayoutGroup>();
            nameStackVLG.spacing = 0;
            nameStackVLG.childForceExpandWidth = true;
            nameStackVLG.childForceExpandHeight = false;
            var nameStackLE = nameStackGo.AddComponent<LayoutElement>();
            nameStackLE.flexibleWidth = 1;

            var nameTmpGo = CreateUIObject("LoadoutName", nameStackGo.transform);
            var nameTmp = nameTmpGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "LOADOUT #1";
            nameTmp.fontSize = 13;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = Color.white;
            nameTmp.raycastTarget = false;
            var nameLE = nameTmpGo.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 18;

            var subtitleGo = CreateUIObject("LoadoutSubtitle", nameStackGo.transform);
            var subtitleTmp = subtitleGo.AddComponent<TextMeshProUGUI>();
            subtitleTmp.text = "LOADOUT #1";
            subtitleTmp.fontSize = 9;
            subtitleTmp.color = StarChartTheme.CyanDim;
            subtitleTmp.raycastTarget = false;
            var subtitleLE = subtitleGo.AddComponent<LayoutElement>();
            subtitleLE.preferredHeight = 12;
        }

        // =====================================================================
        // SlotCellView Builder
        // =====================================================================

        private static void BuildColumnDivider(string name, Transform parent, float xAnchor)
        {
            var go = CreateUIObject(name, parent);
            SetAnchors(go, new Vector2(xAnchor - 0.003f, 0.02f), new Vector2(xAnchor + 0.003f, 0.98f));
            var img = go.AddComponent<Image>();
            img.color = new Color(StarChartTheme.Border.r, StarChartTheme.Border.g, StarChartTheme.Border.b, 0.5f);
            img.raycastTarget = false;
        }

        private static SlotCellView BuildSlotCell(string name, Transform parent)
        {
            var cellGo = CreateUIObject(name, parent);
            var cellView = cellGo.AddComponent<SlotCellView>();

            // Background
            var bgGo = CreateUIObject("Background", cellGo.transform);
            SetStretch(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = StarChartTheme.SlotEmpty;

            // Icon
            var iconGo = CreateUIObject("Icon", cellGo.transform);
            SetAnchors(iconGo, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.clear;
            iconImg.enabled = false;
            iconImg.raycastTarget = false;

            // Placeholder '+' label
            var placeholderGo = CreateUIObject("PlaceholderLabel", cellGo.transform);
            SetStretch(placeholderGo);
            var placeholderTmp = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeholderTmp.text = "+";
            placeholderTmp.fontSize = 20;
            placeholderTmp.alignment = TextAlignmentOptions.Center;
            placeholderTmp.color = new Color(1f, 1f, 1f, 0.25f);
            placeholderTmp.raycastTarget = false;

            // Button
            var btnImg = cellGo.AddComponent<Image>();
            btnImg.color = Color.clear;
            var btn = cellGo.AddComponent<Button>();

            // Wire SlotCellView fields
            WireField(cellView, "_backgroundImage", bgImg);
            WireField(cellView, "_iconImage", iconImg);
            WireField(cellView, "_button", btn);
            WireField(cellView, "_placeholderLabel", placeholderTmp);

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
            // Match GridLayoutGroup cellSize: 44x44 (HTML prototype: --inv-slot-size: 44px)
            rootRect.sizeDelta = new Vector2(44, 44);
            root.AddComponent<CanvasGroup>(); // Required by InventoryItemView for drag alpha control
            var itemView = root.AddComponent<InventoryItemView>();

            // Type dot (top-left corner, colored circle)
            var typeDotGo = CreateUIObjectStandalone("TypeDot", root.transform);
            SetAnchors(typeDotGo, new Vector2(0f, 0.85f), new Vector2(0.25f, 1f));
            var typeDotImg = typeDotGo.AddComponent<Image>();
            typeDotImg.color = StarChartTheme.CoreColor; // default; overridden by Setup()
            typeDotImg.raycastTarget = false;

            // Equipped border (green tint overlay when equipped)
            var equippedBorderGo = CreateUIObjectStandalone("EquippedBorder", root.transform);
            SetStretch(equippedBorderGo);
            var equippedBorderImg = equippedBorderGo.AddComponent<Image>();
            equippedBorderImg.color = Color.clear; // hidden by default
            equippedBorderImg.raycastTarget = false;

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
            WireField(itemView, "_typeDot", typeDotImg);
            WireField(itemView, "_equippedBorder", equippedBorderImg);

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
