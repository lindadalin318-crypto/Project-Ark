using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectArk.SpaceLife.Editor
{
    /// <summary>
    /// Master Plan v1.1 Phase 2 (§6.1.3) — sole authoritative builder for SpaceLife UI prefabs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Authority:</b> this class owns the prefab structure of
    /// <see cref="DialogueUIPrefabPath"/>, <see cref="GiftUIPrefabPath"/>, and
    /// <see cref="SpaceLifeUIRootPrefabPath"/>. No other Editor tool or runtime code may
    /// create or modify these assets. <see cref="SpaceLifeSetupWindow"/> is frozen for UI
    /// construction from Master Plan v1.1 onward; scene panels must be instantiated from
    /// these prefabs.
    /// </para>
    /// <para>
    /// <b>Modes:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><b>Apply</b> (<c>ProjectArk &gt; SpaceLife &gt; Build UI Prefabs (Apply)</c>):
    /// generates or refreshes all three prefabs. Destructive — any manual edits to prefab
    /// assets at the paths below will be overwritten.</item>
    /// <item><b>Audit</b> (<c>ProjectArk &gt; SpaceLife &gt; Audit UI Prefabs</c>): read-only.
    /// Reports whether each prefab exists, whether its critical hierarchy is intact, and
    /// whether the Presenter serialized fields are correctly wired. Never writes.</item>
    /// </list>
    /// <para>
    /// <b>Explicit prohibitions</b> (Master Plan §6.1.3):
    /// </para>
    /// <list type="bullet">
    /// <item>Must not be invoked from <c>OnValidate</c>, <c>Awake</c>, or Play Mode.</item>
    /// <item>Must not modify scene instances — only the Prefab assets.</item>
    /// </list>
    /// </remarks>
    internal static class SpaceLifeUIPrefabBuilder
    {
        // ------------------------------------------------------------
        // Constants
        // ------------------------------------------------------------

        private const string LogPrefix = "[SpaceLifeUIPrefabBuilder]";

        // Prefab output folder — deliberately separate from SetupWindow's legacy
        // Assets/_Prefabs/SpaceLife/UI to avoid confusion during the Phase 2 migration.
        private const string UIPrefabsFolder = "Assets/_Prefabs/UI/SpaceLife";

        public const string DialogueUIPrefabPath = UIPrefabsFolder + "/DialogueUI.prefab";
        public const string GiftUIPrefabPath = UIPrefabsFolder + "/GiftUI.prefab";
        public const string SpaceLifeUIRootPrefabPath = UIPrefabsFolder + "/SpaceLifeUIRoot.prefab";
        public const string OptionButtonPrefabPath = UIPrefabsFolder + "/OptionButton.prefab";

        // ------------------------------------------------------------
        // Menu entries
        // ------------------------------------------------------------

        [MenuItem("ProjectArk/Space Life/Build UI Prefabs (Apply)", priority = 10)]
        public static void ApplyMenu()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Build SpaceLife UI Prefabs",
                "This will regenerate all SpaceLife UI prefabs at:\n\n" +
                $"  {DialogueUIPrefabPath}\n" +
                $"  {GiftUIPrefabPath}\n" +
                $"  {SpaceLifeUIRootPrefabPath}\n" +
                $"  {OptionButtonPrefabPath}\n\n" +
                "Any manual edits to those asset files will be overwritten.\n" +
                "Scene instances are NOT modified.\n\n" +
                "Continue?",
                "Yes, Rebuild",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("SpaceLife UI Prefab Builder", "Preparing folders...", 0f);
                EnsureFolder("Assets", "_Prefabs");
                EnsureFolder("Assets/_Prefabs", "UI");
                EnsureFolder("Assets/_Prefabs/UI", "SpaceLife");

                EditorUtility.DisplayProgressBar("SpaceLife UI Prefab Builder", "Building OptionButton...", 0.15f);
                GameObject optionButtonPrefab = BuildOptionButtonPrefab();

                EditorUtility.DisplayProgressBar("SpaceLife UI Prefab Builder", "Building DialogueUI...", 0.35f);
                GameObject dialoguePrefab = BuildDialogueUIPrefab(optionButtonPrefab);

                EditorUtility.DisplayProgressBar("SpaceLife UI Prefab Builder", "Building GiftUI...", 0.65f);
                GameObject giftPrefab = BuildGiftUIPrefab(optionButtonPrefab);

                EditorUtility.DisplayProgressBar("SpaceLife UI Prefab Builder", "Building SpaceLifeUIRoot...", 0.85f);
                BuildSpaceLifeUIRootPrefab(dialoguePrefab, giftPrefab);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();

                Debug.Log($"{LogPrefix} Apply complete. Rebuilt 4 prefabs under {UIPrefabsFolder}.");
                EditorUtility.DisplayDialog(
                    "Build Complete",
                    $"SpaceLife UI prefabs rebuilt under:\n  {UIPrefabsFolder}\n\n" +
                    "Remember: scene instances already placed in SampleScene need to be replaced " +
                    "with instances of the new prefabs before Phase 3 scene wiring.",
                    "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"{LogPrefix} Apply failed: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Build Failed",
                    $"Prefab build failed:\n{e.Message}\n\nCheck the Console for the full stack trace.",
                    "OK");
            }
        }

        [MenuItem("ProjectArk/Space Life/Audit UI Prefabs", priority = 11)]
        public static void AuditMenu()
        {
            var report = new StringBuilder();
            report.AppendLine("SpaceLife UI Prefab Audit");
            report.AppendLine("=========================");

            int issues = 0;

            issues += AuditPrefabFolder(report);
            issues += AuditOptionButton(report);
            issues += AuditDialogueUI(report);
            issues += AuditGiftUI(report);
            issues += AuditSpaceLifeUIRoot(report);

            report.AppendLine();
            if (issues == 0)
            {
                report.AppendLine("Result: OK — all prefabs present and structurally valid.");
                Debug.Log($"{LogPrefix} Audit passed.\n{report}");
            }
            else
            {
                report.AppendLine($"Result: {issues} issue(s) found.");
                report.AppendLine("Fix with: ProjectArk > SpaceLife > Build UI Prefabs (Apply)");
                Debug.LogError($"{LogPrefix} Audit failed.\n{report}");
            }
        }

        // ------------------------------------------------------------
        // Apply — OptionButton (shared dependency)
        // ------------------------------------------------------------

        /// <summary>
        /// Shared button prefab used by both DialogueUI (choices) and GiftUI (items).
        /// </summary>
        private static GameObject BuildOptionButtonPrefab()
        {
            GameObject buttonGo = new GameObject("OptionButton",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));

            var rect = (RectTransform)buttonGo.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, 36f);

            var image = buttonGo.GetComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
            image.raycastTarget = true;

            var button = buttonGo.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(0.9f, 0.9f, 1f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.8f, 1f);
            button.colors = colors;

            GameObject labelGo = new GameObject("Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(buttonGo.transform, false);
            SetStretchAll((RectTransform)labelGo.transform);

            var label = labelGo.GetComponent<Text>();
            label.text = "Option";
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.raycastTarget = false;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(buttonGo, OptionButtonPrefabPath);
            Object.DestroyImmediate(buttonGo);
            return prefab;
        }

        // ------------------------------------------------------------
        // Apply — DialogueUI
        // ------------------------------------------------------------

        private static GameObject BuildDialogueUIPrefab(GameObject optionButtonPrefab)
        {
            // Root (stretched RectTransform — the parent Canvas determines screen placement)
            GameObject root = new GameObject("DialogueUI", typeof(RectTransform));
            SetStretchAll((RectTransform)root.transform);

            var presenter = root.AddComponent<DialogueUIPresenter>();

            // Panel (bottom dialogue box)
            GameObject panel = BuildPanel(root.transform, "DialoguePanel",
                anchorMin: new Vector2(0.1f, 0.05f),
                anchorMax: new Vector2(0.9f, 0.35f),
                backgroundColor: new Color(0f, 0f, 0f, 0.75f));
            CanvasGroup panelCanvasGroup = panel.GetComponent<CanvasGroup>();

            // Avatar (upper-left inside panel)
            Image avatar = BuildImage(panel.transform, "Avatar",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f),
                anchoredPosition: new Vector2(12f, -12f),
                sizeDelta: new Vector2(96f, 96f),
                color: Color.white);
            avatar.enabled = false;

            // Speaker name (top bar)
            Text speakerName = BuildText(panel.transform, "SpeakerName",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(0.5f, 1f),
                anchoredPosition: new Vector2(60f, -12f),
                sizeDelta: new Vector2(-132f, 32f),
                text: string.Empty,
                fontSize: 20,
                alignment: TextAnchor.UpperLeft,
                color: new Color(1f, 0.9f, 0.6f, 1f));

            // Dialogue body text
            Text dialogueText = BuildText(panel.transform, "DialogueText",
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPosition: Vector2.zero,
                sizeDelta: new Vector2(-140f, -120f),
                text: string.Empty,
                fontSize: 18,
                alignment: TextAnchor.UpperLeft,
                color: Color.white,
                offsetMin: new Vector2(120f, 60f),
                offsetMax: new Vector2(-20f, -56f));

            // Options container (bottom strip, vertical stack)
            GameObject options = new GameObject("OptionsContainer", typeof(RectTransform));
            options.transform.SetParent(panel.transform, false);
            var optRect = (RectTransform)options.transform;
            optRect.anchorMin = new Vector2(0f, 0f);
            optRect.anchorMax = new Vector2(1f, 0f);
            optRect.pivot = new Vector2(0.5f, 0f);
            optRect.offsetMin = new Vector2(120f, 8f);
            optRect.offsetMax = new Vector2(-20f, 56f);
            var layout = options.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            // Close button (top-right)
            Button closeButton = BuildButton(panel.transform, "CloseButton",
                anchorMin: new Vector2(1f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 1f),
                anchoredPosition: new Vector2(-8f, -8f),
                sizeDelta: new Vector2(60f, 28f),
                label: "×",
                fontSize: 20);

            // Wire serialized fields
            var so = new SerializedObject(presenter);
            so.FindProperty("_dialoguePanel").objectReferenceValue = panel;
            so.FindProperty("_dialogueCanvasGroup").objectReferenceValue = panelCanvasGroup;
            so.FindProperty("_avatarImage").objectReferenceValue = avatar;
            so.FindProperty("_speakerNameText").objectReferenceValue = speakerName;
            so.FindProperty("_dialogueText").objectReferenceValue = dialogueText;
            so.FindProperty("_optionsContainer").objectReferenceValue = options.transform;
            so.FindProperty("_optionButtonPrefab").objectReferenceValue = optionButtonPrefab;
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Initialize hidden — CanvasGroup only (Implement_rules.md §12.6 R5: never SetActive(false))
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DialogueUIPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ------------------------------------------------------------
        // Apply — GiftUI
        // ------------------------------------------------------------

        private static GameObject BuildGiftUIPrefab(GameObject optionButtonPrefab)
        {
            GameObject root = new GameObject("GiftUI", typeof(RectTransform));
            SetStretchAll((RectTransform)root.transform);

            var presenter = root.AddComponent<GiftUIPresenter>();

            GameObject panel = BuildPanel(root.transform, "GiftPanel",
                anchorMin: new Vector2(0.25f, 0.2f),
                anchorMax: new Vector2(0.75f, 0.8f),
                backgroundColor: new Color(0.05f, 0.05f, 0.1f, 0.9f));
            CanvasGroup panelCanvasGroup = panel.GetComponent<CanvasGroup>();

            Text npcNameText = BuildText(panel.transform, "NpcNameText",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(0.5f, 1f),
                anchoredPosition: new Vector2(0f, -16f),
                sizeDelta: new Vector2(-40f, 36f),
                text: string.Empty,
                fontSize: 22,
                alignment: TextAnchor.MiddleCenter,
                color: Color.white);

            GameObject itemsContainer = new GameObject("ItemsContainer", typeof(RectTransform));
            itemsContainer.transform.SetParent(panel.transform, false);
            var itemsRect = (RectTransform)itemsContainer.transform;
            itemsRect.anchorMin = new Vector2(0f, 0f);
            itemsRect.anchorMax = new Vector2(1f, 1f);
            itemsRect.offsetMin = new Vector2(16f, 52f);
            itemsRect.offsetMax = new Vector2(-16f, -60f);
            var grid = itemsContainer.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(120f, 64f);
            grid.spacing = new Vector2(8f, 8f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.childAlignment = TextAnchor.UpperLeft;

            Button closeButton = BuildButton(panel.transform, "CloseButton",
                anchorMin: new Vector2(1f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 1f),
                anchoredPosition: new Vector2(-8f, -8f),
                sizeDelta: new Vector2(60f, 28f),
                label: "×",
                fontSize: 20);

            var so = new SerializedObject(presenter);
            so.FindProperty("_giftPanel").objectReferenceValue = panel;
            so.FindProperty("_giftCanvasGroup").objectReferenceValue = panelCanvasGroup;
            so.FindProperty("_itemsContainer").objectReferenceValue = itemsContainer.transform;
            so.FindProperty("_itemButtonPrefab").objectReferenceValue = optionButtonPrefab;
            so.FindProperty("_npcNameText").objectReferenceValue = npcNameText;
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GiftUIPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ------------------------------------------------------------
        // Apply — SpaceLifeUIRoot
        // ------------------------------------------------------------

        /// <summary>
        /// Scene-level UI root: Canvas + CanvasScaler + GraphicRaycaster, with the
        /// DialogueUI and GiftUI prefab instances nested as children. This is what
        /// Phase 3 scene wiring drops into SampleScene.
        /// </summary>
        private static GameObject BuildSpaceLifeUIRootPrefab(GameObject dialoguePrefab, GameObject giftPrefab)
        {
            GameObject root = new GameObject("SpaceLifeUIRoot",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Nested prefab instances — these remain live links to the source prefabs,
            // so regenerating DialogueUI/GiftUI will propagate into any SpaceLifeUIRoot
            // instance placed in a scene.
            if (dialoguePrefab != null)
            {
                GameObject dialogueChild = (GameObject)PrefabUtility.InstantiatePrefab(dialoguePrefab);
                dialogueChild.transform.SetParent(root.transform, false);
            }

            if (giftPrefab != null)
            {
                GameObject giftChild = (GameObject)PrefabUtility.InstantiatePrefab(giftPrefab);
                giftChild.transform.SetParent(root.transform, false);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, SpaceLifeUIRootPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ------------------------------------------------------------
        // Audit
        // ------------------------------------------------------------

        private static int AuditPrefabFolder(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine($"[Folder] {UIPrefabsFolder}");
            if (AssetDatabase.IsValidFolder(UIPrefabsFolder))
            {
                report.AppendLine("  OK");
                return 0;
            }

            report.AppendLine("  MISSING — folder does not exist yet.");
            return 1;
        }

        private static int AuditOptionButton(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine($"[Prefab] {OptionButtonPrefabPath}");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OptionButtonPrefabPath);
            if (prefab == null)
            {
                report.AppendLine("  MISSING");
                return 1;
            }

            int issues = 0;
            if (prefab.GetComponent<Button>() == null)
            {
                report.AppendLine("  Missing Button component on root");
                issues++;
            }

            if (prefab.transform.Find("Label") == null)
            {
                report.AppendLine("  Missing child: Label");
                issues++;
            }

            if (issues == 0) report.AppendLine("  OK");
            return issues;
        }

        private static int AuditDialogueUI(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine($"[Prefab] {DialogueUIPrefabPath}");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueUIPrefabPath);
            if (prefab == null)
            {
                report.AppendLine("  MISSING");
                return 1;
            }

            int issues = 0;
            var presenter = prefab.GetComponent<DialogueUIPresenter>();
            if (presenter == null)
            {
                report.AppendLine("  Missing DialogueUIPresenter component on root");
                issues++;
            }

            // Structural checks
            string[] expectedChildren =
            {
                "DialoguePanel",
                "DialoguePanel/Avatar",
                "DialoguePanel/SpeakerName",
                "DialoguePanel/DialogueText",
                "DialoguePanel/OptionsContainer",
                "DialoguePanel/CloseButton",
            };

            foreach (string path in expectedChildren)
            {
                if (prefab.transform.Find(path) == null)
                {
                    report.AppendLine($"  Missing child: {path}");
                    issues++;
                }
            }

            // Serialized field checks
            if (presenter != null)
            {
                string[] serializedFields =
                {
                    "_dialoguePanel",
                    "_dialogueCanvasGroup",
                    "_avatarImage",
                    "_speakerNameText",
                    "_dialogueText",
                    "_optionsContainer",
                    "_optionButtonPrefab",
                    "_closeButton",
                };

                var so = new SerializedObject(presenter);
                foreach (string fieldName in serializedFields)
                {
                    var prop = so.FindProperty(fieldName);
                    if (prop == null)
                    {
                        report.AppendLine($"  Serialized field not found: {fieldName}");
                        issues++;
                        continue;
                    }

                    if (prop.objectReferenceValue == null)
                    {
                        report.AppendLine($"  Serialized field is null: {fieldName}");
                        issues++;
                    }
                }
            }

            if (issues == 0) report.AppendLine("  OK");
            return issues;
        }

        private static int AuditGiftUI(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine($"[Prefab] {GiftUIPrefabPath}");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GiftUIPrefabPath);
            if (prefab == null)
            {
                report.AppendLine("  MISSING");
                return 1;
            }

            int issues = 0;
            var presenter = prefab.GetComponent<GiftUIPresenter>();
            if (presenter == null)
            {
                report.AppendLine("  Missing GiftUIPresenter component on root");
                issues++;
            }

            string[] expectedChildren =
            {
                "GiftPanel",
                "GiftPanel/NpcNameText",
                "GiftPanel/ItemsContainer",
                "GiftPanel/CloseButton",
            };

            foreach (string path in expectedChildren)
            {
                if (prefab.transform.Find(path) == null)
                {
                    report.AppendLine($"  Missing child: {path}");
                    issues++;
                }
            }

            if (presenter != null)
            {
                string[] serializedFields =
                {
                    "_giftPanel",
                    "_giftCanvasGroup",
                    "_itemsContainer",
                    "_itemButtonPrefab",
                    "_npcNameText",
                    "_closeButton",
                };

                var so = new SerializedObject(presenter);
                foreach (string fieldName in serializedFields)
                {
                    var prop = so.FindProperty(fieldName);
                    if (prop == null)
                    {
                        report.AppendLine($"  Serialized field not found: {fieldName}");
                        issues++;
                        continue;
                    }

                    if (prop.objectReferenceValue == null)
                    {
                        report.AppendLine($"  Serialized field is null: {fieldName}");
                        issues++;
                    }
                }
            }

            if (issues == 0) report.AppendLine("  OK");
            return issues;
        }

        private static int AuditSpaceLifeUIRoot(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine($"[Prefab] {SpaceLifeUIRootPrefabPath}");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpaceLifeUIRootPrefabPath);
            if (prefab == null)
            {
                report.AppendLine("  MISSING");
                return 1;
            }

            int issues = 0;

            var canvas = prefab.GetComponent<Canvas>();
            if (canvas == null)
            {
                report.AppendLine("  Missing Canvas on root");
                issues++;
            }

            if (prefab.GetComponent<CanvasScaler>() == null)
            {
                report.AppendLine("  Missing CanvasScaler on root");
                issues++;
            }

            if (prefab.GetComponent<GraphicRaycaster>() == null)
            {
                report.AppendLine("  Missing GraphicRaycaster on root");
                issues++;
            }

            // Expect nested DialogueUI + GiftUI prefab instances
            if (prefab.GetComponentInChildren<DialogueUIPresenter>(true) == null)
            {
                report.AppendLine("  Missing nested DialogueUIPresenter child");
                issues++;
            }

            if (prefab.GetComponentInChildren<GiftUIPresenter>(true) == null)
            {
                report.AppendLine("  Missing nested GiftUIPresenter child");
                issues++;
            }

            if (issues == 0) report.AppendLine("  OK");
            return issues;
        }

        // ------------------------------------------------------------
        // Build helpers (mirrors SpaceLifeSetupWindow helpers, kept local so the
        // builder is the single authority for UI prefab structure).
        // ------------------------------------------------------------

        private static GameObject BuildPanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color backgroundColor, bool addCanvasGroup = true)
        {
            GameObject panel = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);

            var rect = (RectTransform)panel.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = panel.GetComponent<Image>();
            image.color = backgroundColor;
            image.raycastTarget = true;

            if (addCanvasGroup)
            {
                var canvasGroup = panel.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            return panel;
        }

        private static Text BuildText(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta,
            string text, int fontSize, TextAnchor alignment, Color color,
            Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;

            if (offsetMin.HasValue && offsetMax.HasValue)
            {
                rect.offsetMin = offsetMin.Value;
                rect.offsetMax = offsetMax.Value;
            }
            else
            {
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = sizeDelta;
            }

            var textComponent = go.GetComponent<Text>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = alignment;
            textComponent.color = color;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            textComponent.raycastTarget = false;

            return textComponent;
        }

        private static Image BuildImage(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return image;
        }

        private static Button BuildButton(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta,
            string label, int fontSize)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(0.9f, 0.9f, 1f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.8f, 1f);
            button.colors = colors;

            GameObject labelGo = new GameObject("Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            SetStretchAll((RectTransform)labelGo.transform);

            var textComponent = labelGo.GetComponent<Text>();
            textComponent.text = label;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.raycastTarget = false;

            return button;
        }

        private static void SetStretchAll(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string parent, string newFolder)
        {
            string path = $"{parent}/{newFolder}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, newFolder);
            }
        }
    }
}
