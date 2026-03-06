using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Backpack Monsters rendering philosophy: Icon and Shape are two independent layers.
    ///
    /// Shape Layer  — per-cell Images driven by ItemShapeHelper.GetCells().
    ///                 Responsible for: occupancy color, highlight, selection state.
    ///                 Empty bounding-box cells stay Color.clear (true holes).
    ///
    /// Icon Layer   — a single fixed-size Image anchored to the visual centroid of the shape.
    ///                 NOT stretched to the bounding box. NOT affected by shape.
    ///                 Responsible for: "what is this item" identity.
    ///
    /// This static utility class is the single place that knows how to build both layers.
    /// InventoryItemView, ItemOverlayView, and DragGhostView all delegate here.
    /// </summary>
    public static class ItemIconRenderer
    {
        // =====================================================================
        // Shape Layer
        // =====================================================================

        /// <summary>
        /// Build shape-cell Images inside <paramref name="parent"/> using anchor-based sizing.
        /// Each active cell gets <paramref name="activeColor"/>; empty bounding-box cells
        /// are Color.clear with raycastTarget=false (true holes, not filled squares).
        /// Cells are inserted as first siblings so they render behind icon and labels.
        /// Returns the created Image array (length = bounds.x * bounds.y).
        /// </summary>
        public static Image[] BuildShapeCells(
            Transform parent,
            ItemShape shape,
            Color activeColor,
            float cellPaddingPx = 1f)
        {
            var cells  = ItemShapeHelper.GetCells(shape);
            var bounds = ItemShapeHelper.GetBounds(shape);

            var activeCellSet = new HashSet<Vector2Int>();
            foreach (var c in cells) activeCellSet.Add(c);

            int total = bounds.x * bounds.y;
            var images = new Image[total];

            float cellW = 1f / bounds.x;
            float cellH = 1f / bounds.y;
            int idx = 0;

            for (int row = 0; row < bounds.y; row++)
            {
                for (int col = 0; col < bounds.x; col++)
                {
                    bool isActive = activeCellSet.Contains(new Vector2Int(col, row));

                    var go = new GameObject($"ShapeCell_{col}_{row}",
                        typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(parent, false);
                    go.transform.SetAsFirstSibling(); // behind icon and labels

                    var rt = go.GetComponent<RectTransform>();
                    float xMin = col * cellW;
                    float yMax = 1f - row * cellH;
                    rt.anchorMin = new Vector2(xMin,         yMax - cellH);
                    rt.anchorMax = new Vector2(xMin + cellW, yMax);
                    rt.offsetMin = new Vector2( cellPaddingPx,  cellPaddingPx);
                    rt.offsetMax = new Vector2(-cellPaddingPx, -cellPaddingPx);

                    var img = go.GetComponent<Image>();
                    img.color         = isActive ? activeColor : Color.clear;
                    img.raycastTarget = false; // shape cells never block interaction
                    images[idx++] = img;
                }
            }

            return images;
        }

        /// <summary>
        /// Build shape-cell Images using absolute pixel sizing (for overlays and ghosts
        /// where the parent is NOT sized to the bounding box via anchors).
        /// </summary>
        public static Image[] BuildShapeCellsAbsolute(
            Transform parent,
            ItemShape shape,
            Color activeColor,
            float cellSize,
            float cellGap)
        {
            var cells  = ItemShapeHelper.GetCells(shape);
            var bounds = ItemShapeHelper.GetBounds(shape);

            var occupiedSet = new HashSet<Vector2Int>();
            foreach (var c in cells) occupiedSet.Add(c);

            int total = bounds.x * bounds.y;
            var images = new Image[total];

            for (int r = 0; r < bounds.y; r++)
            {
                for (int c = 0; c < bounds.x; c++)
                {
                    int idx = r * bounds.x + c;
                    bool isActive = occupiedSet.Contains(new Vector2Int(c, r));

                    var go = new GameObject($"ShapeCell_{c}_{r}",
                        typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(parent, false);
                    go.transform.SetAsFirstSibling();

                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin        = Vector2.zero;
                    rt.anchorMax        = Vector2.zero;
                    rt.pivot            = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2( c * (cellSize + cellGap),
                                                      -r * (cellSize + cellGap));
                    rt.sizeDelta        = new Vector2(cellSize, cellSize);

                    var img = go.GetComponent<Image>();
                    img.color         = isActive ? activeColor : Color.clear;
                    img.raycastTarget = false;
                    images[idx] = img;
                }
            }

            return images;
        }

        /// <summary>
        /// Build shape-cell Images by directly reading each SlotCellView's RectTransform —
        /// the same pixel-perfect approach used by DragHighlightLayer.
        /// The parent overlay must be a stretch-anchor child of the same GridContainer as the cells.
        /// Each active cell gets <paramref name="activeColor"/>; bounding-box holes are Color.clear.
        /// </summary>
        /// <param name="parent">The overlay transform (stretch-anchored to GridContainer).</param>
        /// <param name="shape">The item shape.</param>
        /// <param name="activeColor">Color for occupied cells.</param>
        /// <param name="cells">All SlotCellViews in the column (row-major, gridCols wide).</param>
        /// <param name="anchorCol">Anchor column of the item in the grid.</param>
        /// <param name="anchorRow">Anchor row of the item in the grid.</param>
        /// <param name="gridCols">Number of columns in the grid (for col+row → index).</param>
        public static Image[] BuildShapeCellsFromCells(
            Transform parent,
            ItemShape shape,
            Color activeColor,
            SlotCellView[] cells,
            int anchorCol,
            int anchorRow,
            int gridCols)
        {
            var shapeCells = ItemShapeHelper.GetCells(shape);
            var bounds     = ItemShapeHelper.GetBounds(shape);

            var occupiedSet = new HashSet<Vector2Int>();
            foreach (var c in shapeCells) occupiedSet.Add(c);

            int total  = bounds.x * bounds.y;
            var images = new Image[total];

            for (int r = 0; r < bounds.y; r++)
            {
                for (int c = 0; c < bounds.x; c++)
                {
                    int idx      = r * bounds.x + c;
                    bool isActive = occupiedSet.Contains(new Vector2Int(c, r));

                    var go = new GameObject($"ShapeCell_{c}_{r}",
                        typeof(RectTransform), typeof(Image));

                    // CRITICAL: Add LayoutElement.ignoreLayout BEFORE SetParent.
                    // GridLayoutGroup repositions children immediately on SetParent,
                    // so the LayoutElement must exist before the node enters the hierarchy.
                    var le = go.AddComponent<LayoutElement>();
                    le.ignoreLayout = true;

                    go.transform.SetParent(parent, false);
                    go.transform.SetAsFirstSibling();

                    var rt  = go.GetComponent<RectTransform>();
                    var img = go.GetComponent<Image>();
                    img.color         = isActive ? activeColor : Color.clear;
                    img.raycastTarget = false;

                    // Directly copy position/size from the corresponding cell's RectTransform —
                    // guaranteed pixel-perfect regardless of GridLayoutGroup settings.
                    int cellIndex = (anchorRow + r) * gridCols + (anchorCol + c);
                    if (cells != null && cellIndex >= 0 && cellIndex < cells.Length
                        && cells[cellIndex] != null)
                    {
                        var cellRt = cells[cellIndex].GetComponent<RectTransform>();
                        if (cellRt != null)
                        {
                            rt.anchorMin        = cellRt.anchorMin;
                            rt.anchorMax        = cellRt.anchorMax;
                            rt.pivot            = cellRt.pivot;
                            rt.anchoredPosition = cellRt.anchoredPosition;
                            rt.sizeDelta        = cellRt.sizeDelta;
                        }
                    }

                    images[idx] = img;
                }
            }

            return images;
        }

        /// <summary>
        /// Build a fixed-size Icon Image centered on the anchor cell,
        /// by directly reading the anchor cell's RectTransform.
        /// The parent overlay must be a stretch-anchor child of the same GridContainer.
        /// </summary>
        public static Image BuildIconFromCell(
            Transform parent,
            StarChartItemSO item,
            SlotCellView[] cells,
            int anchorCol,
            int anchorRow,
            int gridCols,
            float iconSizePx = 36f,
            float alpha = 1f)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));

            // CRITICAL: Add LayoutElement.ignoreLayout BEFORE SetParent.
            // GridLayoutGroup repositions children immediately on SetParent.
            var le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            SetIconSprite(img, item, alpha);

            var rt = go.GetComponent<RectTransform>();

            int cellIndex = anchorRow * gridCols + anchorCol;
            if (cells != null && cellIndex >= 0 && cellIndex < cells.Length
                && cells[cellIndex] != null)
            {
                var cellRt = cells[cellIndex].GetComponent<RectTransform>();
                if (cellRt != null)
                {
                    // Copy anchor/pivot/position from the cell, then override sizeDelta to icon size
                    rt.anchorMin        = cellRt.anchorMin;
                    rt.anchorMax        = cellRt.anchorMax;
                    rt.pivot            = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = cellRt.anchoredPosition;
                    rt.sizeDelta        = new Vector2(iconSizePx, iconSizePx);
                    return img;
                }
            }

            // Fallback: center in parent
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(iconSizePx, iconSizePx);
            return img;
        }

        /// <summary>
        /// Recolor existing shape-cell Images (e.g. on equip-state change).
        /// <paramref name="cellImages"/> must have been created by BuildShapeCells.
        /// </summary>
        public static void RefreshShapeCellColors(
            Image[] cellImages,
            ItemShape shape,
            Color activeColor)
        {
            if (cellImages == null) return;

            var cells  = ItemShapeHelper.GetCells(shape);
            var bounds = ItemShapeHelper.GetBounds(shape);

            var activeCellSet = new HashSet<Vector2Int>();
            foreach (var c in cells) activeCellSet.Add(c);

            int idx = 0;
            for (int row = 0; row < bounds.y; row++)
            {
                for (int col = 0; col < bounds.x; col++)
                {
                    if (idx >= cellImages.Length) break;
                    bool isActive = activeCellSet.Contains(new Vector2Int(col, row));
                    if (cellImages[idx] != null)
                        cellImages[idx].color = isActive ? activeColor : Color.clear;
                    idx++;
                }
            }
        }

        // =====================================================================
        // Icon Layer
        // =====================================================================

        /// <summary>
        /// Create a fixed-size Icon Image centered on the shape's visual centroid.
        ///
        /// The icon is NOT stretched to the bounding box — it is a fixed square
        /// (<paramref name="iconSizePx"/> × <paramref name="iconSizePx"/>) placed at the
        /// centroid of the active cells, expressed as a fraction of the parent's size.
        ///
        /// This is the Backpack Monsters approach: Icon = identity, Shape = occupancy.
        /// </summary>
        public static Image BuildIconCentered(
            Transform parent,
            StarChartItemSO item,
            float iconSizePx = 36f,
            float alpha = 1f)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;

            SetIconSprite(img, item, alpha);

            // Position: centroid of active cells in normalized parent space
            var centroid = GetShapeCentroidNormalized(item.Shape);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = centroid;
            rt.anchorMax        = centroid;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(iconSizePx, iconSizePx);
            rt.anchoredPosition = Vector2.zero;

            return img;
        }

        /// <summary>
        /// Create a fixed-size Icon Image centered on the anchor cell of an overlay
        /// (absolute pixel positioning, for ItemOverlayView).
        /// </summary>
        public static Image BuildIconOnAnchorCell(
            Transform parent,
            StarChartItemSO item,
            float cellSize,
            float iconSizePx = 36f,
            float alpha = 1f)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;

            SetIconSprite(img, item, alpha);

            // Center on anchor cell (col=0, row=0 of the overlay)
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.zero;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(cellSize * 0.5f, -cellSize * 0.5f);
            rt.sizeDelta        = new Vector2(iconSizePx, iconSizePx);

            return img;
        }

        /// <summary>
        /// Create a fixed-size Icon Image using absolute pixel positioning,
        /// matching the coordinate system of <see cref="BuildShapeCellsAbsolute"/>.
        ///
        /// The icon is placed at the visual centroid of the active cells,
        /// computed in the same pixel space as the shape cells:
        ///   anchorMin = anchorMax = zero, pivot = (0,1),
        ///   position = (col * step, -row * step).
        ///
        /// Use this in DragGhostView where shape cells are built with BuildShapeCellsAbsolute.
        /// Do NOT use BuildIconCentered there — it uses normalized anchor fractions which
        /// are incompatible with the absolute-pixel coordinate system of the shape cells.
        /// </summary>
        public static Image BuildIconAbsolute(
            Transform parent,
            StarChartItemSO item,
            ItemShape shape,
            float cellSize,
            float cellGap,
            float iconSizePx = 36f,
            float alpha = 1f)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;

            SetIconSprite(img, item, alpha);

            // Compute centroid in absolute pixel space (same system as BuildShapeCellsAbsolute).
            // Cell center = (col * step + cellSize*0.5,  -(row * step + cellSize*0.5))
            // because pivot=(0,1) means y increases downward (negative anchoredPosition.y).
            var cells = ItemShapeHelper.GetCells(shape);
            float step = cellSize + cellGap;
            float sumX = 0f, sumY = 0f;
            foreach (var c in cells)
            {
                sumX += c.x * step + cellSize * 0.5f;
                sumY += -(c.y * step + cellSize * 0.5f);
            }
            float cx = sumX / cells.Count;
            float cy = sumY / cells.Count;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.zero;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(cx, cy);
            rt.sizeDelta        = new Vector2(iconSizePx, iconSizePx);

            return img;
        }

        /// <summary>
        /// Update an existing icon Image's sprite and color (e.g. on equip-state change).
        /// </summary>
        public static void SetIconSprite(Image iconImage, StarChartItemSO item, float alpha = 1f)
        {
            if (iconImage == null || item == null) return;

            if (item.Icon != null)
            {
                iconImage.sprite = item.Icon;
                iconImage.color  = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                // No icon asset: use a white placeholder tinted with the type color
                iconImage.sprite = GetOrCreateWhitePlaceholder();
                Color tc = StarChartTheme.GetTypeColor(item.ItemType);
                iconImage.color  = new Color(tc.r, tc.g, tc.b, alpha * 0.8f);
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        /// <summary>
        /// Compute the centroid of the active cells in normalized [0,1] parent space.
        /// Used to anchor the icon so it visually sits "inside" the shape.
        /// </summary>
        public static Vector2 GetShapeCentroidNormalized(ItemShape shape)
        {
            var cells  = ItemShapeHelper.GetCells(shape);
            var bounds = ItemShapeHelper.GetBounds(shape);

            float sumX = 0f, sumY = 0f;
            foreach (var c in cells)
            {
                // Cell center in normalized space: (col+0.5)/boundsX, (row+0.5)/boundsY
                sumX += (c.x + 0.5f) / bounds.x;
                sumY += (c.y + 0.5f) / bounds.y;
            }

            float cx = sumX / cells.Count;
            float cy = sumY / cells.Count;

            // Unity UI: y=0 is bottom, y=1 is top — invert row axis
            return new Vector2(cx, 1f - cy);
        }

        // Cached 1x1 white sprite used as placeholder when item has no icon.
        private static Sprite _whitePlaceholder;
        private static Sprite GetOrCreateWhitePlaceholder()
        {
            if (_whitePlaceholder != null) return _whitePlaceholder;
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            _whitePlaceholder = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            return _whitePlaceholder;
        }
    }
}
