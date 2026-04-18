# Static Geometry Walls Phase 2 Implementation Plan

> **Status:** 已完成 — 2026-04-16 14:54
>
> **For historical traceability:** 这份 implementation plan 已完成落地；以下 checkbox 仅保留执行轨迹，当前应视为完成态记录。

**Goal:** 为 `Level Architect` 增加静态几何墙画布 starter，并把 `Door` 与 future trap / fall / teleport 的 geometry 验证边界收口到现役 workflow。

**Architecture:** 本计划延续 `scene-backed geometry + starter-first` 路线：新增独立的 `RoomGeometryCanvasFactory` 负责创建 `OuterWalls / InnerWalls` 空 Tilemap 画布，不复用 `LevelRuntimeAssistFactory`。同时在 `LevelArchitectWindow` 中新增 `Geometry Authoring` 区块，并在 `LevelValidator` 中补充 `Door` 的根节点/门洞协作检查，但只对 `Door` 生效，不把 `NarrativeFallTrigger` 等 future transfer 误纳入门洞规则。

**Tech Stack:** Unity 6000.3.7f1、Unity Editor API（`Undo`、`GameObjectUtility`、`SerializedObject`）、Tilemap / 2D Physics、NUnit EditMode tests、Markdown、`dotnet build`

> **Progress Update — 2026-04-16 14:54**
> - `RoomGeometryCanvasFactory`、`LevelValidator`、相关测试与规范文档已经落地，`LevelArchitectWindow` 的 `Geometry Authoring` UI wiring 也已补齐。
> - Unity MCP 会话现已恢复，`Level Architect` 窗口可通过官方菜单正常打开，`EditMode` 测试已跑通（26 / 26 passed）。
> - 已在 `SampleScene` 中补齐 `G_Safe_03` 与 `G_Arena_02` 的 `Navigation/Geometry` authoring 根节点，并修正 `DebugRoom/Navigation/Geometry/OuterWalls/OuterWalls_Main` 的 composite collider 链配置。
> - 当前 `LevelValidator.ValidateAll()` 剩余的阻塞已收敛到非本轮范围问题（如 `Checkpoint_Start` 缺 `CheckpointSO`、`G_Arena_02` 缺 `EncounterSO`），不再是静态几何墙 Phase 2 的代码或 authoring 缺口。

---

## File Structure Map

### Create

- `Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactory.cs` — 静态墙画布 starter authority。只创建空 Tilemap 宿主与标准碰撞链，不生成墙形。
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactoryTests.cs` — geometry canvas starter 的 EditMode 回归测试。
- `Docs/0_Plan/complete/2026-04-15-static-geometry-walls-phase2-implementation-plan.md` — 当前 implementation plan。

### Modify

- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs` — 在单房精修上下文新增独立 `Geometry Authoring` 区块，调用 geometry canvas factory，而不是挤进 `Runtime Assist`。
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs` — 增加 `Door` 的 preferred root 检查与 geometry opening 协作检查；明确 future transfer 不纳入该规则。
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs` — 增加 `Door` / geometry opening / future transfer exclusion 的回归测试。
- `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md` — 固化 `Door` 型通路 vs 非门型跨房间转移的分类边界。
- `Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md` — 同步 `Geometry Authoring` 起手方式、`Door` 开口协作、future transfer 不要求门洞。
- `Docs/5_ImplementationLog/ImplementationLog.md` — 记录本轮 Phase 2 implementation 落地。

### Keep As-Is

- `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactory.cs`
- `Assets/Scripts/Level/Narrative/NarrativeFallTrigger.cs`
- `Assets/Scripts/Level/Room/Door.cs`
- `Assets/Scripts/Level/Room/Room.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomAuthoringHierarchy.cs`

这些文件在本轮**不要扩 scope**：

- `LevelRuntimeAssistFactory` 继续只服务 runtime starter，不吞并静态墙画布入口
- `NarrativeFallTrigger` 继续保持 placeholder，不在本轮被正式工具化
- `Door` 不新增运行时“挖墙”职责
- `Room` 不接管静态墙状态
- `RoomAuthoringHierarchy` 继续只负责骨架，不负责具体 Tilemap 画布内容

