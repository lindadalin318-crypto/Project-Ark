# Level Architect Hazards Starter-First Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 `Level Architect` 增加第一批 `Hazards` 语义元素的 starter-first authoring 入口，把现有运行时 hazard family 正式接入 room authoring 主链。

**Architecture:** 这份计划只实现 spec 的第一个子项目：`Hazards starter-first`。做法是复用现有运行时类 `ContactHazard`、`DamageZone`、`TimedHazard`，在 `LevelRuntimeAssistFactory` 中补 hazard starters，在 `LevelArchitectWindow` 中开放 `Hazards` 按钮区，并在 `LevelValidator` / EditMode tests 中补齐 hazard 的 root、collider、`_targetLayer` 护栏。`Traversal / Secret`、`Interaction`、`Group / World` 明确留给后续单独计划。

**Tech Stack:** Unity 6000.3.7f1、C#、Unity Editor API（`Undo`、`SerializedObject`）、NUnit EditMode tests、Markdown、`dotnet build`

---

## Scope Split Note

这份计划**故意不**覆盖整个 spec，而只覆盖第一个可独立交付的子项目：

- **本计划负责：** `Hazards` starter-first
- **后续单独计划：** `Traversal / Secret`、`Interaction`、`Group / World`

这样做的原因是：`Hazards` 已经具备现成运行时类与 validator root，最适合先打通一条完整的“测试 → starter → window → validator → docs”闭环。

---

## File Structure Map

### Create

- `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactoryTests.cs` — `Hazards` starter creation regression tests，验证新按钮最终创建到 `Hazards` 根，组件类型、Trigger Collider、`_targetLayer` 都正确。
- `Docs/9_Superpowers/plans/2026-04-14-level-architect-hazards-starter-first-implementation-plan.md` — 当前实现计划。

### Modify

- `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactory.cs` — 增加 `ContactHazard`、`DamageZone`、`TimedHazard` starter 创建逻辑。
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs` — 在 `Runtime Assist` 中加入 `Hazards` 分组与按钮。
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs` — 增加 hazard 基础验证（Collider Trigger / `_targetLayer`），并接入 `ValidateAll()`。
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs` — 增加 hazard validator regression tests。
- `Docs/6_Diagnostics/LevelArchitect_SupportedElements_Matrix.md` — 把 `EnvironmentHazard` 从“运行时支持未开放”升级为具体的 `Hazards` starter 支持项。
- `Docs/5_ImplementationLog/ImplementationLog_2026-04.md` — 记录这轮 `Hazards starter-first` authoring 落地。

### Keep As-Is

- `Assets/Scripts/Level/Hazard/EnvironmentHazard.cs`
- `Assets/Scripts/Level/Hazard/ContactHazard.cs`
- `Assets/Scripts/Level/Hazard/DamageZone.cs`
- `Assets/Scripts/Level/Hazard/TimedHazard.cs`
- `Assets/Scripts/Level/ProjectArk.Level.asmdef`
- `Assets/Scripts/Level/Editor/ProjectArk.Level.Editor.asmdef`

这些运行时类和 asmdef 当前已经足够支撑本轮 authoring 接入，不要在这轮里额外扩写 hazard family 或重构程序集。

---

### Task 1: 先写 `Hazards` starter creation 的失败测试

**Files:**

- Create: `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactoryTests.cs`
- Verify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactory.cs`

- [ ] **Step 1: 新建 `LevelRuntimeAssistFactoryTests.cs`，先把三个 hazard starter 的期望写死**

Write this exact content to `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactoryTests.cs`:

