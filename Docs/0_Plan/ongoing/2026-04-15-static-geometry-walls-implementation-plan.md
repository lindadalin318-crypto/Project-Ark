# Static Geometry Walls Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 `静态几何墙` 的 spec 落成第一版可执行闭环：新建房间自动带出 `Navigation/Geometry/OuterWalls/InnerWalls` 骨架，`LevelValidator` 能抓最关键的 geometry authoring 错误，并把这条 authoring 语言写回现役 workflow 文档。

**Architecture:** 这份计划只实现静态几何墙的 **MVP authoring 闭环**，不做自动画墙、不做玩法墙、不做运行时墙系统。实现方式是新增一个超轻 `RoomGeometryRoot` marker、用一个共享的 editor helper 统一 `RoomFactory` 与 `LevelSliceBuilder` 的 room skeleton 创建，再在 `LevelValidator` 中补 geometry 结构与外轮廓碰撞链规则，并用 EditMode tests 锁住骨架与 validator 行为。

**Tech Stack:** Unity 6000.3.7f1 / 6000.4.x、C#、Unity Editor API（`Undo`、`SerializedObject`）、NUnit EditMode tests、Tilemap/2D Physics、Markdown、`dotnet build`

---

## Scope Split Note

这份 implementation plan **只覆盖**静态几何墙 spec 的第一阶段，也就是：

- `Navigation/Geometry` authoring 骨架
- `OuterWalls / InnerWalls` 结构约定
- `RoomGeometryRoot` marker
- `LevelValidator` 的 geometry 护栏
- 现役 workflow 文档同步

**明确不在本计划内：**

- `BreakableWall`
- `PhaseBarrier`
- `ProjectileBarrier`
- 自动刷整圈外轮廓 / 自动门洞 / 墙厚推断
- `Level Architect` 新增独立 Geometry starter 按钮
- 基于 JSON schema 的墙形 authoring

---

## File Structure Map

### Create

- `Assets/Scripts/Level/Room/RoomGeometryRoot.cs` — 超轻 marker component，挂在 `Navigation/Geometry`，只作为 validator / tooling 锚点，不参与 runtime 主链。
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomAuthoringHierarchy.cs` — 统一创建和确保标准房间骨架（含 `Geometry / OuterWalls / InnerWalls`）的 editor helper，供 `RoomFactory` 和 `LevelSliceBuilder` 复用。
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomAuthoringHierarchyTests.cs` — geometry 骨架 helper 的 regression tests，验证创建结果与幂等性。
- `Docs/0_Plan/ongoing/2026-04-15-static-geometry-walls-implementation-plan.md` — 当前 implementation plan。

### Modify

- `Assets/Scripts/Level/Editor/LevelArchitect/RoomFactory.cs` — 改为通过共享 helper 创建标准房间骨架，不再手写第二套 hierarchy。
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelSliceBuilder.cs` — JSON 导入的房间骨架切到同一套 helper，避免 `RoomFactory` / JSON import 漂移。
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs` — 增加 `Navigation/Geometry`、`RoomGeometryRoot`、`OuterWalls` 碰撞链和命名误挂点校验。
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs` — 增加 geometry validator regression tests，并把现有 `CreateValidRoomRig()` 升级到新骨架。
- `Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md` — 补入新的静态几何墙 authoring 路径说明。
- `Docs/5_ImplementationLog/ImplementationLog.md` — 记录本轮静态几何墙 MVP authoring 落地。

### Keep As-Is

- `Assets/Scripts/Level/Room/Room.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactory.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/DoorWiringService.cs`
- `Docs/0_Plan/specs/2026-04-15-static-geometry-walls-design.md`

这些文件在本轮里**不要额外扩 scope**：

- `Room` 不接管静态墙状态
- `LevelArchitectWindow` 不新增 Geometry 专属 UI
- `LevelRuntimeAssistFactory` 不扩成墙生成器
- `DoorWiringService` 不被绑到 Geometry 自动修复上

---

### Task 1: 先把 geometry room skeleton helper 的失败测试写出来

**Files:**
- Create: `Assets/Scripts/Level/Editor/LevelArchitect/RoomAuthoringHierarchyTests.cs`
- Verify: `Assets/Scripts/Level/Editor/LevelArchitect/RoomAuthoringHierarchy.cs`
- Verify: `Assets/Scripts/Level/Room/RoomGeometryRoot.cs`

- [ ] **Step 1: 新建 `RoomAuthoringHierarchyTests.cs`，先把 geometry 骨架的最低期望写死**

Write this exact content to `Assets/Scripts/Level/Editor/LevelArchitect/RoomAuthoringHierarchyTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    [TestFixture]
    public class RoomAuthoringHierarchyTests
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
        public void EnsureForRoom_CreatesGeometryRootsAndMarker()
        {
            var roomObject = new GameObject("Room_Geometry_Test");
            _createdObjects.Add(roomObject);

            var hierarchy = RoomAuthoringHierarchy.EnsureForRoom(roomObject.transform);

            Assert.That(hierarchy.NavigationRoot, Is.Not.Null);
            Assert.That(hierarchy.GeometryRoot, Is.Not.Null);
            Assert.That(hierarchy.OuterWallsRoot, Is.Not.Null);
            Assert.That(hierarchy.InnerWallsRoot, Is.Not.Null);
            Assert.That(roomObject.transform.Find("Navigation/Geometry"), Is.Not.Null);
            Assert.That(roomObject.transform.Find("Navigation/Geometry/OuterWalls"), Is.Not.Null);
            Assert.That(roomObject.transform.Find("Navigation/Geometry/InnerWalls"), Is.Not.Null);
            Assert.That(hierarchy.GeometryRoot.GetComponent<RoomGeometryRoot>(), Is.Not.Null);
        }

        [Test]
        public void EnsureForRoom_IsIdempotentAndDoesNotDuplicateGeometryRoots()
        {
            var roomObject = new GameObject("Room_Geometry_Idempotent");
            _createdObjects.Add(roomObject);

            var first = RoomAuthoringHierarchy.EnsureForRoom(roomObject.transform);
            var second = RoomAuthoringHierarchy.EnsureForRoom(roomObject.transform);

            Assert.That(first.NavigationRoot, Is.SameAs(second.NavigationRoot));
            Assert.That(first.GeometryRoot, Is.SameAs(second.GeometryRoot));
            Assert.That(first.OuterWallsRoot, Is.SameAs(second.OuterWallsRoot));
            Assert.That(first.InnerWallsRoot, Is.SameAs(second.InnerWallsRoot));
            Assert.That(roomObject.transform.Find("Navigation").GetComponentsInChildren<RoomGeometryRoot>(true).Length, Is.EqualTo(1));
        }
    }
}
```

- [ ] **Step 2: 先运行这组 EditMode tests，确认它们现在会失败**

Run in Unity Editor:

- 打开 `Window > General > Test Runner`
- 切到 `EditMode`
- 运行 `RoomAuthoringHierarchyTests`

Expected:

- 编译或测试失败
- 报错应指向缺失的 `RoomAuthoringHierarchy` 和/或 `RoomGeometryRoot`
- 这是预期结果，说明我们真的把 geometry skeleton 的新约束写成了测试

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Level/Editor/LevelArchitect/RoomAuthoringHierarchyTests.cs
git commit -m "test: add room geometry hierarchy expectations"
```