---

### Task 1: 先把 geometry canvas starter 的失败测试写出来

**Files:**
- Create: `Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactoryTests.cs`
- Verify: `Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactory.cs`
- Verify: `Assets/Scripts/Level/Editor/LevelArchitect/RoomAuthoringHierarchy.cs`

- [x] **Step 1: 新建 `RoomGeometryCanvasFactoryTests.cs`，先把画布 starter 的最小行为写死**

Write this exact content to `Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactoryTests.cs`:

```csharp
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectArk.Level.Editor
{
    [TestFixture]
    public class RoomGeometryCanvasFactoryTests
    {
        private readonly List<Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void CreateCanvas_CreatesOuterWallCanvasUnderGeometryRootWithStandardColliderChain()
        {
            var room = CreateRoom("Room_Geometry_Canvas_Outer");
            var hierarchy = RoomAuthoringHierarchy.EnsureForRoom(room.transform);

            var created = RoomGeometryCanvasFactory.CreateCanvas(
                room.GetComponent<Room>(),
                RoomGeometryCanvasFactory.WallCanvasKind.OuterWalls);

            Assert.That(created, Is.Not.Null);
            Assert.That(created.transform.parent, Is.SameAs(hierarchy.OuterWallsRoot));
            Assert.That(created.name, Is.EqualTo("OuterWalls_Main"));
            Assert.That(created.GetComponent<Tilemap>(), Is.Not.Null);
            Assert.That(created.GetComponent<TilemapRenderer>(), Is.Not.Null);
            Assert.That(created.GetComponent<TilemapCollider2D>(), Is.Not.Null);
            Assert.That(created.GetComponent<Rigidbody2D>(), Is.Not.Null);
            Assert.That(created.GetComponent<CompositeCollider2D>(), Is.Not.Null);
            Assert.That(created.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Static));
            Assert.That(created.GetComponent<TilemapCollider2D>().compositeOperation, Is.Not.EqualTo(Collider2D.CompositeOperation.None));
        }

        [Test]
        public void CreateCanvas_CreatesUniqueInnerWallCanvasNamesOnRepeatedCalls()
        {
            var room = CreateRoom("Room_Geometry_Canvas_Inner");
            var hierarchy = RoomAuthoringHierarchy.EnsureForRoom(room.transform);

            var first = RoomGeometryCanvasFactory.CreateCanvas(
                room.GetComponent<Room>(),
                RoomGeometryCanvasFactory.WallCanvasKind.InnerWalls);
            var second = RoomGeometryCanvasFactory.CreateCanvas(
                room.GetComponent<Room>(),
                RoomGeometryCanvasFactory.WallCanvasKind.InnerWalls);

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(first.transform.parent, Is.SameAs(hierarchy.InnerWallsRoot));
            Assert.That(second.transform.parent, Is.SameAs(hierarchy.InnerWallsRoot));
            Assert.That(first.name, Is.EqualTo("InnerWalls_Main"));
            Assert.That(second.name, Is.Not.EqualTo(first.name));
        }

        private GameObject CreateRoom(string roomId)
        {
            var roomObject = new GameObject(roomId);
            _createdObjects.Add(roomObject);

            var room = roomObject.AddComponent<Room>();
            var box = roomObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(20f, 12f);

            var roomData = ScriptableObject.CreateInstance<RoomSO>();
            roomData.name = $"{roomId}_Data";
            _createdObjects.Add(roomData);

            SetPrivateField(roomData, "_roomID", roomId);
            SetPrivateField(roomData, "_displayName", roomId);
            SetPrivateField(room, "_data", roomData);

            return roomObject;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            Assert.Fail($"Could not find private field '{fieldName}' on {target.GetType().Name}.");
        }
    }
}
```

- [x] **Step 2: 运行这组 EditMode tests，确认它们现在会失败**

Run in Unity Editor:

- 打开 `Window > General > Test Runner`
- 切到 `EditMode`
- 运行 `RoomGeometryCanvasFactoryTests`

