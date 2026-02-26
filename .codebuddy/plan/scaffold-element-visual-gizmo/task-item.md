# 实施计划：ScaffoldToSceneGenerator 元素可视化 Gizmo

- [ ] 1. 新增 `GetElementGizmoColor()` 辅助方法
   - 在 `ScaffoldToSceneGenerator.cs` 中添加一个静态辅助方法，接收 `ScaffoldElementType` 枚举值，返回对应的 `Color`
   - 颜色映射：`PlayerSpawn`→黄、`EnemySpawn`→红、`Checkpoint`→绿、`Wall`→灰、`WallCorner`→深灰、`CrateWooden`→橙、`CrateMetal`→蓝灰、`Hazard`→紫、`Door`→青，其余→白
   - _需求：1.2_

- [ ] 2. 新增 `AddGizmoVisuals()` 辅助方法
   - 在 `ScaffoldToSceneGenerator.cs` 中添加辅助方法 `AddGizmoVisuals(GameObject go, string labelText, Color color)`
   - 方法内：① 在 `go` 上 `AddComponent<SpriteRenderer>()`，赋值内置白色方块 Sprite（`Resources.GetBuiltinResource<Sprite>("Sprites/Default")`），设置 `color` 和 `sortingOrder = 1`
   - ② 创建子 GO `"Label"`，`AddComponent<TextMesh>()`，设置 `text`、`fontSize=12`、`anchor=MiddleCenter`、`alignment=Center`、`color=Color.white`；设置子 GO `localPosition=(0,0.6,0)`、`localScale=(0.1,0.1,0.1)`；设置 `MeshRenderer.sortingOrder = 2`
   - _需求：1.1、1.3、2.1、2.2、2.3、2.4_

- [ ] 3. 修改 `CreateElementGO()` 方法，注入可视化
   - 在现有 `CreateElementGO(string name, Transform parent, Vector3 localPos)` 方法中，增加可选参数 `ScaffoldElementType type` 和 `string labelOverride = null`
   - 方法末尾调用 `AddGizmoVisuals(go, labelOverride ?? type.ToString(), GetElementGizmoColor(type))`
   - 更新 Phase 3 中所有调用 `CreateElementGO` 的地方，传入对应的 `elem.ElementType`
   - _需求：1.1、1.2、1.4、2.1_

- [ ] 4. 为 Phase 4 的 Door / SpawnPoint GO 添加可视化
   - 在 `GenerateDoors()` 方法中，找到创建 `Door_to_XXX` GO 和 `SpawnPoint_from_XXX` GO 的代码
   - 对 `Door_to_XXX` GO 调用 `AddGizmoVisuals(go, "Door", GetElementGizmoColor(ScaffoldElementType.Door))`
   - 对 `SpawnPoint_from_XXX` GO 调用 `AddGizmoVisuals(go, "SpawnPt", new Color(0.5f, 0.8f, 1.0f, 0.6f))`
   - _需求：3.1、3.2_

- [ ] 5. 在生成完成后的 TODO Checklist 中追加提示
   - 在 `Generate()` 方法末尾的 Console 输出（TODO Checklist 部分）中，追加一条提示：`"[ ] Hide/Remove Gizmo visuals (SpriteRenderer + TextMesh Label children) before shipping"`
   - _需求：4.3_