---

### Task 2: 实现 `RoomGeometryRoot` 与统一 room skeleton helper，并接进 `RoomFactory / LevelSliceBuilder`

**Files:**
- Create: `Assets/Scripts/Level/Room/RoomGeometryRoot.cs`
- Create: `Assets/Scripts/Level/Editor/LevelArchitect/RoomAuthoringHierarchy.cs`
- Modify: `Assets/Scripts/Level/Editor/LevelArchitect/RoomFactory.cs`
- Modify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelSliceBuilder.cs`
- Test: `Assets/Scripts/Level/Editor/LevelArchitect/RoomAuthoringHierarchyTests.cs`

- [ ] **Step 1: 新建超轻 marker `RoomGeometryRoot.cs`**

Write this exact content to `Assets/Scripts/Level/Room/RoomGeometryRoot.cs`:

```csharp
using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Marker component for a Room's static geometry authoring root.
    /// Lives on `Navigation/Geometry` and provides a stable validator/tooling anchor.
    /// Does not own runtime wall state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomGeometryRoot : MonoBehaviour
    {
    }
}
```

- [ ] **Step 2: 新建共享 helper `RoomAuthoringHierarchy.cs`，收口标准 room hierarchy authority**

Write this exact content to `Assets/Scripts/Level/Editor/LevelArchitect/RoomAuthoringHierarchy.cs`:

```csharp
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Shared editor authority for creating and repairing the standard Room authoring hierarchy.
    /// Keeps `RoomFactory` and `LevelSliceBuilder` on the same structure contract.
    /// </summary>
    public static class RoomAuthoringHierarchy
    {
        public const string NavigationRootName = "Navigation";
        public const string ElementsRootName = "Elements";
        public const string EncountersRootName = "Encounters";
        public const string HazardsRootName = "Hazards";
        public const string DecorationRootName = "Decoration";
        public const string TriggersRootName = "Triggers";
        public const string DoorsRootName = "Doors";
        public const string SpawnPointsRootName = "SpawnPoints";
        public const string GeometryRootName = "Geometry";
        public const string OuterWallsRootName = "OuterWalls";
        public const string InnerWallsRootName = "InnerWalls";

        public readonly struct RoomHierarchyRefs
        {
            public RoomHierarchyRefs(
                Transform navigationRoot,
                Transform elementsRoot,
                Transform encountersRoot,
                Transform hazardsRoot,
                Transform decorationRoot,
                Transform triggersRoot,
                Transform doorsRoot,
                Transform navigationSpawnPointsRoot,
                Transform geometryRoot,
                Transform outerWallsRoot,
                Transform innerWallsRoot)
            {
                NavigationRoot = navigationRoot;
                ElementsRoot = elementsRoot;
                EncountersRoot = encountersRoot;
                HazardsRoot = hazardsRoot;
                DecorationRoot = decorationRoot;
                TriggersRoot = triggersRoot;
                DoorsRoot = doorsRoot;
                NavigationSpawnPointsRoot = navigationSpawnPointsRoot;
                GeometryRoot = geometryRoot;
                OuterWallsRoot = outerWallsRoot;
                InnerWallsRoot = innerWallsRoot;
            }

            public Transform NavigationRoot { get; }
            public Transform ElementsRoot { get; }
            public Transform EncountersRoot { get; }
            public Transform HazardsRoot { get; }
            public Transform DecorationRoot { get; }
            public Transform TriggersRoot { get; }
            public Transform DoorsRoot { get; }
            public Transform NavigationSpawnPointsRoot { get; }
            public Transform GeometryRoot { get; }
            public Transform OuterWallsRoot { get; }
            public Transform InnerWallsRoot { get; }
        }

        public static RoomHierarchyRefs EnsureForRoom(Transform roomRoot)
        {
            var navigationRoot = EnsureChild(roomRoot, NavigationRootName);
            var elementsRoot = EnsureChild(roomRoot, ElementsRootName);
            var encountersRoot = EnsureChild(roomRoot, EncountersRootName);
            var hazardsRoot = EnsureChild(roomRoot, HazardsRootName);
            var decorationRoot = EnsureChild(roomRoot, DecorationRootName);
            var triggersRoot = EnsureChild(roomRoot, TriggersRootName);

            var doorsRoot = EnsureChild(navigationRoot, DoorsRootName);
            var navigationSpawnPointsRoot = EnsureChild(navigationRoot, SpawnPointsRootName);
            var geometryRoot = EnsureChild(navigationRoot, GeometryRootName);
            EnsureComponent<RoomGeometryRoot>(geometryRoot.gameObject);

            var outerWallsRoot = EnsureChild(geometryRoot, OuterWallsRootName);
            var innerWallsRoot = EnsureChild(geometryRoot, InnerWallsRootName);

            return new RoomHierarchyRefs(
                navigationRoot,
                elementsRoot,
                encountersRoot,
                hazardsRoot,
                decorationRoot,
                triggersRoot,
                doorsRoot,
                navigationSpawnPointsRoot,
                geometryRoot,
                outerWallsRoot,
                innerWallsRoot);
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            var childObject = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            return Undo.AddComponent<T>(target);
        }
    }
}
```

- [ ] **Step 3: 把 `RoomFactory.cs` 的 hierarchy 创建切到共享 helper，不再维护手写第二套骨架**

In `Assets/Scripts/Level/Editor/LevelArchitect/RoomFactory.cs`, replace this block:

```csharp
// ── Standard hierarchy roots (Batch 2) ──
var navigationRoot = CreateChildObject(roomGO.transform, NAVIGATION_ROOT_NAME);
CreateChildObject(roomGO.transform, ELEMENTS_ROOT_NAME);
var encountersRoot = CreateChildObject(roomGO.transform, ENCOUNTERS_ROOT_NAME);
CreateChildObject(roomGO.transform, HAZARDS_ROOT_NAME);
CreateChildObject(roomGO.transform, DECORATION_ROOT_NAME);
CreateChildObject(roomGO.transform, TRIGGERS_ROOT_NAME);