```csharp
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    [TestFixture]
    public class LevelRuntimeAssistFactoryTests
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
        public void CreateRoomAssist_CreatesContactHazardStarterUnderHazardsRoot()
        {
            var room = CreateRoom("Room_Hazard_Contact");

            var created = LevelRuntimeAssistFactory.CreateRoomAssist(
                room,
                LevelRuntimeAssistFactory.RoomAssistType.ContactHazard);

            Assert.That(created, Is.Not.Null);
            Assert.That(created.transform.parent, Is.Not.Null);
            Assert.That(created.transform.parent.name, Is.EqualTo("Hazards"));
            Assert.That(created.GetComponent<ContactHazard>(), Is.Not.Null);
            AssertHazardStarter(created.GetComponent<ContactHazard>());
        }

        [Test]
        public void CreateRoomAssist_CreatesDamageZoneStarterUnderHazardsRoot()
        {
            var room = CreateRoom("Room_Hazard_DamageZone");

            var created = LevelRuntimeAssistFactory.CreateRoomAssist(
                room,
                LevelRuntimeAssistFactory.RoomAssistType.DamageZone);

            Assert.That(created, Is.Not.Null);
            Assert.That(created.transform.parent, Is.Not.Null);
            Assert.That(created.transform.parent.name, Is.EqualTo("Hazards"));
            Assert.That(created.GetComponent<DamageZone>(), Is.Not.Null);
            AssertHazardStarter(created.GetComponent<DamageZone>());
        }

        [Test]
        public void CreateRoomAssist_CreatesTimedHazardStarterUnderHazardsRoot()
        {
            var room = CreateRoom("Room_Hazard_Timed");

            var created = LevelRuntimeAssistFactory.CreateRoomAssist(
                room,
                LevelRuntimeAssistFactory.RoomAssistType.TimedHazard);

            Assert.That(created, Is.Not.Null);
            Assert.That(created.transform.parent, Is.Not.Null);
            Assert.That(created.transform.parent.name, Is.EqualTo("Hazards"));
            Assert.That(created.GetComponent<TimedHazard>(), Is.Not.Null);
            AssertHazardStarter(created.GetComponent<TimedHazard>());
        }

        private Room CreateRoom(string roomId)
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

            return room;
        }

        private static void AssertHazardStarter(EnvironmentHazard hazard)
        {
            Assert.That(hazard, Is.Not.Null);

            var collider = hazard.GetComponent<Collider2D>();
            Assert.That(collider, Is.Not.Null);
            Assert.That(collider.isTrigger, Is.True);

            var serialized = new SerializedObject(hazard);
            int bits = serialized.FindProperty("_targetLayer").FindPropertyRelative("m_Bits").intValue;
            Assert.That(bits, Is.Not.EqualTo(0));
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

- [ ] **Step 2: 先编译，确认测试因为新枚举值还不存在而失败**

Run:

```powershell
dotnet build "f:\UnityProjects\Project-Ark\Project-Ark.slnx"
```

Expected:

- 构建失败
- 错误应包含 `LevelRuntimeAssistFactory.RoomAssistType` 不存在 `ContactHazard`、`DamageZone`、`TimedHazard`
- 不要修别的文件，直接进入 Task 2

- [ ] **Step 3: 用 `git diff --stat` 确认本任务只新增了测试文件**

Run:

```powershell
git -C "f:\UnityProjects\Project-Ark" --no-pager diff --stat -- "Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactoryTests.cs"
```

Expected:

- 只出现 `LevelRuntimeAssistFactoryTests.cs` 的新增统计

---

### Task 2: 在 `LevelRuntimeAssistFactory` 中实现三种 hazard starters

**Files:**

- Modify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactory.cs`
- Verify: `Assets/Scripts/Level/Hazard/ContactHazard.cs`
- Verify: `Assets/Scripts/Level/Hazard/DamageZone.cs`
- Verify: `Assets/Scripts/Level/Hazard/TimedHazard.cs`

- [ ] **Step 1: 扩充 `RoomAssistType`、`GetDisplayName()`、`CreateRoomAssist()` 的分发表**

In `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactory.cs`, replace the enum / constant / switch / display-name blocks so they become exactly this:

```csharp
public static class LevelRuntimeAssistFactory
{
    public enum RoomAssistType
    {
        Checkpoint,
        OpenEncounterTrigger,
        BiomeTrigger,
        ScheduledBehaviour,
        WorldEventTrigger,
        ContactHazard,
        DamageZone,
        TimedHazard
    }

    private const string ELEMENTS_ROOT_NAME = "Elements";
    private const string ENCOUNTERS_ROOT_NAME = "Encounters";
    private const string HAZARDS_ROOT_NAME = "Hazards";
    private const string TRIGGERS_ROOT_NAME = "Triggers";
    private const string SPAWN_POINTS_ROOT_NAME = "SpawnPoints";

    public static GameObject CreateRoomAssist(Room room, RoomAssistType assistType)
    {
        if (room == null)
        {
            Debug.LogWarning("[LevelRuntimeAssist] Cannot create runtime assist: room is null.");
            return null;
        }

        return assistType switch
        {
            RoomAssistType.Checkpoint => CreateCheckpoint(room),
            RoomAssistType.OpenEncounterTrigger => CreateOpenEncounterTrigger(room),
            RoomAssistType.BiomeTrigger => CreateBiomeTrigger(room),
            RoomAssistType.ScheduledBehaviour => CreateScheduledBehaviour(room),
            RoomAssistType.WorldEventTrigger => CreateWorldEventTrigger(room),
            RoomAssistType.ContactHazard => CreateContactHazard(room),
            RoomAssistType.DamageZone => CreateDamageZone(room),
            RoomAssistType.TimedHazard => CreateTimedHazard(room),
            _ => null
        };
    }

    public static string GetDisplayName(RoomAssistType assistType)
    {
        return assistType switch
        {
            RoomAssistType.Checkpoint => "Checkpoint",
            RoomAssistType.OpenEncounterTrigger => "Open Encounter",
            RoomAssistType.BiomeTrigger => "Biome Trigger",
            RoomAssistType.ScheduledBehaviour => "Scheduled",
            RoomAssistType.WorldEventTrigger => "World Event",
            RoomAssistType.ContactHazard => "Contact Hazard",
            RoomAssistType.DamageZone => "Damage Zone",
            RoomAssistType.TimedHazard => "Timed Hazard",
            _ => assistType.ToString()
        };
    }
```

- [ ] **Step 2: 在同文件中新增三种 hazard starter 创建方法**

Insert these exact methods right after `CreateWorldEventTrigger` in `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactory.cs`:

```csharp
private static GameObject CreateContactHazard(Room room)
{
    Transform root = EnsureAuthoringRoot(room, HAZARDS_ROOT_NAME);
    string objectName = GameObjectUtility.GetUniqueNameForSibling(root, $"ContactHazard_{room.RoomID}");
    GameObject hazardObject = CreateChild(root, objectName, GetRoomCenter(room));

    EnsureTriggerBoxCollider(hazardObject, new Vector2(4f, 4f));
    var hazard = Undo.AddComponent<ContactHazard>(hazardObject);
    ApplyPlayerLayer(hazard, "_targetLayer");

    FinalizeCreatedObject(hazardObject, $"[LevelRuntimeAssist] Created ContactHazard starter in room '{room.RoomID}'. Tune damage, knockback and hit cooldown before playtest.");
    return hazardObject;
}

private static GameObject CreateDamageZone(Room room)
{
    Transform root = EnsureAuthoringRoot(room, HAZARDS_ROOT_NAME);
    string objectName = GameObjectUtility.GetUniqueNameForSibling(root, $"DamageZone_{room.RoomID}");
    GameObject hazardObject = CreateChild(root, objectName, GetRoomCenter(room));

    EnsureTriggerBoxCollider(hazardObject, GetSuggestedTriggerSize(room, 0.45f, 0.35f, new Vector2(6f, 4f)));
    var hazard = Undo.AddComponent<DamageZone>(hazardObject);
    ApplyPlayerLayer(hazard, "_targetLayer");

    FinalizeCreatedObject(hazardObject, $"[LevelRuntimeAssist] Created DamageZone starter in room '{room.RoomID}'. Tune damage, tick interval and playable footprint before playtest.");
    return hazardObject;
}

private static GameObject CreateTimedHazard(Room room)
{
    Transform root = EnsureAuthoringRoot(room, HAZARDS_ROOT_NAME);
    string objectName = GameObjectUtility.GetUniqueNameForSibling(root, $"TimedHazard_{room.RoomID}");
    GameObject hazardObject = CreateChild(root, objectName, GetRoomCenter(room));

    EnsureTriggerBoxCollider(hazardObject, new Vector2(4f, 4f));
    var hazard = Undo.AddComponent<TimedHazard>(hazardObject);
    ApplyPlayerLayer(hazard, "_targetLayer");

    FinalizeCreatedObject(hazardObject, $"[LevelRuntimeAssist] Created TimedHazard starter in room '{room.RoomID}'. Tune active/inactive durations, cooldown and visuals before playtest.");
    return hazardObject;
}
```

- [ ] **Step 3: 重新编译，确认 starter factory 与新测试都能通过编译**

Run:

```powershell
dotnet build "f:\UnityProjects\Project-Ark\Project-Ark.slnx"
```

Expected:

- 构建成功
- 不应再出现 `RoomAssistType` 缺少 hazard 枚举值的错误

