# WkecWulin 背包物品 Drag & Drop 系统 — 完整逻辑拆解

> 分析版本：WkecWulin v0.2.3_9345  
> 分析日期：2026-03-03  
> 目的：为 Project Ark 星图部件拖拽功能提供参考蓝本

---

## 目录

1. [系统总览与架构图](#1-系统总览与架构图)
2. [核心类职责说明](#2-核心类职责说明)
3. [数据层：DragDropItem & DragDropItemSettings](#3-数据层dragdropitem--dragdropitemsettings)
4. [拖拽源：UIDragDropHandler](#4-拖拽源uidragdrophandler)
5. [槽位视图：UIDragDropItem](#5-槽位视图uidragdropitem)
6. [放置容器：UIDragDropContainer](#6-放置容器uidragdropcontainer)
7. [放置验证：UIDropLogic](#7-放置验证uidroplogic)
8. [全局协调者：UIDragDropVirtualPanel](#8-全局协调者uidragdropvirtualpanel)
9. [商品格：UIShopItemSlot](#9-商品格uishopitemslot)
10. [商品生成器：UIDragDropItemGenerator](#10-商品生成器uidragdropitemgenerator)
11. [辅助系统：音效 & 移动监听](#11-辅助系统音效--移动监听)
12. [完整事件流时序图](#12-完整事件流时序图)
13. [关键设计模式与亮点](#13-关键设计模式与亮点)
14. [对 Project Ark 的借鉴建议](#14-对-project-ark-的借鉴建议)

---

## 1. 系统总览与架构图

WkecWulin 的拖拽系统是一套**五层分离**的架构，每一层职责单一、通过事件解耦：

```
┌─────────────────────────────────────────────────────────────────┐
│                    UIDragDropVirtualPanel                        │
│  (全局协调者 · 场景单例 · 事件广播 · 数据中台 · Ghost渲染)        │
└──────────────┬──────────────────────────────┬───────────────────┘
               │ 注册/广播                     │ 查询/转移
               ▼                              ▼
┌──────────────────────────┐    ┌─────────────────────────────────┐
│   UIDragDropContainer    │    │        DragDropItem (数据)       │
│  (放置目标 · 槽位管理)    │    │  DragDropItemSettings (SO配置)  │
│  + UIDropLogic (验证器)  │    │  ItemUnitManager (实例管理)      │
└──────────────┬───────────┘    └─────────────────────────────────┘
               │ 包含
               ▼
┌──────────────────────────┐
│      UIDragDropItem      │
│  (槽位视图 · 图标渲染)    │
│  + UIDragDropHandler     │  ← 拖拽事件源 (IBeginDragHandler等)
│  + UIDragDropSound       │  ← 音效挂载
└──────────────────────────┘
```

### 场景 GameObject 层级结构

```
[Canvas]
  └── UIDragDropVirtualPanel (MonoBehaviour)
        ├── DragDisplayTransform  ← Ghost图标 (跟随鼠标的虚拟图标)
        └── TipsUI[]              ← 悬停信息弹窗

[背包面板]
  └── UIDragDropContainer (MonoBehaviour)
        ├── UIDropLogic (MonoBehaviour)  ← 验证器
        └── ItemSlotsParent
              ├── Slot_0
              │     └── UIDragDropItem (MonoBehaviour)
              │           ├── UIDragDropHandler (MonoBehaviour)  ← 拖拽源
              │           └── UIDragDropSound (MonoBehaviour)    ← 音效
              ├── Slot_1
              └── ...

[商店面板]
  └── UIDragDropContainer (MonoBehaviour)
        ├── UIDropLogic (MonoBehaviour)
        └── ItemSlotsParent
              └── UIShopItemSlot (MonoBehaviour)
                    └── UIDragDropItem + UIDragDropHandler
```

---

## 2. 核心类职责说明

| 类名 | 文件 | 职责 |
|------|------|------|
| `UIDragDropVirtualPanel` | `UI/UIDragDropVirtualPanel.cs` | 场景级单例协调者，管理所有Container注册、Ghost渲染、事件广播、物品数据中台、物品转移 |
| `UIDragDropContainer` | `UI/UIDragDropContainer.cs` | 放置目标容器，管理内部槽位列表，响应拖拽广播，执行Drop验证与物品增删 |
| `UIDropLogic` | `UI/UIDropLogic.cs` | 独立的Drop条件验证器，支持类型过滤、来源过滤、容量限制、自动转移 |
| `UIDragDropItem` | `UI/UIDragDropItem.cs` | 单个槽位的视图层，负责图标渲染、稀有度显示、锁定状态、拖拽状态同步 |
| `UIDragDropHandler` | `UI/UIDragDropHandler.cs` | 拖拽事件源，实现Unity EventSystem的Drag接口，携带物品元数据，触发VirtualPanel |
| `DragDropItem` | `UI/Data/DragDropItem.cs` | 物品数据模型（可序列化），包含图标、属性、实例ID、容器归属 |
| `DragDropItemSettings` | `UI/Data/DragDropItemSettings.cs` | ScriptableObject配置表，存储所有可拖拽物品的定义，提供ID查找 |
| `UIDragDropItemGenerator` | `UI/UIDragDropItemGenerator.cs` | 商店物品生成器，负责随机生成、按概率表生成、存档恢复 |
| `UIShopItemSlot` | `UI/UIShopItemSlot.cs` | 商店格子视图，包装UIDragDropItem，增加锁定/预购/推荐等商店专属功能 |
| `UIDragDropSound` | `UI/UIDragDropSound.cs` | 音效挂载组件，监听Handler的Begin/End事件播放对应音效 |
| `UIMoveDropListener` | `UI/UIMoveDropListener.cs` | 特殊Drop监听器，计算拖拽偏移量（用于自由放置场景） |

---

## 3. 数据层：DragDropItem & DragDropItemSettings

### 3.1 DragDropItem — 物品数据模型

```csharp
[System.Serializable]
public class DragDropItem
{
    // 基础属性
    public int ItemId;       // 物品定义ID（模板ID）
    public int ItemType;     // 物品类型（1=宝具, 2=秘籍）
    public int Rarity;       // 稀有度（1白/2蓝/3紫/4金）
    public int Sect;         // 门派归属
    public int Weight;       // 负重
    public int Price;        // 买价
    public int Sell;         // 售价

    // 图标（三档尺寸）
    public Sprite _bigTexture;   // 大图标（拖拽Ghost用）
    public Sprite _bagTexture;   // 中图标（背包格用）
    public Sprite _smallTexture; // 小图标

    // 运行时数据（Clone时赋值）
    public int InstId;       // 实例ID（唯一，由ItemUnitManager分配）
    public int ContainerId;  // 当前所在容器ID

    // 网格偏移（用于图标在格子内的位置微调）
    public int[] GridData = new int[] { 0, 0 };
}
```

**关键设计：Clone模式**

`DragDropItem` 是**模板数据**，运行时通过 `Clone(instId, containerId)` 创建实例副本：

```csharp
public DragDropItem Clone(int instId, int containerId)
{
    DragDropItem clone = new DragDropItem();
    // 复制所有模板数据
    clone.ItemId = this.ItemId;
    clone.Rarity = this.Rarity;
    // ...
    clone.InstId = instId;          // 注入实例ID
    clone.SetContainerTo(containerId); // 注入容器归属
    return clone;
}
```

这样 SO 中的模板数据永远不被修改（符合 Project Ark 的 SO 不可运行时修改原则）。

### 3.2 DragDropItemSettings — SO配置表

```csharp
[CreateAssetMenu(menuName = "SO/DragDropItemSettings")]
public class DragDropItemSettings : ScriptableObject
{
    public List<DragDropItem> _ItemList;
    private Dictionary<int, DragDropItem> _Dict; // 懒加载字典，O(1)查找

    public DragDropItem GetByItemId(int itemId) { ... }
    public List<DragDropItem> GetAllItems() { ... }
}
```

**注意**：字典是懒加载的（`CheckInit()`），首次查询时才构建，避免 SO 初始化顺序问题。

### 3.3 ItemUnitManager — 实例ID管理器

负责分配全局唯一的 `InstId`（实例序列号），以及 `RecoverItem`（存档恢复时重建实例）。

```
ItemUnitManager.CreateItem(itemId)   → 分配新InstId，返回ItemUnit
ItemUnitManager.RecoverItem(item)    → 从存档数据恢复，保持原InstId
ItemUnitManager.DestroyItem(instId)  → 回收InstId
ItemUnitManager.FixItemInstanceSerial(maxId) → 修正序列号（防止与存档ID冲突）
```

---

## 4. 拖拽源：UIDragDropHandler

**文件**：`UI/UIDragDropHandler.cs`  
**接口**：`IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler`

### 4.1 携带的元数据

```csharp
public int _CarryType = 0;    // 物品类型（0=宝具, 1=技能）
public int _CarryId = 0;      // 物品定义ID
public int _CarryInstId = 0;  // 物品实例ID（关键！用于跨容器识别）
public int _CarryLevel = 1;   // 角色等级（影响Tips显示）
public int _CarryGroupId = 0; // 分组ID（影响Tips位置）
```

### 4.2 拖拽生命周期

```
OnBeginDrag()
  ├── 检查 _EnableDrag 开关
  ├── 记录 _BeginDragPosition（静态，供UIMoveDropListener计算偏移）
  ├── 设置 _IsDragging = true
  ├── 调用 VirtualPanel.OnBeginDrag(this, rectTransform, instId)
  ├── 调用 VirtualPanel.Drag(eventData)  ← 立即同步Ghost位置
  ├── LockSelf()  ← 原图标半透明 + 关闭Raycast
  └── 触发 _OnBeginDraggingAction  ← 通知UIDragDropItem/UIDragDropSound

OnDrag()
  └── 调用 VirtualPanel.Drag(eventData)  ← 持续更新Ghost位置

OnEndDrag()
  ├── 调用 VirtualPanel.OnEndDrag(eventData)
  ├── 设置 _IsDragging = false
  ├── UnLockSelf()  ← 恢复原图标
  └── 触发 _OnEndDraggingAction
```

### 4.3 LockSelf / UnLockSelf 机制

拖拽开始时，原图标进入"锁定"状态：
- `Image.color.a = 0.5f`（半透明，表示"正在被拖走"）
- `Image.raycastTarget = false`（关闭射线检测，防止自身遮挡Drop目标）

拖拽结束或 `OnDropEnd()` 被调用时恢复。

### 4.4 Tips（悬停信息弹窗）系统

`_EnableShowTips = true` 时，`OnPointerEnter/Move/Exit` 会调用 `VirtualPanel.ShowTips()`，传入物品的完整元数据，由 VirtualPanel 根据 `_CarryType` 找到对应的 `UIDragDropItem_Tips` 子类显示。

### 4.5 Outline（描边）系统

通过 `_WeaponOutlineMaterial` 材质球实现描边效果，可通过 `SwitchOutline(bool)` 控制。

---

## 5. 槽位视图：UIDragDropItem

**文件**：`UI/UIDragDropItem.cs`

### 5.1 核心状态

```csharp
private int _InstanceId;  // 当前存放的物品实例ID（-1表示空槽）
private int _ItemId;      // 当前存放的物品定义ID（-1表示空槽）
private int _ContainerId; // 所属容器ID
public bool HasItem { get { return ItemId > 0; } }
```

### 5.2 与 UIDragDropHandler 的绑定

`Awake()` 时自动查找同 GameObject 上的 `UIDragDropHandler`，并注入回调：

```csharp
_DragHandler._OnBeginDraggingAction = this.OnBeginDragging;
_DragHandler._OnEndDraggingAction = this.OnEndDragging;
```

这样 `UIDragDropItem` 可以感知拖拽状态，用于防止"拖拽结束后误触锁定按钮"：

```csharp
// SwitchLocked() 中的防误触逻辑
float lastDraggingTimePass = Time.time - _LastDraggingTime;
bool isAfterDragging = lastDraggingTimePass < 0.5f; // 拖拽后0.5秒内忽略点击
if (isAfterDragging) return;
```

### 5.3 SetupItem — 物品装填

```csharp
public void SetupItem(int itemInstId)
{
    _InstanceId = itemInstId;
    var item = VirtualPanel.GetItemByInstId(itemInstId); // 从VirtualPanel数据中台查询
    SetupItemData(item);  // 渲染图标、名称、稀有度、价格、负重
    DragHandler._CarryInstId = item.InstId;  // 同步Handler的携带数据
    DragHandler._CarryId = item.ItemId;
    ActiveSelf();  // SetActive(true)
}
```

### 5.4 RemoveItem — 物品移除

```csharp
public void RemoveItem()
{
    _InstanceId = -1;
    _ItemId = -1;
    _Item = null;
    ResetLayous();  // 清空图标、名称等UI
    DeactiveSelf(); // SetActive(false)
}
```

**注意**：`RemoveItem()` 不销毁实例ID，`DestroyItem()` 才会调用 `ItemUnitManager.DestroyItem()`。

### 5.5 AutoDisableParent 机制

`_AutoDisableParent = true` 时，物品存在时隐藏父级背景图（`_Parent.enabled = false`），物品移除时显示父级背景图。这实现了"有物品时显示物品图标，无物品时显示空槽背景"的效果。

---

## 6. 放置容器：UIDragDropContainer

**文件**：`UI/UIDragDropContainer.cs`  
**接口**：`IDropHandler, IPointerEnterHandler, IPointerExitHandler`

### 6.1 核心状态机

```
_ListenDrag: bool  ← 是否正在监听当前拖拽（由OnBeginDragEvent设置）
_DragInside: bool  ← 拖拽物是否在本容器范围内（由OnPointerEnter/Exit设置）
```

状态变化触发 UnityEvent：
- `_OnListenEvent(bool)` — 监听状态变化时（可用于高亮边框）
- `_OnInsideEvent(bool)` — 进入/离开时（可用于更强烈的高亮）
- `_OnBeginDragAndListenEvent(int)` — 开始拖拽且符合条件时（可用于预高亮）
- `_OnDropSuccessEvent(int)` — 成功放入时（触发游戏逻辑）
- `_OnRemoveItemEvent(int)` — 物品被移除时
- `_OnItemAddRemoveEvent()` — 任何增删事件时

### 6.2 OnBeginDragEvent — 广播响应

VirtualPanel 在拖拽开始时广播给所有注册的 Container：

```csharp
public void OnBeginDragEvent(int carryId)
{
    SetListen(false);
    SetInside(false);

    if (_EnableDrop)
    {
        // 提前验证：此物品是否符合本容器的放置条件
        if (_DropLogic != null && _DropLogic.CheckDropCondition(carryId))
        {
            SetListen(true);  // 进入监听状态
            _OnBeginDragAndListenEvent?.Invoke(carryId); // 触发预高亮
        }
    }
}
```

**关键点**：Container 在拖拽**开始时**就完成了条件预判，而不是等到鼠标悬停时才判断。这意味着所有符合条件的容器会**同时高亮**，给玩家清晰的视觉引导。

### 6.3 两种 Inside 检测模式

**模式一：PointerEnter/Exit（默认）**

依赖 Unity EventSystem 的 `IPointerEnterHandler`，适合普通情况。

```csharp
public void OnPointerEnter(PointerEventData eventData)
{
    if (_ListenDrag && _EnableDrop)
    {
        SetInside(true); // 进入高亮
    }
}
```

**模式二：CheckInsideOnDragIntoRect（矩形检测）**

`_CheckInsideOnDragIntoRect = true` 时，在 `OnDragEvent` 中主动检测鼠标是否在 RectTransform 内：

```csharp
public void OnDragEvent(PointerEventData eventData)
{
    if (_ListenDrag && _CheckInsideOnDragIntoRect)
    {
        var inside = VirtualPanel.ScreenPointToLocalPointInRectangle(
            _RectTransform, eventData.position, out Vector2 localPos);
        if (inside != _DragInside)
        {
            SetInside(false); // 注意：这里只处理离开，进入由PointerEnter处理
        }
    }
}
```

此模式适用于 Canvas 层级复杂、`PointerEnter` 事件被遮挡的情况。

### 6.4 OnDrop — 放置处理

```csharp
public void OnDrop(PointerEventData eventData)
{
    int itemInstId = VirtualPanel.CurrentItemInstId;

    if (_ListenDrag)
    {
        SetListen(false);

        if (_EnableDrop)
        {
            bool dropSuccess = false;
            if (_DropLogic != null
                && _DropLogic.CheckDropCondition(itemInstId)  // 二次验证
                && _DropLogic._DragListenOnly == false)       // 非只监听模式
            {
                dropSuccess = true;
            }
            if (dropSuccess)
            {
                VirtualPanel.OnDropSuccess(eventData, itemInstId, this.InstanceId);
            }
        }
    }

    VirtualPanel.OnDropEnd(); // 无论成功与否，都结束拖拽状态
    SetInside(false);
}
```

**注意**：`CheckDropCondition` 在 `OnBeginDragEvent` 和 `OnDrop` 中各调用一次（双重验证），防止拖拽过程中状态变化导致的非法放置。

### 6.5 槽位管理

```csharp
// 重新收集所有子槽位（LateUpdate中检查_SlotDirty标志）
protected void ReCollectSlots()
{
    _DragDropItemSlots.Clear();
    // 遍历 _ItemSlotsParent 的所有子节点
    // 找到 UIDragDropItem 组件
    // 统计 HasItem 数量
}

// 添加物品到第一个空槽
public bool AddItem(int itemInstId)
{
    // 1. 检查DropLogic条件
    // 2. 找到空槽 DropItemToEmptySlot()
    // 3. 更新_ItemCnt
    // 4. 触发事件
}
```

---

## 7. 放置验证：UIDropLogic

**文件**：`UI/UIDropLogic.cs`

### 7.1 验证条件（按优先级）

```
1. DenyDrop == true          → 拒绝（临时禁止，可动态设置）
2. item == null || ItemId<=0 → 拒绝（非法物品）
3. CheckHasItem(instId)      → 拒绝（已存放此物品，防重复）
4. CheckItemLimits()         → 拒绝（容量已满，除非_AutoTransfer）
5. _CheckItemGroupId         → 检查物品类型是否在白名单内
6. _CheckContainerId         → 检查来源容器是否在白名单内
7. 全部通过                  → 允许
```

### 7.2 关键配置字段

```csharp
public int _ItemInstLimits = -1;    // 容量上限（-1=无限）
public bool _DragListenOnly = false; // 只监听不接受（用于"预览"容器）
public bool _CheckItemGroupId;       // 是否限制物品类型
public int[] _DropItemGroupIds;      // 允许的物品类型列表
public bool _CheckContainerId;       // 是否限制来源容器
public int[] _DropContainerIds;      // 允许的来源容器列表
public bool _AutoTransfer;           // 满时自动转移旧物品
public int _AutoTransferContainerId; // 自动转移目标容器ID
```

### 7.3 AutoTransfer — 自动转移机制

当容器已满且 `_AutoTransfer = true` 时，放入新物品会自动将最旧的物品（`_ItemInstList[0]`）转移到 `_AutoTransferContainerId` 指定的容器：

```csharp
private bool TransferItem()
{
    var container = VirtualPanel.GetContainerByInstId(_AutoTransferContainerId);
    var itemInstId = _ItemInstList[0]; // 最旧的物品
    VirtualPanel.TransferItem(itemInstId, container.InstanceId);
    return true;
}
```

这实现了"背包满时自动将最旧物品移到仓库"的功能。

---

## 8. 全局协调者：UIDragDropVirtualPanel

**文件**：`UI/UIDragDropVirtualPanel.cs`

### 8.1 场景单例模式

```csharp
// 按场景名存储，支持多场景
private static Dictionary<string, UIDragDropVirtualPanel> _Dictionary_DragDropVirtualPanels;

// 任何组件都可以通过此方法获取当前场景的VirtualPanel
public static UIDragDropVirtualPanel CurrentDragDropVirtualPanel(GameObject go)
{
    var key = go.scene.name;
    return _Dictionary_DragDropVirtualPanels[key];
}
```

**优点**：不使用 `FindObjectOfType`，不使用全局静态单例，支持多场景共存。

### 8.2 Ghost 渲染系统

```csharp
// 拖拽开始时：复制原图标到Ghost对象
private void CopyTarget(RectTransform rectTarget)
{
    var item = GetItemByInstId(_CurrentItemInstId);
    var sprite = _UseBigIcon ? item.BigIcon() : item.SmallIcon();
    var img = _VirtualTarget.gameObject.GetComponent<Image>();
    img.sprite = sprite;
    if (_UseNativeSize) img.SetNativeSize(); // 使用原始尺寸
    ShowVirtualTarget(); // SetActive(true)
}

// 拖拽过程中：每帧更新Ghost位置
public void Drag(PointerEventData eventData)
{
    var pos = ScreenToWorldPoint(eventData.position); // 屏幕坐标→世界坐标
    _VirtualTarget.position = pos;
    OnDragEvent(eventData); // 广播给所有Container
}
```

Ghost 对象（`_DragDisplayTransform`）始终在 Canvas 最顶层，确保不被其他UI遮挡。

### 8.3 事件广播机制

```csharp
// 拖拽开始：广播给所有Container
private void OnBeginDragEvent()
{
    foreach (var container in _DragDropContainersList)
    {
        container.OnBeginDragEvent(_CurrentItemInstId);
    }
}

// 拖拽中：广播给所有Container（用于矩形检测模式）
private void OnDragEvent(PointerEventData eventData)
{
    foreach (var container in _DragDropContainersList)
    {
        container.OnDragEvent(eventData);
    }
}

// 拖拽结束：广播给所有Container
private void OnEndDragEvent(PointerEventData eventData)
{
    foreach (var container in _DragDropContainersList)
    {
        container.OnEndDragEvent(eventData);
    }
    ReleaseDragHandler(); // 释放当前拖拽Handler引用
}
```

### 8.4 物品数据中台

VirtualPanel 是运行时物品实例数据的唯一持有者：

```csharp
private Dictionary<int, DragDropItem> _DragDropItemsDict; // instId → DragDropItem实例

// 创建新物品实例
public int CreateItem(int itemId, int containerId)
{
    var itemDef = GetItemByItemId(itemId);       // 从SO查模板
    var unit = ItemUnitManager.CreateItem(itemId); // 分配InstId
    var itemClone = itemDef.Clone(unit.Inst, containerId); // 创建副本
    _DragDropItemsDict[unit.Inst] = itemClone;   // 注册到数据中台
    return unit.Inst;
}

// 查询物品实例
public DragDropItem GetItemByInstId(int itemInstId)
{
    return _DragDropItemsDict[itemInstId];
}
```

### 8.5 TransferItem — 物品转移

```csharp
public void TransferItem(int itemInstId, int toContainerId)
{
    var item = GetItemByInstId(itemInstId);
    var toContainer = GetContainerByInstId(toContainerId);
    int fromContainerId = item.ContainerId;

    if (item.ContainerId != toContainer.InstanceId)
    {
        var fromContainer = GetContainerByInstId(item.ContainerId);
        fromContainer.RemoveItem(itemInstId);        // 从原容器移除
        item.SetContainerTo(toContainer.InstanceId); // 更新数据归属
        toContainer.AddItem(itemInstId);             // 加入新容器

        _OnEvent_ItemTransfer?.Invoke(new int[] { itemInstId, fromContainerId, toContainerId });
    }
}
```

### 8.6 Tips 系统

支持多种物品类型的信息弹窗，通过 `ItemTipsTypeName` 枚举区分：

```csharp
public enum ItemTipsTypeName
{
    None = 0,
    Item = 1,    // 宝具类
    Skill = 2,   // 技能类
    Buff = 3,    // Buff类
    CharAttr = 4,// 角色属性
    Destiny = 5, // 天命卡牌
    FormulaLog = 99, // 数值公式日志（调试用）
}
```

每种类型对应一个 `UIDragDropItem_Tips` 子类实例，通过 `_TipsPositions` 配置显示位置。

### 8.7 商店来源检测

```csharp
// 配置哪些ContainerId属于"商店"
public int[] _ShopContainerIds;
private HashSet<int> _ShopIds;

// 拖拽开始时检测来源
if (_ShopIds.Contains(item.ContainerId))
{
    _OnBeginDragItemFromShop?.Invoke(itemInstId); // 触发商店专属逻辑
}
```

---

## 9. 商品格：UIShopItemSlot

**文件**：`UI/UIShopItemSlot.cs`

`UIShopItemSlot` 是对 `UIDragDropItem` 的包装，增加了商店专属功能：

| 功能 | 实现 |
|------|------|
| 格子锁定 | `_SlotIsLocked` + `_ImageSlotIsLocked` GameObject |
| 物品锁定 | 委托给 `_DragDropItem.SetIsLocked()` |
| 稀有度边框 | `_ItemFrames[]` 数组，按 `RarityId` 切换 Sprite |
| 预购显示 | `_PreOrder` GameObject + `_PreOrderIcon` |
| 推荐标记 | `RecommendationsItem` GameObject |
| 悬停回调 | `_ActionOnPointerEnter/Exit`（Action委托，非UnityEvent） |

**格子锁定 vs 物品锁定的区别**：
- **格子锁定**（`_SlotIsLocked`）：整个格子不可用，不显示物品，不可拖拽
- **物品锁定**（`_DragDropItem._Locked`）：物品存在但被玩家锁定，刷新商店时不会替换此物品

---

## 10. 商品生成器：UIDragDropItemGenerator

**文件**：`UI/UIDragDropItemGenerator.cs`

### 10.1 三种生成模式

```
_RandomMode = false → GM模式：按顺序遍历物品列表（调试用）
_RandomMode = true, _RandomByTable = false → 普通随机：完全随机
_RandomMode = true, _RandomByTable = true  → 概率表随机：按稀有度权重随机
```

### 10.2 概率表随机流程

```
GenerateItems()
  ├── InitLockedShopItems()     ← 收集被锁定的商品（不重新生成）
  ├── CollectItemDef()          ← 过滤可生成的物品列表
  ├── InitGenerationCondition() ← 收集玩家已持有物品（用于限制重复）
  └── GenerateItems_TableRandom()
        ├── 前2格：尝试生成秘籍（ScrollBook_RandomRange）
        └── 剩余格：按稀有度权重生成宝具（GenerateItems_TableRandom_Weapon）
              ├── 从22_Parameter表读取品阶权重 [白,蓝,紫,金]
              ├── 应用玩家特殊加成（ExtraPriority）
              ├── Rarity_RandomRange() 随机品阶
              └── 从该品阶物品中随机选一个
```

### 10.3 生成约束条件

```csharp
// 检查物品是否可生成
private bool CheckRandomlItemValid(int itemId, List<int> bucket)
{
    // 1. 玩家已持有此秘籍 → 不生成
    // 2. bucket中已有此ID → 不生成（同一批次不重复）
    // 3. _DistinctBooks中已有 → 不生成（秘籍不重复）
    // 4. 被锁定的商品中已有 → 不生成
}
```

### 10.4 存档与恢复

```csharp
// 保存：将当前商品格的ItemId列表存入PlayerData
public bool SaveShopItemsData()
{
    List<int> itemIds = slots.Select(s => s.HasItem() ? s.ItemId() : -1).ToList();
    playerDataManager._PlayerShopData.SaveShopItems(itemIds);
}

// 恢复：从PlayerData读取ItemId列表，重建物品实例
public void LoadShopItemsFromPlayer()
{
    playerDataManager._PlayerShopData.GetShopItemIds(bucket);
    ItemUnitManager.FixItemInstanceSerial(maxId); // 修正序列号防冲突
    SetupSlots(slots, bucket, false);
}
```

---

## 11. 辅助系统：音效 & 移动监听

### 11.1 UIDragDropSound — 音效系统

挂载在与 `UIDragDropHandler` 相同的 GameObject 上，通过 Action 委托监听拖拽事件：

```csharp
void Start()
{
    uIDragDropHandler._OnBeginDraggingAction += PlayAudio_Start;
    uIDragDropHandler._OnEndDraggingAction += PlayAudio_End;
}

void OnDestroy()
{
    // 正确取消订阅，防止内存泄漏
    uIDragDropHandler._OnBeginDraggingAction -= PlayAudio_Start;
    uIDragDropHandler._OnEndDraggingAction -= PlayAudio_End;
}
```

音效来源：从物品数据表（`Item.SetSound`）查找对应音效，实现每种物品有独特的拖拽音效。

**防重复播放逻辑**：`PlayAudio_End` 中检查 `_CarryId` 和 `_CarryInstId` 是否与开始时一致，防止拖拽过程中物品切换导致的音效错乱。

### 11.2 UIMoveDropListener — 自由放置监听

用于需要计算拖拽**偏移量**的场景（如自由拖放到地图上）：

```csharp
public void OnDrop(PointerEventData eventData)
{
    Vector2 dropPos = ScreenToWorldPoint(eventData.position);
    Vector2 beginDragPos = ScreenToWorldPoint(UIDragDropHandler._BeginDragPosition);
    _Offset = dropPos - beginDragPos; // 计算拖拽偏移量
    _CurrentItemInstId = VirtualPanel.CurrentItemInstId;
    _OnDrop?.Invoke(); // 触发外部逻辑
}
```

---

## 12. 完整事件流时序图

### 12.1 正常拖拽放置流程

```
玩家按下鼠标
    │
    ▼
UIDragDropHandler.OnBeginDrag()
    ├── VirtualPanel.OnBeginDrag(handler, rect, instId)
    │       ├── CopyTarget() → Ghost图标显示
    │       └── OnBeginDragEvent() → 广播给所有Container
    │               └── Container.OnBeginDragEvent(instId)
    │                       ├── DropLogic.CheckDropCondition() → 预判
    │                       ├── SetListen(true)  ← 符合条件的Container进入监听
    │                       └── _OnBeginDragAndListenEvent → 高亮边框
    └── LockSelf() → 原图标半透明

玩家移动鼠标
    │
    ▼
UIDragDropHandler.OnDrag()
    └── VirtualPanel.Drag(eventData)
            ├── Ghost位置更新
            └── OnDragEvent() → 广播给所有Container（矩形检测模式用）

鼠标进入目标Container
    │
    ▼
UIDragDropContainer.OnPointerEnter()
    └── SetInside(true) → _OnInsideEvent → 更强烈高亮

玩家松开鼠标
    │
    ▼
UIDragDropContainer.OnDrop()
    ├── 二次验证 DropLogic.CheckDropCondition()
    ├── VirtualPanel.OnDropSuccess(eventData, instId, containerId)
    │       └── TransferItem(instId, toContainerId)
    │               ├── fromContainer.RemoveItem(instId)
    │               │       ├── DropLogic.RemoveItem()
    │               │       ├── slot.RemoveItem() → 清空图标
    │               │       └── _OnRemoveItemEvent
    │               ├── item.SetContainerTo(toContainerId)
    │               ├── toContainer.AddItem(instId)
    │               │       ├── DropLogic.AddItem()
    │               │       ├── slot.SetupItem() → 显示图标
    │               │       └── _OnDropSuccessEvent
    │               └── _OnEvent_ItemTransfer → 游戏逻辑层
    └── VirtualPanel.OnDropEnd()
            ├── HideVirtualTarget() → Ghost隐藏
            └── OnEndDragEvent() → 广播结束
                    └── Container.OnEndDragEvent()
                            ├── SetListen(false)
                            └── SetInside(false)

UIDragDropHandler.OnEndDrag()
    └── UnLockSelf() → 原图标恢复
```

### 12.2 拖拽失败（放到无效区域）流程

```
玩家松开鼠标（无效区域）
    │
    ▼
UIDragDropHandler.OnEndDrag()
    └── VirtualPanel.OnEndDrag(eventData)
            ├── HideVirtualTarget() → Ghost隐藏
            └── OnEndDragEvent() → 广播结束（所有Container重置状态）

（无OnDrop事件触发，物品留在原位）
```

---

## 13. 关键设计模式与亮点

### 13.1 广播-订阅模式（BeginDrag广播）

**问题**：如何让所有符合条件的容器在拖拽开始时就高亮？

**解法**：VirtualPanel 在 `OnBeginDrag` 时主动广播给所有注册的 Container，Container 自己判断是否进入监听状态。

```
优点：
✓ 不依赖 PointerEnter，即使鼠标没有悬停也能高亮
✓ 新增Container只需注册到VirtualPanel，无需修改其他代码
✓ 高亮逻辑完全由Container自己控制（UnityEvent）
```

### 13.2 双重验证模式

`CheckDropCondition` 在 `OnBeginDragEvent`（预判）和 `OnDrop`（最终验证）各调用一次，防止拖拽过程中状态变化（如容量变化）导致的非法放置。

### 13.3 数据中台模式

所有运行时物品实例数据集中在 `VirtualPanel._DragDropItemsDict` 中，任何组件都通过 `VirtualPanel.GetItemByInstId()` 查询，而不是各自持有数据副本。

```
优点：
✓ 数据一致性保证（单一数据源）
✓ 物品转移只需修改 item.ContainerId，无需同步多处数据
✓ 便于存档（只需序列化这一个字典）
```

### 13.4 InstId 与 ItemId 分离

- `ItemId`：物品定义ID（模板），对应 SO 中的配置，多个实例共享同一 ItemId
- `InstId`：物品实例ID（运行时唯一），由 `ItemUnitManager` 分配

这使得同一种物品可以有多个实例同时存在于不同容器中，且每个实例有独立的状态（锁定、位置等）。

### 13.5 UIDropLogic 独立验证器

验证逻辑从 Container 中剥离为独立组件，支持：
- 在 Inspector 中可视化配置验证条件
- 同一验证逻辑可复用于多个 Container
- `_DragListenOnly` 模式：只高亮不接受（用于"预览"或"提示"容器）

### 13.6 拖拽后防误触机制

`UIDragDropItem` 记录最后一次拖拽结束时间，在 `SwitchLocked()` 中检查是否在 0.5 秒内，防止拖拽结束时的鼠标抬起事件误触锁定按钮。

---

## 14. 对 Project Ark 的借鉴建议

### 14.1 可直接借鉴的设计

#### A. BeginDrag 广播机制（高优先级）

Project Ark 目前的 `HighlightMatchingColumns` 是硬编码的类型匹配。可以借鉴 WkecWulin 的广播模式：

```csharp
// 当前 Project Ark 方式（硬编码）
private void HighlightMatchingColumns(StarChartItemSO item)
{
    foreach (var slot in _allSlots)
    {
        if (slot.AcceptsType(item.SlotType))
            slot.SetHighlight(HighlightState.Valid);
    }
}

// 借鉴后（广播模式）
// DragDropManager.OnBeginDrag() 广播给所有注册的 SlotCellView
// SlotCellView.OnBeginDragEvent() 自己判断是否高亮
// 新增 SlotType 无需修改 DragDropManager
```

#### B. UIDropLogic 独立验证器（中优先级）

将 `SlotCellView` 中的 Drop 条件验证提取为独立的 `StarChartDropLogic` 组件，支持 Inspector 配置。

#### C. 双重验证（低优先级）

在 `OnBeginDrag` 时预判 + `OnDrop` 时最终验证，防止边界情况。

### 14.2 不建议借鉴的部分

| WkecWulin 特性 | 原因 |
|----------------|------|
| `SetActive` 控制显隐 | Project Ark 规范禁止，统一用 CanvasGroup |
| `ItemUnitManager` 全局实例管理 | Project Ark 用 ServiceLocator 模式，不需要全局静态管理器 |
| `UIDragDropItemGenerator` 随机生成 | 星图部件不需要随机生成，是固定的部件列表 |
| `UIShopItemSlot` 商店格 | 星图部件槽位逻辑不同，不需要锁定/预购等商店功能 |

### 14.3 星图部件拖拽的特殊需求

Project Ark 的星图部件拖拽与 WkecWulin 背包拖拽的主要差异：

| 维度 | WkecWulin 背包 | Project Ark 星图 |
|------|----------------|-----------------|
| 槽位类型 | 单一类型（物品格） | 多类型（武器/被动/主动/特殊） |
| 放置验证 | 类型+来源+容量 | 类型匹配+多格占用 |
| 视觉反馈 | 边框高亮 | 三色预览（Valid/Replace/Invalid） |
| 动画 | 无 | PrimeTween snap-in/fly-back |
| 多格支持 | 无 | SetMultiCellHighlight |
| Ghost | 大图标跟随 | 已有实现 |

Project Ark 的拖拽系统在动画和多格支持上已经超越了 WkecWulin，主要可借鉴的是**广播机制**和**独立验证器**两个架构设计。