// ── Optional Camera Confiner Child ──
```

with this exact code:

```csharp
// ── Standard hierarchy roots ──
var hierarchy = RoomAuthoringHierarchy.EnsureForRoom(roomGO.transform);
var navigationRoot = hierarchy.NavigationRoot;
var encountersRoot = hierarchy.EncountersRoot;

// ── Optional Camera Confiner Child ──
```

Then replace this block:

```csharp
// ── Standard Navigation placeholders ──
CreateChildObject(navigationRoot.transform, "Doors");
CreateChildObject(navigationRoot.transform, "SpawnPoints");

// ── SpawnPoints Container ──
var spawnPointsGO = CreateChildObject(encountersRoot.transform, "SpawnPoints");
```

with this exact code:

```csharp
// ── Encounter SpawnPoints Container ──
var encounterSpawnRoot = encountersRoot.Find(RoomAuthoringHierarchy.SpawnPointsRootName);
if (encounterSpawnRoot == null)
{
    encounterSpawnRoot = CreateChildObject(encountersRoot.transform, RoomAuthoringHierarchy.SpawnPointsRootName).transform;
}

var spawnPointsGO = encounterSpawnRoot.gameObject;
```

- [ ] **Step 4: 把 `LevelSliceBuilder.cs` 的 hierarchy 创建也切到同一套 helper**

In `Assets/Scripts/Level/Editor/LevelArchitect/LevelSliceBuilder.cs`, replace this block:

```csharp
// Standard hierarchy
var navRoot = CreateChild(roomGO.transform, "Navigation");
CreateChild(roomGO.transform, "Elements");
var encRoot = CreateChild(roomGO.transform, "Encounters");
CreateChild(roomGO.transform, "Hazards");
CreateChild(roomGO.transform, "Decoration");
CreateChild(roomGO.transform, "Triggers");