- [ ] **Step 4: 在 Unity Test Runner 中运行 `LevelRuntimeAssistFactoryTests`，确认三个 starter 测试通过**

Run in Unity Editor:

- 打开 `Window > General > Test Runner`
- 切到 `EditMode`
- 运行 `LevelRuntimeAssistFactoryTests`

Expected:

- 3 个测试全部通过
- 失败时优先检查：创建对象是否挂在 `Hazards` 根、`Collider2D.isTrigger` 是否为 `true`、`_targetLayer` 是否被写入 `Player`

---

### Task 3: 在 `LevelArchitectWindow` 中开放 `Hazards` 按钮区

**Files:**

- Modify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs`

- [ ] **Step 1: 更新 Runtime Assist 的帮助文案，把 hazards 明确写进根节点说明**

In `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs`, replace the three help strings in `DrawBuildRuntimeAssistSection()` / `DrawRoomRuntimeAssistSection()` with this exact content:

```csharp
string hint = _selectedRooms.Count <= 0
    ? "先单选一个房间，再从这里补 Checkpoint、Encounter Trigger、Hazard、Biome Trigger 等标准 starter。"
    : $"当前选中了 {_selectedRooms.Count} 个房间。Runtime Assist 只对单房显示，避免工具替你批量猜设计。";

GUILayout.Label(
    "适合在结构与连接已基本稳定后，补第一批 runtime 对象起点。创建后会自动选中新对象，方便继续补 SO / phase / key / hazard 参数配置。",
    EditorStyles.wordWrappedMiniLabel);

EditorGUILayout.LabelField(
    "根节点分组：Elements → Checkpoint；Encounters → Open Encounter；Hazards → Contact / Zone / Timed；Triggers → Biome / Scheduled / World Event。",
    EditorStyles.wordWrappedMiniLabel);
```

- [ ] **Step 2: 在 `DrawRoomRuntimeAssistButtons()` 中加入 `Hazards` 分组按钮**

In `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs`, insert this exact block between `Encounters` 与 `Triggers` 两组之间：

```csharp
GUILayout.Space(2f);
EditorGUILayout.LabelField("Hazards", EditorStyles.miniBoldLabel);
EditorGUILayout.BeginHorizontal();
if (GUILayout.Button(LevelRuntimeAssistFactory.GetDisplayName(LevelRuntimeAssistFactory.RoomAssistType.ContactHazard), GUILayout.Height(buttonHeight)))
{
    CreateRoomRuntimeAssist(room, LevelRuntimeAssistFactory.RoomAssistType.ContactHazard);
}
if (GUILayout.Button(LevelRuntimeAssistFactory.GetDisplayName(LevelRuntimeAssistFactory.RoomAssistType.DamageZone), GUILayout.Height(buttonHeight)))
{
    CreateRoomRuntimeAssist(room, LevelRuntimeAssistFactory.RoomAssistType.DamageZone);
}
EditorGUILayout.EndHorizontal();

if (GUILayout.Button(LevelRuntimeAssistFactory.GetDisplayName(LevelRuntimeAssistFactory.RoomAssistType.TimedHazard), GUILayout.Height(buttonHeight)))
{
    CreateRoomRuntimeAssist(room, LevelRuntimeAssistFactory.RoomAssistType.TimedHazard);
}
```

- [ ] **Step 3: 重新编译，确认窗口脚本没有引入新的编辑器编译错误**

Run:

```powershell
dotnet build "f:\UnityProjects\Project-Ark\Project-Ark.slnx"
```

Expected:

- 构建成功
- 不应出现 `DrawRoomRuntimeAssistButtons` 中的重复变量名或未闭合括号错误

- [ ] **Step 4: 在 Unity 里做一次 UI smoke test**

Run in Unity Editor:

- 打开 `ProjectArk > Level Architect`
- 选中任意一个 `Room`
- 进入 `Build` 或 `Quick Edit`
- 查看 `Runtime Assist / Starter Objects`

Expected:

- 能看到新的 `Hazards` 分组
- 分组内有 `Contact Hazard`、`Damage Zone`、`Timed Hazard` 三个按钮
- 点击后对象会自动选中，且新对象在房间的 `Hazards` 根下

---

### Task 4: 给 hazards 补 validator 护栏与 regression tests

**Files:**

- Modify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`
- Modify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`

- [ ] **Step 1: 先给 `LevelValidatorTests.cs` 加两个失败测试**

Insert these exact test methods into `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`, immediately after `ValidateAll_ReportsWarning_WhenBiomeTriggerPlacedOutsideTriggersRoot()`:

```csharp
[Test]
public void ValidateAll_ReportsWarning_WhenEnvironmentHazardPlacedOutsideHazardsRoot()
{
    var roomRig = CreateValidRoomRig("Room_Hazard_WrongRoot");
    var hazardObject = CreateGameObjectWithComponent<ContactHazard>("Hazard_WrongRoot");
    hazardObject.transform.SetParent(roomRig.ElementsRoot, false);
    hazardObject.AddComponent<BoxCollider2D>().isTrigger = true;
    SetPrivateField(hazardObject.GetComponent<ContactHazard>(), "_targetLayer", (LayerMask)1);

    var results = LevelValidator.ValidateAll();

    Assert.That(results.Any(result =>
        result.TargetObject == hazardObject.GetComponent<ContactHazard>() &&
        result.Severity == LevelValidator.Severity.Warning &&
        result.Message.Contains("EnvironmentHazard") &&
        result.Message.Contains("Hazards")));
}