Expected:

- 编译或测试失败
- 报错应指向缺失的 `RoomGeometryCanvasFactory`
- 这是预期结果，说明 geometry canvas starter 的行为已经被测试锁住

- [x] **Step 3: Commit**

```bash
git add Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactoryTests.cs
git commit -m "test: add geometry canvas starter expectations"
```

---

### Task 2: 实现独立 `RoomGeometryCanvasFactory`，只负责空 Tilemap 画布

**Files:**
- Create: `Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactory.cs`
- Test: `Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactoryTests.cs`

- [x] **Step 1: 新建 `RoomGeometryCanvasFactory.cs`，把画布 starter authority 从 runtime assist 中分离出来**

Write this exact content to `Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactory.cs`:

```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Creates blank Tilemap canvases for scene-backed static wall authoring.
    /// This is not a runtime assist factory: it only prepares the geometry host
    /// and the standard collision chain under Navigation/Geometry.
    /// </summary>
    public static class RoomGeometryCanvasFactory
    {
        public enum WallCanvasKind
        {
            OuterWalls,
            InnerWalls
        }

        public static GameObject CreateCanvas(Room room, WallCanvasKind kind)
        {
            if (room == null)
            {
                Debug.LogWarning("[RoomGeometryCanvasFactory] Cannot create canvas: room is null.");
                return null;
            }

            var hierarchy = RoomAuthoringHierarchy.EnsureForRoom(room.transform);
            Transform parent = kind == WallCanvasKind.OuterWalls ? hierarchy.OuterWallsRoot : hierarchy.InnerWallsRoot;
            string baseName = kind == WallCanvasKind.OuterWalls ? "OuterWalls_Main" : "InnerWalls_Main";
            string objectName = GameObjectUtility.GetUniqueNameForSibling(parent, baseName);

            var canvas = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(canvas, $"Create {objectName}");
            canvas.transform.SetParent(parent, false);
            canvas.transform.position = room.transform.position;

            Undo.AddComponent<Tilemap>(canvas);
            var renderer = Undo.AddComponent<TilemapRenderer>(canvas);
            var tilemapCollider = Undo.AddComponent<TilemapCollider2D>(canvas);
            var rigidbody = Undo.AddComponent<Rigidbody2D>(canvas);
            Undo.AddComponent<CompositeCollider2D>(canvas);

            Undo.RecordObject(renderer, "Configure TilemapRenderer");
            renderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;

            Undo.RecordObject(rigidbody, "Configure Static Rigidbody2D");
            rigidbody.bodyType = RigidbodyType2D.Static;
            rigidbody.simulated = true;

            Undo.RecordObject(tilemapCollider, "Configure TilemapCollider2D");
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            Selection.activeGameObject = canvas;
            SceneView.lastActiveSceneView?.FrameSelected();
            SceneView.RepaintAll();
            EditorUtility.SetDirty(canvas);

            Debug.Log($"[RoomGeometryCanvasFactory] Created {kind} canvas '{objectName}' in room '{room.RoomID}'. Paint tiles manually to author wall geometry.");
            return canvas;
        }

        public static string GetDisplayName(WallCanvasKind kind)
        {
            return kind switch
            {
                WallCanvasKind.OuterWalls => "Create Outer Wall Canvas",
                WallCanvasKind.InnerWalls => "Create Inner Wall Canvas",
                _ => kind.ToString()
            };
        }
    }
}
```

- [x] **Step 2: 再跑 `RoomGeometryCanvasFactoryTests`，确认 starter 行为通过**

Run in Unity Editor:

- `Window > General > Test Runner`
- `EditMode`
- 运行 `RoomGeometryCanvasFactoryTests`

Expected:

- 两个测试都通过
- 创建对象位于 `Navigation/Geometry/OuterWalls` 或 `Navigation/Geometry/InnerWalls`
- 新画布具备 `Tilemap + TilemapRenderer + TilemapCollider2D + Rigidbody2D(Static) + CompositeCollider2D`

- [x] **Step 3: Commit**