// Optional camera confiner
```

with this exact code:

```csharp
// Standard hierarchy
var hierarchy = RoomAuthoringHierarchy.EnsureForRoom(roomGO.transform);
var navRoot = hierarchy.NavigationRoot;
var encRoot = hierarchy.EncountersRoot;

// Optional camera confiner
```

Then delete this block:

```csharp
// Navigation placeholders
CreateChild(navRoot.transform, "Doors");
CreateChild(navRoot.transform, "SpawnPoints");
```

and leave the existing encounter spawn point creation in place.

- [ ] **Step 5: 回到 Unity，让新文件进入编译，再跑 `RoomAuthoringHierarchyTests` 确认通过**

Run in Unity Editor:

- 等待 Unity 完成脚本编译
- 打开 `EditMode` Test Runner
- 运行 `RoomAuthoringHierarchyTests`

Expected:

- 两个测试都通过
- 不再出现缺失 `RoomGeometryRoot` 或 `RoomAuthoringHierarchy` 的编译错误

- [ ] **Step 6: 做一次 `RoomFactory` / `Blockout` / JSON import 的手动 smoke test**

Run in Unity Editor:

1. 打开 `ProjectArk > Level > Authority > Level Architect`
2. 在 `Build` 页创建一个房间
3. 在 `Blockout` 模式再画一个房间
4. 用任意最小 JSON 切片跑一次 `Import LevelDesigner JSON`
5. 分别检查三个房间的层级

Expected:

- 都有 `Navigation/Geometry`
- 都有 `Navigation/Geometry/OuterWalls`
- 都有 `Navigation/Geometry/InnerWalls`
- `Navigation/Geometry` 上有且只有一个 `RoomGeometryRoot`

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Level/Room/RoomGeometryRoot.cs Assets/Scripts/Level/Editor/LevelArchitect/RoomAuthoringHierarchy.cs Assets/Scripts/Level/Editor/LevelArchitect/RoomFactory.cs Assets/Scripts/Level/Editor/LevelArchitect/LevelSliceBuilder.cs Assets/Scripts/Level/Editor/LevelArchitect/RoomAuthoringHierarchyTests.cs
git commit -m "feat: add static geometry room skeleton"
```

---

### Task 3: 先把 geometry validator 的失败测试补齐

**Files:**
- Modify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`
- Verify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`

- [ ] **Step 1: 在 `LevelValidatorTests.cs` 顶部补 Tilemap using**

In `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`, add this line immediately after `using UnityEngine;`:

```csharp
using UnityEngine.Tilemaps;
```

- [ ] **Step 2: 先把 geometry validator 的 5 个回归测试写进去**

Insert these exact test methods into `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`, immediately after `ValidateAll_ReportsWarning_WhenEnvironmentHazardHasNoTargetLayer()`:

```csharp
[Test]
public void ValidateAll_ReportsWarning_WhenNavigationGeometryRootMissing()
{
    var roomRig = CreateValidRoomRig("Room_Geometry_MissingRoot");
    Object.DestroyImmediate(roomRig.GeometryRoot.gameObject);

    var results = LevelValidator.ValidateAll();

    Assert.That(results.Any(result =>
        result.TargetObject == roomRig.NavigationRoot.gameObject &&
        result.Severity == LevelValidator.Severity.Warning &&
        result.Message.Contains("Navigation/Geometry")));
}

[Test]
public void ValidateAll_ReportsWarning_WhenGeometryRootMissingMarkerComponent()
{
    var roomRig = CreateValidRoomRig("Room_Geometry_MissingMarker");
    Object.DestroyImmediate(roomRig.GeometryRoot.GetComponent<RoomGeometryRoot>());

    var results = LevelValidator.ValidateAll();

    Assert.That(results.Any(result =>
        result.TargetObject == roomRig.GeometryRoot.gameObject &&
        result.Severity == LevelValidator.Severity.Warning &&
        result.Message.Contains("RoomGeometryRoot")));
}

[Test]
public void ValidateAll_ReportsError_WhenGeometryRootHasDuplicateMarker()
{
    var roomRig = CreateValidRoomRig("Room_Geometry_DuplicateMarker");
    roomRig.GeometryRoot.gameObject.AddComponent<RoomGeometryRoot>();

    var results = LevelValidator.ValidateAll();

    Assert.That(results.Any(result =>
        result.TargetObject == roomRig.GeometryRoot.gameObject &&
        result.Severity == LevelValidator.Severity.Error &&
        result.Message.Contains("multiple RoomGeometryRoot")));
}

[Test]
public void ValidateAll_ReportsError_WhenOuterWallsCompositeColliderHasNoStaticRigidbody()
{
    var roomRig = CreateValidRoomRig("Room_Geometry_MissingStaticBody");
    var outerWall = CreateChild(roomRig.OuterWallsRoot, "OuterWalls_Main");
    outerWall.gameObject.AddComponent<Tilemap>();
    outerWall.gameObject.AddComponent<TilemapRenderer>();
    outerWall.gameObject.AddComponent<TilemapCollider2D>();
    outerWall.gameObject.AddComponent<CompositeCollider2D>();

    var results = LevelValidator.ValidateAll();

    Assert.That(results.Any(result =>
        result.TargetObject == outerWall.gameObject &&
        result.Severity == LevelValidator.Severity.Error &&
        result.Message.Contains("Rigidbody2D") &&
        result.Message.Contains("Static")));
}

[Test]
public void ValidateAll_ReportsWarning_WhenWallNamedTilemapLivesUnderElementsRoot()
{
    var roomRig = CreateValidRoomRig("Room_Geometry_WrongRoot");
    var misplacedWall = CreateChild(roomRig.ElementsRoot, "OuterWalls_Main");
    misplacedWall.gameObject.AddComponent<Tilemap>();
    misplacedWall.gameObject.AddComponent<TilemapRenderer>();
    misplacedWall.gameObject.AddComponent<TilemapCollider2D>();

    var results = LevelValidator.ValidateAll();

    Assert.That(results.Any(result =>
        result.TargetObject == misplacedWall.gameObject &&
        result.Severity == LevelValidator.Severity.Warning &&
        result.Message.Contains("Navigation/Geometry") &&
        result.Message.Contains("OuterWalls_Main")));
}
```