[Test]
public void ValidateAll_ReportsWarning_WhenEnvironmentHazardHasNoTargetLayer()
{
    var roomRig = CreateValidRoomRig("Room_Hazard_MissingLayer");
    var hazardObject = CreateGameObjectWithComponent<ContactHazard>("Hazard_MissingLayer");
    hazardObject.transform.SetParent(roomRig.HazardsRoot, false);
    hazardObject.AddComponent<BoxCollider2D>().isTrigger = true;

    var results = LevelValidator.ValidateAll();

    Assert.That(results.Any(result =>
        result.TargetObject == hazardObject.GetComponent<ContactHazard>() &&
        result.Severity == LevelValidator.Severity.Warning &&
        result.Message.Contains("_targetLayer")));
}
```

- [ ] **Step 2: 先运行 `LevelValidatorTests`，确认新测试会失败**

Run in Unity Editor:

- 打开 `Window > General > Test Runner`
- 切到 `EditMode`
- 运行 `LevelValidatorTests`

Expected:

- 新增的 `EnvironmentHazardHasNoTargetLayer` 失败
- 失败原因是 `LevelValidator` 目前还没有 hazard 专属验证入口

- [ ] **Step 3: 在 `ValidateAll()` 里加入 hazard 验证调用**

In `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`, change the validation call chain so this exact block appears in `ValidateAll()`:

```csharp
ValidateLocks();
ValidateCheckpoints();
ValidateOpenEncounterTriggers();
ValidateHiddenAreaMasks();
ValidateBiomeTriggers();
ValidateScheduledBehaviours();
ValidateActivationGroups();
ValidateEnvironmentHazards();
ValidatePreferredAuthoringRoots();
```

- [ ] **Step 4: 在同文件中新增 `ValidateEnvironmentHazards()` 方法**

Insert this exact method into `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`, just before `ValidatePreferredAuthoringRoots()`:

```csharp
private static void ValidateEnvironmentHazards()
{
    var hazards = UnityEngine.Object.FindObjectsByType<EnvironmentHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    foreach (var hazard in hazards)
    {
        if (hazard == null) continue;

        string componentLabel = hazard.GetType().Name;
        ValidateTriggerCollider(hazard, componentLabel);
        ValidateLayerMask(hazard, "_targetLayer", componentLabel);
    }
}
```

- [ ] **Step 5: 重新运行 `LevelValidatorTests`，确认 hazard regression tests 通过**

Run in Unity Editor:

- 打开 `Window > General > Test Runner`
- 切到 `EditMode`
- 运行 `LevelValidatorTests`

Expected:

- 现有测试继续通过
- 新增的两条 hazard 测试通过
- 如果失败，先检查 `_targetLayer` 字段名是否拼错，再检查 `ValidateAll()` 是否真的调用了 `ValidateEnvironmentHazards()`

---

### Task 5: 同步权威矩阵与实现日志

**Files:**

- Modify: `Docs/6_Diagnostics/LevelArchitect_SupportedElements_Matrix.md`
- Modify: `Docs/5_ImplementationLog/ImplementationLog_2026-04.md`

- [ ] **Step 1: 把 `Hazards` starter 支持写回支持矩阵**

In `Docs/6_Diagnostics/LevelArchitect_SupportedElements_Matrix.md`, make these exact documentation changes:

1. 在 `## 5.1 当前已开放的 Room Runtime Assist` 表里，插入以下三行，位置放在 `WorldEventTrigger` 与 `Lock Starter` 之间：