```bash
git add Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactory.cs Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactoryTests.cs
git commit -m "feat: add static wall canvas starter"
```

---

### Task 3: 在 `Level Architect` 暴露独立 `Geometry Authoring` 区块，而不是复用 `Runtime Assist`

**Files:**
- Modify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs`
- Verify: `Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactory.cs`

- [x] **Step 1: 给单房精修上下文增加 `Geometry Authoring` 区块**

In `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs`, update the single-room entry points and add a dedicated geometry section.

First, in `DrawSingleRoomSelectionSummary(Room room)`, insert this block right before `EditorGUILayout.EndVertical();`:

```csharp
            GUILayout.Space(6f);
            DrawRoomGeometryAuthoringSection(room, true);
```

Then, in `DrawSingleRoomInfo(Room room, bool includeQuickEditSections)`, replace the `includeQuickEditSections` block with:

```csharp
            if (includeQuickEditSections)
            {
                DrawConnectionInspector(room, doors);
                DrawRoomGeometryAuthoringSection(room, true);
                DrawRoomRuntimeAssistSection(room, true);
            }
```

Finally, add these two methods near the existing `CreateRoomRuntimeAssist(...)` helper:

```csharp
        private void DrawRoomGeometryAuthoringSection(Room room, bool compact)
        {
            if (room == null)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope("HelpBox"))
            {
                EditorGUILayout.LabelField(compact ? "Geometry Authoring" : "Static Geometry Authoring", compact ? EditorStyles.miniBoldLabel : EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Room: {GetRoomListLabel(room)}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(
                    "创建空 Tilemap 画布用于 scene-backed static wall authoring。它不是 Runtime Assist，不会生成墙形，只会准备标准 Geometry 宿主与碰撞链。",
                    EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField(
                    "OuterWalls 用于主外轮廓；InnerWalls 用于房内阻挡与分隔。画布创建后请手工绘制 Tilemap，并用 Validate All 检查结构。",
                    EditorStyles.wordWrappedMiniLabel);

                float buttonHeight = compact ? 20f : 22f;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(RoomGeometryCanvasFactory.GetDisplayName(RoomGeometryCanvasFactory.WallCanvasKind.OuterWalls), GUILayout.Height(buttonHeight)))
                    {
                        CreateRoomGeometryCanvas(room, RoomGeometryCanvasFactory.WallCanvasKind.OuterWalls);
                        return;
                    }

                    if (GUILayout.Button(RoomGeometryCanvasFactory.GetDisplayName(RoomGeometryCanvasFactory.WallCanvasKind.InnerWalls), GUILayout.Height(buttonHeight)))
                    {
                        CreateRoomGeometryCanvas(room, RoomGeometryCanvasFactory.WallCanvasKind.InnerWalls);
                        return;
                    }
                }
            }
        }

        private void CreateRoomGeometryCanvas(Room room, RoomGeometryCanvasFactory.WallCanvasKind kind)
        {
            if (room == null)
            {
                return;
            }

            TrackRecentRoom(room);
            var createdObject = RoomGeometryCanvasFactory.CreateCanvas(room, kind);
            if (createdObject == null)
            {
                return;
            }

            _selectedRooms.Clear();
            _selectedRooms.Add(room);
            _selectedConnection = null;
            Repaint();
            SceneView.RepaintAll();
        }
```

- [x] **Step 2: 编译检查 `Level Architect` UI 改动不破坏现有 Editor 程序集**

Run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx
```

Expected:

- `Build succeeded.`
- `ProjectArk.Level.Editor` 能通过编译
- 不应出现 `RoomGeometryCanvasFactory`、`DrawRoomGeometryAuthoringSection`、`CreateRoomGeometryCanvas` 的未定义错误

- [x] **Step 3: 在 Unity Editor 里做一次最小 UI 冒烟**

Run in Unity Editor:

- 打开 `ProjectArk > Level > Authority > Level Architect`
- 在 `Build` 里建一个房间，或选中已有单房
- 切到 `Quick Edit`
- 单选该房间
- 在选择摘要或单房精修区看到 `Geometry Authoring`
- 点 `Create Outer Wall Canvas`