- [ ] **Step 3: 把 `CreateValidRoomRig()` 升级到新 geometry skeleton，否则旧 helper 会把整个测试集拖成红色噪音**

Replace the body of `CreateValidRoomRig(string roomName)` with this exact code:

```csharp
{
    var roomObject = CreateGameObjectWithComponent<Room>(roomName);
    roomObject.AddComponent<BoxCollider2D>().isTrigger = true;

    var roomData = CreateScriptableObject<RoomSO>($"{roomName}_Data");
    SetPrivateField(roomData, "_roomID", roomName);
    SetPrivateField(roomData, "_displayName", roomName);
    SetPrivateField(roomObject.GetComponent<Room>(), "_data", roomData);

    var navigationRoot = CreateChild(roomObject.transform, "Navigation");
    var geometryRoot = CreateChild(navigationRoot, "Geometry");
    geometryRoot.gameObject.AddComponent<RoomGeometryRoot>();
    var outerWallsRoot = CreateChild(geometryRoot, "OuterWalls");
    var innerWallsRoot = CreateChild(geometryRoot, "InnerWalls");
    CreateChild(navigationRoot, "Doors");
    CreateChild(navigationRoot, "SpawnPoints");

    var elementsRoot = CreateChild(roomObject.transform, "Elements");
    var encountersRoot = CreateChild(roomObject.transform, "Encounters");
    var hazardsRoot = CreateChild(roomObject.transform, "Hazards");
    var decorationRoot = CreateChild(roomObject.transform, "Decoration");
    var triggersRoot = CreateChild(roomObject.transform, "Triggers");
    var confiner = CreateChild(roomObject.transform, "CameraConfiner");
    confiner.gameObject.layer = 2;

    return new RoomTestRig
    {
        Room = roomObject.GetComponent<Room>(),
        NavigationRoot = navigationRoot,
        GeometryRoot = geometryRoot,
        OuterWallsRoot = outerWallsRoot,
        InnerWallsRoot = innerWallsRoot,
        ElementsRoot = elementsRoot,
        EncountersRoot = encountersRoot,
        HazardsRoot = hazardsRoot,
        DecorationRoot = decorationRoot,
        TriggersRoot = triggersRoot,
        CameraConfiner = confiner
    };
}
```

Then replace the `RoomTestRig` class with this exact version:

```csharp
private sealed class RoomTestRig
{
    public Room Room;
    public Transform NavigationRoot;
    public Transform GeometryRoot;
    public Transform OuterWallsRoot;
    public Transform InnerWallsRoot;
    public Transform ElementsRoot;
    public Transform EncountersRoot;
    public Transform HazardsRoot;
    public Transform DecorationRoot;
    public Transform TriggersRoot;
    public Transform CameraConfiner;
}
```

- [ ] **Step 4: 先运行 `LevelValidatorTests`，确认新 geometry tests 现在失败**

Run in Unity Editor:

- 打开 `EditMode` Test Runner
- 运行 `LevelValidatorTests`

Expected:

- 新增的 geometry tests 失败
- 失败原因应是 `LevelValidator` 还没有 geometry 规则，而不是测试 helper 自己搭坏了

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs
git commit -m "test: add geometry validator regression coverage"
```

---

### Task 4: 在 `LevelValidator` 中实现 geometry 护栏，并跑通回归测试

**Files:**
- Modify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`
- Test: `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`

- [ ] **Step 1: 先给 `LevelValidator.cs` 补 Tilemap using，并把 geometry 验证接进 `ValidateAll()`**

In `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`, add this line immediately after `using UnityEngine;`:

```csharp
using UnityEngine.Tilemaps;
```

Then in `ValidateAll()`, replace this block:

```csharp
ValidateRoomSO(room);
ValidateBoxColliderTrigger(room);
ValidateCameraConfiner(room);
ValidateStandardRoomHierarchy(room);
ValidateArenaBossConfig(room);
ValidateRoomEncounterMode(room);
```