```markdown
| `ContactHazard` | **引导式起点** | `Hazards` | 会创建 trigger collider + `ContactHazard` 组件；创建后仍需手动调伤害、击退和命中冷却。 |
| `DamageZone` | **引导式起点** | `Hazards` | 会创建 trigger collider + `DamageZone` 组件；创建后仍需手动调伤害、tick 间隔和覆盖范围。 |
| `TimedHazard` | **引导式起点** | `Hazards` | 会创建 trigger collider + `TimedHazard` 组件；创建后仍需手动调激活周期、命中冷却和视觉同步。 |
```

1. 在 `## 5.2 当前 validator 已覆盖，但未开放直接创建按钮的元素` 表里，删除这一行：

```markdown
| `EnvironmentHazard` | **运行时支持未开放** | validator 会检查根节点，但无 starter 按钮 |
```

1. 在 `## 7. 当前“支持搭建元素”总表` 里，紧接在现有 `WorldEventTrigger` 行后面加入这三行：

```markdown
| Element | `ContactHazard` | **引导式起点** | `Runtime Assist` |
| Element | `DamageZone` | **引导式起点** | `Runtime Assist` |
| Element | `TimedHazard` | **引导式起点** | `Runtime Assist` |
```

1. 在 `## 8. 当前明确不应计入“Level Architect 已支持搭建”的项` 中，把这一行：

```markdown
| `HiddenAreaMask` / `ActivationGroup` / `DestroyableObject` / `EnvironmentHazard` 的 starter 创建 | 当前无 authoring 按钮 |
```

改成：

```markdown
| `HiddenAreaMask` / `ActivationGroup` / `DestroyableObject` 的 starter 创建 | 当前无 authoring 按钮 |
```

- [ ] **Step 2: 追加实现日志，记录 `Hazards starter-first` 落地**

At the top of `Docs/5_ImplementationLog/ImplementationLog_2026-04.md`, insert a new entry using the project's standard structure. Use the actual local execution time in the title, and use this exact body text:

```markdown
## Level Architect Hazards starter-first authoring 接入 — 2026-04-14 11:27

### 新建文件
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactoryTests.cs`
- `Docs/9_Superpowers/plans/2026-04-14-level-architect-hazards-starter-first-implementation-plan.md`

### 修改文件
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactory.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`
- `Docs/6_Diagnostics/LevelArchitect_SupportedElements_Matrix.md`
- `Docs/5_ImplementationLog/ImplementationLog_2026-04.md`

### 内容
- 为 `Level Architect` 的 `Runtime Assist` 新增 `Hazards` 分组，并开放 `ContactHazard`、`DamageZone`、`TimedHazard` 三种 starter-first 创建入口，使现有 hazard family 正式进入 room authoring 主链。
- 在 `LevelRuntimeAssistFactory` 中补齐 `Hazards` 根下的标准 starter 创建逻辑：统一创建 trigger collider、挂载具体 hazard 组件，并默认把 `_targetLayer` 指向 `Player`，让作者创建后可以直接进入参数细调。
- 在 `LevelValidator` 中新增 `EnvironmentHazard` 基础验证，覆盖 Collider Trigger 与 `_targetLayer` 两项高频 authoring 错误；同时补充对应的 EditMode regression tests，防止后续回归。
- 同步更新 `LevelArchitect_SupportedElements_Matrix.md`，把 hazards 从“运行时支持未开放”升级为现役可创建 starter 的 authoring 项。

### 目的
- 先打通 `Hazards` 这条最强语义、最高复用的 room element authoring 闭环，验证 spec 中“starter-first + validator”策略在 `Level Architect` 上的落地方式。
- 为后续 `Traversal / Secret`、`Interaction`、`Group / World` 三条扩展链提供一套可复用的接入模板。

### 技术
- Level Architect starter-first authoring：复用现有 `EnvironmentHazard` family，在编辑器侧只补标准创建入口，不额外发明新的运行时系统。
- 回归防护：使用 EditMode tests 固定 `Hazards` 根、Trigger Collider、`_targetLayer` 三类高频 authoring 约束。
```