Expected:

- 新对象出现在 `Navigation/Geometry/OuterWalls`
- 新对象被自动选中
- 对象名为 `OuterWalls_Main`（重复创建时自动变成唯一名称）
- 该区块的文案明确说明“这不是 Runtime Assist”

- [x] **Step 4: Commit**

```bash
git add Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactory.cs
git commit -m "feat: expose geometry authoring in level architect"
```

---

### Task 4: 先把 `Door` / geometry opening / future transfer exclusion 的失败测试写出来

**Files:**
- Modify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`
- Verify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`
- Verify: `Assets/Scripts/Level/Narrative/NarrativeFallTrigger.cs`

- [x] **Step 1: 在 `LevelValidatorTests.cs` 里补三条新的边界测试**

Append these tests near the existing geometry validation tests in `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`:

```csharp
        [Test]
        public void ValidateAll_ReportsWarning_WhenDoorPlacedOutsideNavigationRoot()
        {
            var roomRig = CreateValidRoomRig("Room_Door_WrongRoot");
            var targetRig = CreateValidRoomRig("Room_Door_Target");
            var doorObject = CreateGameObjectWithTriggerColliderAndComponent<Door>("Door_WrongRoot");
            doorObject.transform.SetParent(roomRig.ElementsRoot, false);
            SetPrivateField(doorObject.GetComponent<Door>(), "_targetRoom", targetRig.Room);
            SetPrivateField(doorObject.GetComponent<Door>(), "_targetSpawnPoint", CreateChild(targetRig.NavigationRoot, "Spawn_Target"));
            SetPrivateField(doorObject.GetComponent<Door>(), "_playerLayer", (LayerMask)1);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == doorObject.GetComponent<Door>() &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("Door") &&
                result.Message.Contains("Navigation")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenDoorCenterSitsInsideFilledOuterWallTile()
        {
            var roomRig = CreateValidRoomRig("Room_Door_FilledWall");
            var targetRig = CreateValidRoomRig("Room_Door_TargetWall");
            var outerWall = CreateChild(roomRig.OuterWallsRoot, "OuterWalls_Main");
            var tilemap = outerWall.gameObject.AddComponent<Tilemap>();
            outerWall.gameObject.AddComponent<TilemapRenderer>();
            var tilemapCollider = outerWall.gameObject.AddComponent<TilemapCollider2D>();
            outerWall.gameObject.AddComponent<CompositeCollider2D>();
            var rigidbody = outerWall.gameObject.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Static;
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            var tile = ScriptableObject.CreateInstance<Tile>();
            _createdObjects.Add(tile);
            tilemap.SetTile(Vector3Int.zero, tile);

            var doorRoot = roomRig.NavigationRoot.Find(RoomAuthoringHierarchy.DoorsRootName);
            Assert.That(doorRoot, Is.Not.Null);

            var doorObject = CreateGameObjectWithTriggerColliderAndComponent<Door>("Door_FilledWall");
            doorObject.transform.SetParent(doorRoot, false);
            doorObject.transform.position = Vector3.zero;
            SetPrivateField(doorObject.GetComponent<Door>(), "_targetRoom", targetRig.Room);
            SetPrivateField(doorObject.GetComponent<Door>(), "_targetSpawnPoint", CreateChild(targetRig.NavigationRoot, "Spawn_TargetWall"));
            SetPrivateField(doorObject.GetComponent<Door>(), "_playerLayer", (LayerMask)1);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == doorObject.GetComponent<Door>() &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("geometry opening")));
        }

        [Test]
        public void ValidateAll_DoesNotReportDoorOpeningWarning_ForNarrativeFallTrigger()
        {
            var roomRig = CreateValidRoomRig("Room_FallTrigger_Boundary");
            var targetRig = CreateValidRoomRig("Room_FallTrigger_Target");
            var outerWall = CreateChild(roomRig.OuterWallsRoot, "OuterWalls_Main");
            var tilemap = outerWall.gameObject.AddComponent<Tilemap>();
            outerWall.gameObject.AddComponent<TilemapRenderer>();
            outerWall.gameObject.AddComponent<TilemapCollider2D>();
            var tile = ScriptableObject.CreateInstance<Tile>();
            _createdObjects.Add(tile);
            tilemap.SetTile(Vector3Int.zero, tile);

            var triggerObject = CreateGameObjectWithTriggerColliderAndComponent<NarrativeFallTrigger>("NarrativeFall_Boundary");
            triggerObject.transform.SetParent(roomRig.TriggersRoot, false);
            triggerObject.transform.position = Vector3.zero;
            SetPrivateField(triggerObject.GetComponent<NarrativeFallTrigger>(), "_targetRoom", targetRig.Room);
            SetPrivateField(triggerObject.GetComponent<NarrativeFallTrigger>(), "_landingPoint", CreateChild(targetRig.NavigationRoot, "Landing_Target"));
            SetPrivateField(triggerObject.GetComponent<NarrativeFallTrigger>(), "_playerLayer", (LayerMask)1);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == triggerObject.GetComponent<NarrativeFallTrigger>() &&
                result.Message.Contains("geometry opening")), Is.False);
        }
```