with this exact code:

```csharp
ValidateRoomSO(room);
ValidateBoxColliderTrigger(room);
ValidateCameraConfiner(room);
ValidateStandardRoomHierarchy(room);
ValidateRoomGeometryAuthoring(room);
ValidateArenaBossConfig(room);
ValidateRoomEncounterMode(room);
```

- [ ] **Step 2: 在 `ValidateStandardRoomHierarchy()` 后面插入 geometry 相关规则实现**

Insert this exact code into `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`, immediately after `ValidateStandardRoomHierarchy(Room room)`:

```csharp
private static void ValidateRoomGeometryAuthoring(Room room)
{
    var navigationRoot = room.transform.Find(RoomAuthoringHierarchy.NavigationRootName);
    if (navigationRoot == null)
    {
        return;
    }

    var geometryRoot = navigationRoot.Find(RoomAuthoringHierarchy.GeometryRootName);
    if (geometryRoot == null)
    {
        _lastResults.Add(new ValidationResult
        {
            Severity = Severity.Warning,
            Message = $"Room '{room.RoomID}' is missing Navigation/Geometry root for static geometry authoring.",
            TargetObject = navigationRoot.gameObject,
            CanAutoFix = true,
            FixAction = () => RoomAuthoringHierarchy.EnsureForRoom(room.transform)
        });
        return;
    }

    var outerWallsRoot = geometryRoot.Find(RoomAuthoringHierarchy.OuterWallsRootName);
    if (outerWallsRoot == null)
    {
        _lastResults.Add(new ValidationResult
        {
            Severity = Severity.Warning,
            Message = $"Room '{room.RoomID}' is missing Navigation/Geometry/OuterWalls root.",
            TargetObject = geometryRoot.gameObject,
            CanAutoFix = true,
            FixAction = () => RoomAuthoringHierarchy.EnsureForRoom(room.transform)
        });
    }

    var innerWallsRoot = geometryRoot.Find(RoomAuthoringHierarchy.InnerWallsRootName);
    if (innerWallsRoot == null)
    {
        _lastResults.Add(new ValidationResult
        {
            Severity = Severity.Info,
            Message = $"Room '{room.RoomID}' is missing Navigation/Geometry/InnerWalls root.",
            TargetObject = geometryRoot.gameObject,
            CanAutoFix = true,
            FixAction = () => RoomAuthoringHierarchy.EnsureForRoom(room.transform)
        });
    }

    ValidateGeometryRootMarker(room, geometryRoot);

    if (outerWallsRoot != null)
    {
        ValidateOuterWallsCollisionChain(room, outerWallsRoot);
    }

    ValidateMisplacedNamedWallTilemaps(room, RoomAuthoringHierarchy.ElementsRootName);
    ValidateMisplacedNamedWallTilemaps(room, RoomAuthoringHierarchy.TriggersRootName);
}

private static void ValidateGeometryRootMarker(Room room, Transform geometryRoot)
{
    var markers = geometryRoot.GetComponents<RoomGeometryRoot>();
    if (markers.Length == 0)
    {
        _lastResults.Add(new ValidationResult
        {
            Severity = Severity.Warning,
            Message = $"Room '{room.RoomID}' Navigation/Geometry is missing RoomGeometryRoot marker.",
            TargetObject = geometryRoot.gameObject,
            CanAutoFix = true,
            FixAction = () => Undo.AddComponent<RoomGeometryRoot>(geometryRoot.gameObject)
        });
        return;
    }

    if (markers.Length > 1)
    {
        _lastResults.Add(new ValidationResult
        {
            Severity = Severity.Error,
            Message = $"Room '{room.RoomID}' Navigation/Geometry has multiple RoomGeometryRoot components.",
            TargetObject = geometryRoot.gameObject,
            CanAutoFix = false,
            FixAction = null
        });
    }

    var components = geometryRoot.GetComponents<MonoBehaviour>();
    foreach (var component in components)
    {
        if (component == null || component is RoomGeometryRoot)
        {
            continue;
        }

        _lastResults.Add(new ValidationResult
        {
            Severity = Severity.Warning,
            Message = $"Room '{room.RoomID}' Navigation/Geometry should stay marker-only, but found '{component.GetType().Name}'.",
            TargetObject = component,
            CanAutoFix = false,
            FixAction = null
        });
    }
}

private static void ValidateOuterWallsCollisionChain(Room room, Transform outerWallsRoot)
{
    var tilemapColliders = outerWallsRoot.GetComponentsInChildren<TilemapCollider2D>(true);
    if (tilemapColliders.Length == 0)
    {
        _lastResults.Add(new ValidationResult
        {
            Severity = Severity.Warning,
            Message = $"Room '{room.RoomID}' OuterWalls has no TilemapCollider2D chain yet.",
            TargetObject = outerWallsRoot.gameObject,
            CanAutoFix = false,
            FixAction = null
        });
        return;
    }

    foreach (var tilemapCollider in tilemapColliders)
    {
        if (tilemapCollider == null)
        {
            continue;
        }

        if (tilemapCollider.GetComponent<Tilemap>() == null)
        {
            _lastResults.Add(new ValidationResult
            {
                Severity = Severity.Error,
                Message = $"Outer wall '{tilemapCollider.gameObject.name}' is missing Tilemap component.",
                TargetObject = tilemapCollider.gameObject,
                CanAutoFix = false,
                FixAction = null
            });
        }

        var compositeCollider = tilemapCollider.GetComponent<CompositeCollider2D>();
        if (compositeCollider == null)
        {
            continue;
        }

        var rigidbody = tilemapCollider.GetComponent<Rigidbody2D>();
        if (rigidbody == null || rigidbody.bodyType != RigidbodyType2D.Static)
        {
            _lastResults.Add(new ValidationResult
            {
                Severity = Severity.Error,
                Message = $"Outer wall '{tilemapCollider.gameObject.name}' uses CompositeCollider2D but is missing a Static Rigidbody2D.",
                TargetObject = tilemapCollider.gameObject,
                CanAutoFix = false,
                FixAction = null
            });
        }

        if (tilemapCollider.compositeOperation == Collider2D.CompositeOperation.None)
        {
            _lastResults.Add(new ValidationResult
            {
                Severity = Severity.Error,
                Message = $"Outer wall '{tilemapCollider.gameObject.name}' has CompositeCollider2D but TilemapCollider2D is not configured to use composite.",
                TargetObject = tilemapCollider.gameObject,
                CanAutoFix = false,
                FixAction = null
            });
        }
    }
}

private static void ValidateMisplacedNamedWallTilemaps(Room room, string rootName)
{
    var root = room.transform.Find(rootName);
    if (root == null)
    {
        return;
    }

    var tilemapColliders = root.GetComponentsInChildren<TilemapCollider2D>(true);
    foreach (var tilemapCollider in tilemapColliders)
    {
        if (tilemapCollider == null)
        {
            continue;
        }

        string objectName = tilemapCollider.gameObject.name;
        bool looksLikeWallRoot = objectName.StartsWith(RoomAuthoringHierarchy.OuterWallsRootName, StringComparison.Ordinal)
            || objectName.StartsWith(RoomAuthoringHierarchy.InnerWallsRootName, StringComparison.Ordinal);

        if (!looksLikeWallRoot)
        {
            continue;
        }

        _lastResults.Add(new ValidationResult
        {
            Severity = Severity.Warning,
            Message = $"Named wall tilemap '{objectName}' is under '{rootName}' in Room '{room.RoomID}'. Static walls should live under Navigation/Geometry.",
            TargetObject = tilemapCollider.gameObject,
            CanAutoFix = false,
            FixAction = null
        });
    }
}
```