- [ ] **Step 3: 检查文档变更范围，确认这轮只回写矩阵与实现日志**

Run:

```powershell
git -C "f:\UnityProjects\Project-Ark" --no-pager diff --stat -- "Docs/6_Diagnostics/LevelArchitect_SupportedElements_Matrix.md" "Docs/5_ImplementationLog/ImplementationLog_2026-04.md"
```

Expected:

- 只看到这两个文档的改动统计
- `LevelArchitect_SupportedElements_Matrix.md` 明确出现 `ContactHazard`、`DamageZone`、`TimedHazard`

---

### Task 6: 做完整验证并停在可 review 状态

**Files:**

- Verify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactoryTests.cs`
- Verify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`
- Verify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactory.cs`
- Verify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs`
- Verify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`
- Verify: `Docs/6_Diagnostics/LevelArchitect_SupportedElements_Matrix.md`
- Verify: `Docs/5_ImplementationLog/ImplementationLog_2026-04.md`

- [ ] **Step 1: 做最终编译检查**

Run:

```powershell
dotnet build "f:\UnityProjects\Project-Ark\Project-Ark.slnx"
```

Expected:

- 构建成功
- 不应出现新的 `Level Architect` / `LevelValidator` / `Hazard` 编译错误

- [ ] **Step 2: 在 Unity Test Runner 中跑本轮相关 EditMode tests**

Run in Unity Editor:

- 打开 `Window > General > Test Runner`
- 切到 `EditMode`
- 运行 `LevelRuntimeAssistFactoryTests`
- 运行 `LevelValidatorTests`

Expected:

- 两个测试类全部通过
- 如果 `LevelValidatorTests` 中旧测试失败，先确认这轮没有误改非 hazard 验证逻辑

- [ ] **Step 3: 做一次房间 authoring 手工 smoke test**

Run in Unity Editor:

1. 打开一个已接好 `RoomManager` 的测试场景
2. 选中一个 `Room`
3. 在 `Level Architect > Runtime Assist` 中依次点击：
   - `Contact Hazard`
   - `Damage Zone`
   - `Timed Hazard`
4. 对每个新对象检查：
   - 父节点是否为房间下的 `Hazards`
   - 是否存在 `Collider2D`
   - `Collider2D.isTrigger` 是否为 `true`
   - 对应 hazard 组件是否正确挂载
   - `_targetLayer` 是否指向 `Player`
5. 点击 `Validate All`

Expected:

- 新对象均落在 `Hazards` 根下
- validator 不再把 hazard 视为“未开放入口”对象
- 如果没有补业务参数，只允许出现合理的 warning，不应出现空引用级错误

- [ ] **Step 4: 检查最终 diff，停在待 review 状态，不要自行提交**

Run:

```powershell
git -C "f:\UnityProjects\Project-Ark" --no-pager diff --stat
```

Expected:

- 只看到本计划涉及的脚本与文档
- **不要提交**；按当前协作约束，除非用户明确要求，否则只把改动留在工作区供 review

---

## Self-Review

### Spec Coverage

- `Hazards` 作为第一批优先语义元素进入 `Level Architect`：由 Task 2 / Task 3 落地
- `starter-first` 而不是完整 inspector：由 Task 2 的三种 starter 方法落地
- validator 必须跟上：由 Task 4 落地
- 支持矩阵必须同步回写：由 Task 5 落地
- 只做首个子项目、不扩张到其他子系统：由本计划的 scope split 控制

### Placeholder Scan

- 没有 `TODO` / `TBD`
- 所有代码步骤都给了 exact content
- 验证步骤都给了明确命令或 Unity 菜单路径

### Type Consistency

- starter 类型统一使用：`ContactHazard` / `DamageZone` / `TimedHazard`
- runtime assist 枚举统一使用：`LevelRuntimeAssistFactory.RoomAssistType.*`
- validator 字段统一使用：`_targetLayer`
- authoring 根统一使用：`Hazards`

---

## Execution Handoff

Plan complete and saved to `Docs/9_Superpowers/plans/2026-04-14-level-architect-hazards-starter-first-implementation-plan.md`. Two execution options:

**1. Subagent-Driven (recommended)** - 我按任务逐个派新的执行 agent，下一个任务做完就 review 一次，风险最低。

**2. Inline Execution** - 我在当前会话里直接按计划开始实现，并在关键检查点停下来给你看。

**你想用哪种方式？**