- [x] **Step 2: 运行 `LevelValidatorTests`，确认新规则现在还没实现所以会失败**

Run in Unity Editor:

- `Window > General > Test Runner`
- `EditMode`
- 运行 `LevelValidatorTests`

Expected:

- 新增的 door / geometry opening 边界测试失败
- 错误点应集中在：`Door` 没有 preferred root warning，且 door-center-in-wall 还没有 geometry opening warning
- `NarrativeFallTrigger` exclusion 测试应在实现前作为红灯存在（因为 warning 文案/规则尚未加进去）

- [x] **Step 3: Commit**

```bash
git add Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs
git commit -m "test: add door geometry boundary coverage"
```

---

### Task 5: 在 `LevelValidator` 中只对 `Door` 增加根节点与 geometry opening 协作检查

**Files:**
- Modify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`
- Test: `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`

- [x] **Step 1: 先把 `Door` 纳入 preferred root 检查**

In `ValidatePreferredAuthoringRoots()`, add this line before `ValidatePreferredRoot<ActivationGroup>(...)`:

```csharp
            ValidatePreferredRoot<Door>("Door", "Navigation");
```

- [x] **Step 2: 在 door validator 主链里增加 geometry opening 协作检查，但只作用于 `Door`**

In `ValidateAll()`, insert this call right after `ValidateDoorTargetSpawnPoint(rooms);`:

```csharp
            ValidateDoorGeometryAlignment(rooms);
```

Then add these helper methods below `ValidateDoorTargetSpawnPoint(...)` in `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`:

```csharp
        private static void ValidateDoorGeometryAlignment(Room[] rooms)
        {
            foreach (var room in rooms)
            {
                if (room == null) continue;

                var geometryRoot = room.transform.Find($"{RoomAuthoringHierarchy.NavigationRootName}/{RoomAuthoringHierarchy.GeometryRootName}");
                if (geometryRoot == null)
                {
                    continue;
                }

                var wallTilemaps = geometryRoot.GetComponentsInChildren<Tilemap>(true);
                if (wallTilemaps.Length == 0)
                {
                    continue;
                }

                var doors = room.GetComponentsInChildren<Door>(true);
                foreach (var door in doors)
                {
                    if (door == null) continue;
                    if (!TryGetImmediateRoomRoot(door.transform, out Room ownerRoom, out string rootName)) continue;
                    if (ownerRoom != room) continue;
                    if (!string.Equals(rootName, RoomAuthoringHierarchy.NavigationRootName, StringComparison.Ordinal)) continue;

                    Vector3 probePoint = TryGetDoorProbePoint(door);
                    if (!DoorProbeHitsFilledWallTile(wallTilemaps, probePoint))
                    {
                        continue;
                    }

                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"Door '{door.gameObject.name}' in Room '{room.RoomID}' overlaps filled wall geometry. Door-type paths should align with a geometry opening instead of sitting inside a filled wall tilemap.",
                        TargetObject = door,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }
            }
        }

        private static Vector3 TryGetDoorProbePoint(Door door)
        {
            var collider = door.GetComponent<Collider2D>();
            return collider != null ? collider.bounds.center : door.transform.position;
        }

        private static bool DoorProbeHitsFilledWallTile(Tilemap[] tilemaps, Vector3 worldPoint)
        {
            foreach (var tilemap in tilemaps)
            {
                if (tilemap == null)
                {
                    continue;
                }

                var cell = tilemap.WorldToCell(worldPoint);
                if (tilemap.GetTile(cell) != null)
                {
                    return true;
                }
            }

            return false;
        }