- [ ] **Step 3: 跑 `LevelValidatorTests`，把 geometry 回归测试跑绿**

Run in Unity Editor:

- 打开 `EditMode` Test Runner
- 运行 `LevelValidatorTests`

Expected:

- 现有 tests 继续通过
- 新增的 5 个 geometry tests 全部通过
- 不应引入新的红色噪音测试

- [ ] **Step 4: 额外做一次整项目编译检查**

Run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx
```

Expected:

- 构建成功
- 不应出现 `RoomGeometryRoot`、`RoomAuthoringHierarchy`、`TilemapCollider2D.compositeOperation` 相关编译错误

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs
git commit -m "feat: validate static geometry wall authoring"
```

---

### Task 5: 同步 workflow 文档、做 editor 验收，并补实现日志

**Files:**
- Modify: `Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md`
- Modify: `Docs/5_ImplementationLog/ImplementationLog.md`

- [ ] **Step 1: 在 `Level_WorkflowSpec.md` 中补入新的 geometry authoring 规则**

In `Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md`, insert this exact block immediately after the bullet `- 房间位置、尺寸、楼层关系正确` under `### 6.1 Phase 1：结构搭建 Pass`:

```md
- 每个新房间都应带有 `Navigation/Geometry` 骨架；静态几何墙统一 author 在 `Navigation/Geometry/OuterWalls` 与 `Navigation/Geometry/InnerWalls`
- `Navigation/Geometry` 应保持 marker-only，只挂 `RoomGeometryRoot`，不要把玩法脚本或状态组件塞在这个根上
- 外轮廓主墙优先放在 `OuterWalls`，并使用统一的 `Tilemap + TilemapCollider2D (+ Static Rigidbody2D + CompositeCollider2D)` 组合
```

Then insert this exact block immediately after the bullet `- 若当前进入语义补件阶段，可直接用 Runtime Assist / Starter Objects 入口补 Checkpoint、OpenEncounterTrigger、BiomeTrigger、ScheduledBehaviour、WorldEventTrigger，连接 inspector 里则可直接补 Lock starter`:

```md
- 静态几何墙不走 `Runtime Assist` 按钮链，而是跟随新房默认骨架直接在 `Navigation/Geometry` 下 author；若 `Validate All` 报 geometry 结构 warning，优先修根节点与 marker，再修具体 Tilemap 碰撞链
```

- [ ] **Step 2: 在 Unity Editor 里做最终 authoring 验收**

Run in Unity Editor:

1. 用 `Build` 新建一个 `Transit` 房间
2. 用 `Blockout` 再画一个 `Combat` 房间
3. 选中其中一个房间，在 `Navigation/Geometry/OuterWalls` 下手动创建 `OuterWalls_Main`
4. 给 `OuterWalls_Main` 添加 `Tilemap`、`TilemapRenderer`、`TilemapCollider2D`、`Rigidbody2D`（`Static`）、`CompositeCollider2D`
5. 点击 `Validate All`

Expected:

- 新房骨架自动带出 `Geometry / OuterWalls / InnerWalls`
- `Geometry` 上只有一个 `RoomGeometryRoot`
- 合法的 `OuterWalls_Main` 不会触发缺失 rigidbody / root / marker 的 geometry 错误
- 如果把 `OuterWalls_Main` 临时拖到 `Elements` 下，再次 `Validate All`，应看到错误根放置 warning

- [ ] **Step 3: 把本轮实现按项目格式追加到 `ImplementationLog.md`**

Insert a new entry at the top of `Docs/5_ImplementationLog/ImplementationLog.md` using this exact structure (replace the timestamp with the real execution time):

```md
## 落地静态几何墙 MVP authoring 闭环，统一 `Navigation/Geometry` room skeleton — YYYY-MM-DD HH:MM

### 新建文件
- `Assets/Scripts/Level/Room/RoomGeometryRoot.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomAuthoringHierarchy.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomAuthoringHierarchyTests.cs`

### 修改文件
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomFactory.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelSliceBuilder.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`
- `Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md`
- `Docs/5_ImplementationLog/ImplementationLog.md`

### 内容
- 新增 `RoomGeometryRoot` 作为 `Navigation/Geometry` 的超轻 marker，并用 `RoomAuthoringHierarchy` 把 `RoomFactory` 与 `LevelSliceBuilder` 的标准房间骨架收口到同一条 authority 链。
- 新建房间、Blockout 画房与 JSON 导入房间现在都会自动带出 `Navigation/Geometry/OuterWalls/InnerWalls`，不再依赖作者手工搭 geometry 根节点。
- `LevelValidator` 增加了 geometry authoring 规则：检查 `Navigation/Geometry`、`RoomGeometryRoot`、`OuterWalls` 碰撞链、以及命名墙 Tilemap 被误放到 `Elements/Triggers` 的问题。
- 新增/更新 EditMode tests，锁住 geometry skeleton 与 validator 行为，避免后续 `RoomFactory` 和 JSON import 再次漂移。

### 目的
- 把静态几何墙从 spec 推进为第一版可执行 authoring 语言，让外轮廓墙先拥有统一骨架、统一护栏和统一工具 authority。
- 为后续 `BreakableWall`、`PhaseBarrier` 等变化墙保留干净的空间事实主链，避免再次把静态墙和玩法墙混写。

### 技术
- 使用 editor-only shared helper 收口 room skeleton authority，而不是在 `RoomFactory` 与 `LevelSliceBuilder` 中继续复制 hierarchy 创建代码。
- 使用 `RoomGeometryRoot` marker + `LevelValidator` + NUnit EditMode tests 建立 starter-first 的 authoring 闭环，保持 `Scene-backed geometry` 原则不变。
```

- [ ] **Step 4: 再跑一次编译 + Validate All，确认日志和文档改动没有拖出额外问题**

Run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx
```

Then in Unity Editor:

- 打开 `ProjectArk > Level > Authority > Level Architect`
- 点击 `Validate All`

Expected:

- `dotnet build` 成功
- validator 正常工作
- 没有因为文档同步而漏掉实际实现收尾

- [ ] **Step 5: Commit**

```bash
git add Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md Docs/5_ImplementationLog/ImplementationLog.md
git commit -m "docs: document static geometry wall authoring"
```

---

## Self-Review Checklist

在执行本计划前，再逐条自查一次：

- spec 的 `Navigation/Geometry / OuterWalls / InnerWalls / RoomGeometryRoot / validator / skeleton-only tooling` 是否都能在任务里找到对应落点
- 是否避免把 scope 扩展到 `BreakableWall / PhaseBarrier / Geometry starter buttons / auto-wall drawing`
- 是否已经处理 `RoomFactory` 与 `LevelSliceBuilder` 双 authority 漂移问题
- 是否已经考虑 `LevelValidatorTests.CreateValidRoomRig()` 升级，否则旧测试会因为 geometry 新规则全部误报
- 是否保留了 `Scene-backed geometry` 原则，没有给 `Room` 或 `RoomGeometryRoot` 增加 runtime owner 职责

---

## Execution Handoff

**Plan complete and saved to** `Docs/0_Plan/ongoing/2026-04-15-static-geometry-walls-implementation-plan.md`。

**Two execution options:**

**1. Subagent-Driven (recommended)** — 我按 task 派发新的执行 agent，逐段实现、逐段 review，适合这种 editor + validator + docs 的多点改动。

**2. Inline Execution** — 我在当前会话里直接按这个 plan 开始落代码，并在关键节点停下来给你汇报。

**Which approach?**
