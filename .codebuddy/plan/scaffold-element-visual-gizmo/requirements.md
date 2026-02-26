# 需求文档：ScaffoldToSceneGenerator 元素可视化 Gizmo

## 引言

当前 `ScaffoldToSceneGenerator.cs` 生成的房间元素（`PlayerSpawn`、`EnemySpawn`、`Checkpoint`、`Wall`、`WallCorner`、`CrateWooden`、`CrateMetal`、`Hazard` 等占位符 GO）在 Scene 视图中是**空的 GameObject**，没有任何视觉表示，导致开发者在 Scene 视图中无法直观区分各元素的位置和类型。

本需求为每个元素 GO 添加：
1. 一个 **`SpriteRenderer`**，使用 Unity 内置白色方块 Sprite + 按类型分配的非白色纯色，作为可视化色块
2. 一个 **`TextMesh`**（世界空间 3D 文字），显示该元素的名称/类型标签，居中显示在色块上方

颜色方案复用 `LevelDesignerWindow.GetElementColor()` 中已有的配色，保持视觉一致性。

---

## 需求

### 需求 1：为每个元素 GO 添加 SpriteRenderer 色块

**用户故事：** 作为一名关卡设计师，我希望生成的每个房间元素在 Scene 视图中都有一个彩色方块，以便我能直观地看到各元素的位置和类型。

#### 验收标准

1. WHEN `ScaffoldToSceneGenerator` 生成任意元素 GO（`PlayerSpawn`、`EnemySpawn`、`Checkpoint`、`Wall`、`WallCorner`、`CrateWooden`、`CrateMetal`、`Hazard`）时，THEN 系统 SHALL 在该 GO 上添加一个 `SpriteRenderer` 组件，使用 Unity 内置的白色方块 Sprite（`"Sprites/Default"` 或 `Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd")`）。
2. WHEN 添加 `SpriteRenderer` 时，THEN 系统 SHALL 根据元素类型分配以下颜色（与 `LevelDesignerWindow` 保持一致）：
   - `PlayerSpawn` → 黄色 `(0.9, 0.8, 0.2, 0.8)`
   - `EnemySpawn` → 红色 `(0.9, 0.3, 0.3, 0.8)`
   - `Checkpoint` → 绿色 `(0.3, 0.9, 0.4, 0.8)`
   - `Wall` → 灰色 `(0.6, 0.6, 0.6, 0.8)`
   - `WallCorner` → 深灰色 `(0.5, 0.5, 0.5, 0.8)`
   - `CrateWooden` → 橙色 `(0.8, 0.5, 0.2, 0.8)`
   - `CrateMetal` → 蓝灰色 `(0.4, 0.4, 0.5, 0.8)`
   - `Hazard` → 紫色 `(0.9, 0.2, 0.8, 0.8)`
   - `Door` → 青色 `(0.3, 0.7, 0.8, 0.8)`
3. WHEN 添加 `SpriteRenderer` 时，THEN 系统 SHALL 将其 `sortingOrder` 设置为 `1`，确保色块显示在 Tilemap 之上。
4. WHEN 添加 `SpriteRenderer` 时，THEN 系统 SHALL 将 GO 的 `localScale` 设置为合理的大小（默认 `(1, 1, 1)`，Wall 类型可设为 `(2, 1, 1)` 以区分方向）。

---

### 需求 2：为每个元素 GO 添加 TextMesh 标签

**用户故事：** 作为一名关卡设计师，我希望每个元素色块上方都有一个文字标签，显示该元素的类型名称，以便我无需点击 GO 就能识别元素类型。

#### 验收标准

1. WHEN `ScaffoldToSceneGenerator` 生成任意元素 GO 时，THEN 系统 SHALL 在该 GO 下创建一个名为 `"Label"` 的子 GO，并添加 `TextMesh` 组件（世界空间 3D 文字，无需 Canvas）。
2. WHEN 添加 `TextMesh` 时，THEN 系统 SHALL 将其 `text` 设置为该元素的类型名称（如 `"PlayerSpawn"`、`"EnemySpawn"` 等），`fontSize` 设置为 `12`，`anchor` 设置为 `TextAnchor.MiddleCenter`，`alignment` 设置为 `TextAlignment.Center`。
3. WHEN 添加 `TextMesh` 时，THEN 系统 SHALL 将 `Label` 子 GO 的 `localPosition` 设置为 `(0, 0.6, 0)`（色块上方），`localScale` 设置为 `(0.1, 0.1, 0.1)` 以适配世界空间大小。
4. WHEN 添加 `TextMesh` 时，THEN 系统 SHALL 将文字颜色设置为白色，并将 `GetComponent<MeshRenderer>().sortingOrder` 设置为 `2`，确保文字显示在色块之上。

---

### 需求 3：Door 元素的生成 GO 也应有可视化标签

**用户故事：** 作为一名关卡设计师，我希望生成的 Door GO（`Door_to_XXX`）也有颜色标识，以便在 Scene 视图中快速定位门的位置。

#### 验收标准

1. WHEN `ScaffoldToSceneGenerator` 在 Phase 4 生成 `Door_to_XXX` GO 时，THEN 系统 SHALL 同样为其添加 `SpriteRenderer`（青色）和 `TextMesh` 标签（显示 `"Door"`）。
2. WHEN 生成 `SpawnPoint_from_XXX` GO 时，THEN 系统 SHALL 为其添加 `SpriteRenderer`（浅蓝色 `(0.5, 0.8, 1.0, 0.6)`）和 `TextMesh` 标签（显示 `"SpawnPt"`），以区分于 Door GO。

---

### 需求 4：可视化 GO 不影响运行时行为

**用户故事：** 作为一名开发者，我希望添加的 SpriteRenderer 和 TextMesh 不会对游戏运行时产生任何副作用。

#### 验收标准

1. WHEN 游戏运行时，THEN `SpriteRenderer` 和 `TextMesh` 组件 SHALL 不影响任何物理碰撞、触发器检测或游戏逻辑。
2. IF 需要在发布版本中隐藏这些可视化元素，THEN 系统 SHALL 支持通过统一的 Layer（如 `"EditorOnly"` 或 `"Ignore Raycast"`）或 Tag 来批量隐藏/显示。
3. WHEN 生成完成后，THEN 系统 SHALL 在 Console 的 TODO Checklist 中提示开发者："可视化 Gizmo GO 仅用于编辑期间，发布前请隐藏或删除"。