```

This keeps the rule intentionally conservative:

- 只看 `Door`
- 只看 `Navigation` 下的 `Door`
- 只检查 `Navigation/Geometry` 里的 Tilemap
- 不扫描 `NarrativeFallTrigger`、hazard trigger、world event trigger 等 future transfer 元素

- [x] **Step 3: 重新跑 validator tests，并做一次整体编译**

Run in Unity Editor:

- `Window > General > Test Runner`
- `EditMode`
- 运行 `LevelValidatorTests`

Expected:

- 新增的 3 条测试通过
- 旧的 geometry / authoring root 测试仍通过

Then run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx
```

Expected:

- `Build succeeded.`
- `LevelValidator` 没有新增命名、类型或 `using` 错误

- [x] **Step 4: Commit**

```bash
git add Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs
git commit -m "feat: harden door geometry validation boundaries"
```

---

### Task 6: 同步文档口径，并跑一轮代表房间 authoring 验收

**Files:**
- Modify: `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md`
- Modify: `Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md`
- Modify: `Docs/5_ImplementationLog/ImplementationLog.md`

- [x] **Step 1: 在 `Level_CanonicalSpec.md` 固化 `Door` vs future transfer 的分类边界**

Under the room element classification section in `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md`, add this exact note after the `Path / Environment / Directing` table:

```markdown
### 8.5.1 `Door` 型通路与非门型跨房间转移的边界

- `Door`、层间门、阶段门仍属于 **`Path`**，默认挂在 `Navigation`，并表达“玩家从这里穿过去”的导航语义。
- `Door` 型通路若与静态墙协作，应通过 `Navigation/Geometry` 中 author 的开口达成；`Door` 本体不负责运行时挖墙。
- 掉落陷阱、叙事坠落、特殊 teleport 等 future transfer，即使跨 room，也不自动归类为 `Door`。
- 这类 future transfer 应按其真实语义落位到 `Hazards` 或 `Triggers` 等家族，并且**不强制要求墙体开口**。
```

- [x] **Step 2: 在 `Level_WorkflowSpec.md` 补齐 geometry starter 与 boundary case 的现役操作口径**

In `Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md`, update the static geometry section with this exact block:

```markdown
- `Quick Edit` 的单房精修上下文应提供独立的 `Geometry Authoring` 区块，用于创建 `OuterWalls` / `InnerWalls` 的空 Tilemap 画布；它不是 `Runtime Assist`，不会替作者生成最终墙形。
- `Door` 型通路需要与静态墙开口协作：几何开口由 `Navigation/Geometry` author，连接语义由 `Door` author。
- future trap / fall / teleport 等非门型跨房间转移可以跨 room，但**不要求**墙体开口，也不应被 validator 误报为门洞问题。
```

- [x] **Step 3: 在 Unity Editor 里跑代表房间 authoring 验收，确认规则真实可用**

Run in Unity Editor:

1. 选一个普通 transit / combat 房间，点击 `Create Outer Wall Canvas`
2. 在 `OuterWalls_Main` 上手工刷一段 Tilemap 外墙
3. 留一个门洞，把 `Door` 对齐到开口
4. 执行 `Validate All`
5. 再故意复制一个 Door，把它移动到填满 tile 的墙中心
6. 再执行一次 `Validate All`
7. 额外创建一个 future boundary case：在 `Triggers` 下放一个 `NarrativeFallTrigger` 占位对象，不开墙洞
8. 再执行一次 `Validate All`

Expected:

- 正常对齐开口的 `Door` 不报 geometry opening warning
- 塞进实体墙中心的 `Door` 会报 geometry opening warning
- `NarrativeFallTrigger` 不会因为“没有门洞”被报错
- 整个流程仍然保持：geometry 走独立 starter，runtime assist 走独立 starter

- [x] **Step 4: 追加实现日志，并提交文档收口**

At the top of `Docs/5_ImplementationLog/ImplementationLog.md`, prepend a new entry using the actual execution time and this content:

```markdown
## 新增静态几何墙 Phase 2 画布 starter，并收口 Door / future transfer 边界 — YYYY-MM-DD HH:MM

### 修改文件
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactory.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactoryTests.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`
- `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md`
- `Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md`
- `Docs/5_ImplementationLog/ImplementationLog.md`

### 内容
- 新增静态几何墙 `Geometry Authoring` 画布 starter，使作者可直接在 `Level Architect` 中创建 `OuterWalls / InnerWalls` 的标准 Tilemap 宿主与碰撞链。
- 将该入口与 `Runtime Assist` 明确分离，避免静态墙 authoring 与 runtime starter 语义混淆。
- 在 `LevelValidator` 中新增 `Door` 的 preferred root 与 geometry opening 协作检查，同时明确排除 future trap / fall / teleport 类跨房间转移的误报。
- 同步更新 `Level` 规范与 workflow 文档，固化 `Door` 型通路与非门型跨房间转移的边界。

### 目的
- 让静态墙的起手 authoring 更顺手，同时防止后续掉落陷阱、叙事坠落、特殊传送被错误套入门洞协作规则。
- 把 `Door` / geometry / validator / workflow 的边界收口到一致口径，降低后续扩展墙家族与 future transfer 时的耦合风险。

### 技术
- 使用独立 editor factory + `starter-first` 策略创建 geometry canvas，不引入自动墙形生成。
- 使用保守的 door-center-to-tile 检查作为 geometry opening 护栏，并将校验范围限定在 `Door` + `Navigation/Geometry` 主链内。
```

Replace `YYYY-MM-DD HH:MM` with the actual current time when you execute the task.

Then commit:

```bash
git add Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactory.cs Assets/Scripts/Level/Editor/LevelArchitect/RoomGeometryCanvasFactoryTests.cs Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md Docs/5_ImplementationLog/ImplementationLog.md
git commit -m "feat: add static geometry phase 2 authoring guards"
```

---

## Self-Review Checklist

在开始执行前，先自己过一遍这 5 条：

1. `RoomGeometryCanvasFactory` 是否保持了“只建空画布、不生成墙形”的边界？
2. `LevelArchitectWindow` 是否把 geometry 入口和 `Runtime Assist` 文案明确分开？
3. `LevelValidator` 是否只对 `Door` 做 opening 协作检查，而不是扫描所有 trigger？
4. `NarrativeFallTrigger` exclusion 测试是否真的覆盖了“不要求墙体开口”的 future boundary case？
5. 文档是否同时更新了 `CanonicalSpec` 与 `WorkflowSpec`，避免工具行为和规范口径漂移？

---

## Completion Note

本计划涉及的代码、测试、`Level Architect` UI 接线、`Door` / future transfer validator 边界，以及 Unity Editor 代表房间 authoring 验收均已完成。

收尾说明：

- `Geometry Authoring` 画布 starter 已落地，并与 `Runtime Assist` 保持独立 authority
- `LevelValidator` 已只对 `Door` 执行 geometry opening 协作检查，不会把 `NarrativeFallTrigger` 等 future transfer 误判为门洞问题
- Unity 会话内 `EditMode` tests 已通过，`SampleScene` 中与本计划直接相关的 geometry authoring 脏数据已完成修复
- 剩余 `CheckpointSO` / `EncounterSO` 等问题已明确收敛为**非本计划范围**

**Plan complete and archived to** `Docs/0_Plan/complete/2026-04-15-static-geometry-walls-phase2-implementation-plan.md`。
