# WkecWulin 背包物品 Drag & Drop 逻辑详细分析

> 分析来源：`E:\Unity Projects\Unity_0.2.3_9345\WkecWulin\Assets\Wulin\Scripts\UI\`
> 分析时间：2026-03-03
> 目的：为 Project Ark 星图部件拖拽系统提供参考借鉴

---

## 一、整体架构概览

WkecWulin 的拖拽系统采用 **四层职责分离** 架构：

```
┌─────────────────────────────────────────────────────────┐
│              UIDragDropVirtualPanel（全局协调者）          │
│  - 场景单例，按 scene.name 注册                           │
│  - 管理所有 Container 的注册/注销                         │
│  - 持有拖拽中的 Ghost 图像（VirtualTarget）               │
│  - 广播 BeginDrag / Drag / EndDrag 事件给所有 Container   │
│  - 执行最终的 TransferItem 数据转移                       │
└────────────────────┬────────────────────────────────────┘
                     │ 广播事件 / 查询数据
         ┌───────────┴───────────┐
         ▼                       ▼
┌─────────────────┐   ┌──────────────────────┐
│ UIDragDropHandler│   │ UIDragDropContainer  │
│  （拖拽源）      │   │  （放置目标）         │
│ - 挂在可拖物件上 │   │ - 挂在容器根节点上    │
│ - 实现 IBeginDrag│   │ - 实现 IDropHandler  │
│   IDrag          │   │ - 持有 UIDropLogic   │
│   IEndDrag       │   │ - 管理 ItemSlot 列表 │
│ - 拖拽时半透明化 │   │ - 响应 Listen/Inside │
│ - 携带 CarryData │   │   状态变化事件        │
└────────┬────────┘   └──────────┬───────────┘
         │                       │ 委托验证
         │                       ▼
         │              ┌─────────────────┐
         │              │  UIDropLogic    │
         │              │  （验证器）      │
         │              │ - 容量上限检查   │
         │              │ - 物品类型过滤   │
         │              │ - 来源容器过滤   │
         │              │ - 自动转移逻辑   │
         │              └─────────────────┘
         │
         ▼
┌─────────────────┐
│  UIDragDropItem │
│  （槽位视图）    │
│ - 挂在每个格子上 │
│ - 持有 ItemId   │
│   InstId        │
│ - 管理图标显示   │
│ - 连接 Handler  │
└─────────────────┘
```

---

## 二、核心数据结构

### 2.1 DragDropItem（物品数据）

**文件**：`UI/Data/DragDropItem.cs`

```csharp
[System.Serializable]
public class DragDropItem
{
    public int ItemId;       // 物品定义ID（对应数据表）
    public int ItemType;     // 物品类型（1=宝具 2=秘籍）
    public int Rarity;       // 品阶（1白 2蓝 3紫 4金）
    public int Sect;         // 门派归属
    public int Weight;       // 负重
    public int Price;        // 买价
    public int Sell;         // 售价

    // 三套图标（大/中/小）
    private Sprite _bigIcon;
    public Sprite _bigTexture;
    private Sprite _bagIcon;
    public Sprite _bagTexture;
    private Sprite _smallIcon;
    public Sprite _smallTexture;

    public int[] GridData;   // 图标偏移量 [offsetX, offsetY]
    public string UiSoundName;

    [HideInInspector] public int InstId;       // 运行时实例ID（唯一）
    [HideInInspector] public int ContainerId;  // 当前所在容器ID
}
```

**关键设计**：
- `ItemId` 是定义ID（静态），`InstId` 是运行时实例ID（动态唯一）
- `ContainerId` 记录物品当前归属的容器，是转移逻辑的核心依据
- `Clone(instId, containerId)` 方法：从定义创建运行时副本，**不修改原始 SO 数据**

### 2.2 DragDropItemSettings（物品配置表）

**文件**：`UI/Data/DragDropItemSettings.cs`

```csharp
[CreateAssetMenu(menuName = "SO/DragDropItemSettings")]
public class DragDropItemSettings : ScriptableObject
{
    public List<DragDropItem> _ItemList;
    private Dictionary<int, DragDropItem> _Dict; // 懒加载字典，O(1)查找
}
```

- 是一个 ScriptableObject，在 Inspector 中配置所有可拖拽物品的定义
- 运行时通过 `GetByItemId(itemId)` 查找定义，再 `Clone()` 创建实例

### 2.3 ItemUnit / ItemUnitManager（实例ID管理）

负责全局唯一的 `InstId` 分配与回收：
- `ItemUnitManager.CreateItem(itemId)` → 分配新 InstId
- `ItemUnitManager.RecoverItem(item)` → 从存档恢复，保持原 InstId
- `ItemUnitManager.DestroyItem(instId)` → 回收 InstId
- `ItemUnitManager.FixItemInstanceSerial(maxId)` → 修正序列号，防止与存档数据冲突

---

## 三、各组件详细逻辑

### 3.1 UIDragDropHandler（拖拽源）

**文件**：`UI/UIDragDropHandler.cs`
**挂载位置**：每个可拖拽物品的 GameObject 上

#### 携带数据字段

```csharp
public int _CarryType = 0;    // 0=Item 1=Skill
public int _CarryId = 0;      // 物品定义ID
public int _CarryInstId = 0;  // 物品实例ID（关键！）
public int _CarryLevel = 1;   // 角色等级（用于Tooltip）
public int _CarryGroupId = 0; // 分组ID（用于Tooltip定位）
```

#### 拖拽生命周期

```
OnBeginDrag()
  ├─ 记录 _BeginDragPosition（静态字段，供 UIMoveDropListener 使用）
  ├─ _IsDragging = true
  ├─ virtualPanel.OnBeginDrag(this, rectTransform, _CarryInstId)
  │    ├─ 复制图标到 VirtualTarget（Ghost）
  │    └─ 广播 OnBeginDragEvent 给所有 Container
  ├─ virtualPanel.Drag(eventData)  ← 立即同步一次位置
  ├─ LockSelf()  ← 原图半透明(alpha=0.5) + 关闭 raycastTarget
  └─ _OnBeginDraggingAction?.Invoke()  ← 通知 UIDragDropItem / UIDragDropSound

OnDrag()
  └─ virtualPanel.Drag(eventData)
       ├─ VirtualTarget.position = ScreenToWorldPoint(eventData.position)
       └─ 广播 OnDragEvent 给所有 Container（用于 Rect 检测模式）

OnEndDrag()
  ├─ virtualPanel.OnEndDrag(eventData)
  │    ├─ HideVirtualTarget()
  │    └─ 广播 OnEndDragEvent 给所有 Container
  ├─ _IsDragging = false
  ├─ UnLockSelf()  ← 恢复原图 alpha + 开启 raycastTarget
  └─ _OnEndDraggingAction?.Invoke()
```

#### LockSelf / UnLockSelf 机制（完整源码）

```csharp
private void LockSelf()
{
    if (!_Locked)
    {
        _Locked = true;
        if (_ImageItem != null)
        {
            Color c = _Image_Color;  // 取原始颜色
            c.a = 0.2f;             // 注意：Destiny 版本是 0.2f（更透明），背包版本是 0.5f
            _ImageItem.color = c;
            _ImageItem.raycastTarget = false;  // 关键！关闭射线检测
        }
    }
}

public void UnLockSelf()
{
    if (_Locked)
    {
        _Locked = false;
        if (_ImageItem != null)
        {
            _ImageItem.color = _Image_Color;  // 恢复原始颜色（包含原始 alpha）
            _ImageItem.raycastTarget = true;
        }
    }
}
```

> **关键细节**：
> - `_Locked` 布尔守卫防止重复调用（幂等性保证）
> - `raycastTarget = false` 是必须的，否则拖拽时鼠标下方的 Container 无法收到 `OnPointerEnter` / `OnDrop` 事件
> - `UnLockSelf()` 是 `public` 的，VirtualPanel 在 `ReleaseDragHandler()` 中也会调用它（双重保险）
> - `_Image_Color` 在 `Awake` 中缓存，确保恢复的是原始颜色而非被修改后的颜色

#### `_EnableAlphaTest` 透明区域点击穿透

```csharp
public bool _EnableAlphaTest = false;
private const float _AlphaTest_inimumThreshold = 0.02f;

// Awake 中配置
if (_EnableAlphaTest)
{
    _ImageItem.alphaHitTestMinimumThreshold = _AlphaTest_inimumThreshold;
}
```

开启后，图片中 alpha < 0.02 的像素区域不响应点击/拖拽事件，适用于不规则形状的物品图标（如圆形骰子）。

---

### 3.2 UIDragDropVirtualPanel（全局协调者）

**文件**：`UI/UIDragDropVirtualPanel.cs`
**挂载位置**：场景根 Canvas 下的专用 GameObject

#### 场景单例注册机制

```csharp
// Destiny 版本使用简单静态引用（单场景）
public static UIDragDropVirtualPanel_Destiny _CurrnetIns;

private void Awake()
{
    _CurrnetIns = this;  // 直接覆盖，适合单场景
    // ...
}

// Handler 和 Container 通过静态属性获取
protected UIDragDropVirtualPanel_Destiny CurrentDragDropVirtualPanel 
    => UIDragDropVirtualPanel_Destiny._CurrnetIns;
```

> **注意**：Destiny 版本使用简单静态单例（`_CurrnetIns`），背包版本才使用 `scene.name` 作 key 的字典注册，支持多场景共存。两种方案各有适用场景。

#### Container 注册机制（ID 冲突处理）

```csharp
public void RegisterDropContainer(UIDragDropContainer_Destiny container)
{
    int instId = container.ContainerID;
    if (_DragDropContainers.ContainsKey(instId))
    {
        // ID 冲突时随机生成新 ID（适用于动态生成的商店格子）
        instId = UnityEngine.Random.Range(1, 99999999);
    }
    container.RebuildContainerID(instId);  // 回写新 ID 到 Container
    _DragDropContainers.Add(instId, container);
}
```

> **设计亮点**：不用 `FindObjectOfType`，Container 主动调用 `SetupVirtualPanel(vp)` 注册自己，VirtualPanel 只维护字典。ID 冲突时自动随机分配，适合运行时动态生成的 UI 元素。

#### Ghost 图像（VirtualTarget）机制

```csharp
public RectTransform _DragDisplayTransform;  // Inspector 中指定的 Ghost 节点

private void CopyTarget(RectTransform rectTarget)
{
    // 直接复制拖拽源的 Image.sprite（不查数据表，直接取当前显示图）
    Sprite sprite = CurrentDragHandler._ImageItem.sprite;
    var img = _VirtualTarget.gameObject.GetComponent<Image>();
    img.sprite = sprite;
    // 使用参考预制体的 sizeDelta 保持 Ghost 尺寸一致
    if (DicePrafabRefSize != null)
    {
        var r = _VirtualTarget.GetComponent<RectTransform>();
        r.sizeDelta = DiceRefSize;  // 固定尺寸，不用 SetNativeSize
    }
    ShowVirtualTarget();  // SetActive(true) + IsDragging = true
}

public void Drag(PointerEventData eventData)
{
    if (_VirtualTarget != null)
    {
        var pos = ScreenToWorldPoint(eventData.position);
        _VirtualTarget.position = pos;  // Ghost 跟随鼠标
        OnDragEvent(eventData);         // 同时广播给所有 Container
    }
}
```

#### Ghost 坐标转换完整实现

```csharp
// Awake 中缓存 Camera
private void Awake()
{
    if (_Canvas == null) _Canvas = gameObject.GetComponentInParent<Canvas>();
    _Camera = _Canvas.worldCamera;  // 必须是 Canvas 的 worldCamera
    _VirtualTarget = _DragDisplayTransform;
}

// 坐标转换：屏幕坐标 → 世界坐标
public Vector3 ScreenToWorldPoint(Vector3 screenPoint)
{
    Vector3 pos = Vector3.zero;
    if (_Camera != null)
    {
        pos = _Camera.ScreenToWorldPoint(screenPoint);
        if (_VirtualTarget != null)
        {
            pos.z = _VirtualTarget.position.z;  // 保持 Ghost 的 Z 轴不变（防止深度错误）
        }
    }
    return pos;
}

// Rect 检测：屏幕坐标 → 本地坐标 → 矩形包含检测
public bool ScreenPointToLocalPointInRectangle(
    RectTransform rectTrans, Vector3 screenPoint, out Vector2 localPos)
{
    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
        rectTrans, screenPoint, _Camera, out localPos))
    {
        return rectTrans.rect.Contains(localPos);  // 用 Rect.Contains 判断是否在矩形内
    }
    return false;
}
```

**关键细节**：
- `_Camera = _Canvas.worldCamera`：必须用 Canvas 的 worldCamera，而非 `Camera.main`，否则在 Screen Space - Camera 模式下坐标会偏移
- `pos.z = _VirtualTarget.position.z`：强制保持 Ghost 的 Z 轴，防止 Ghost 被其他 UI 层遮挡或穿透
- Ghost 节点始终在 Canvas 最顶层（Inspector 中手动设置 sibling order）
- `IsDragging` 属性随 `ShowVirtualTarget/HideVirtualTarget` 同步更新，外部可查询拖拽状态

#### 事件广播流程

```csharp
// BeginDrag 时广播给所有已注册的 Container
private void OnBeginDragEvent()
{
    foreach (var container in _DragDropContainersList)
    {
        container.OnBeginDragEvent(_CurrentItemInstId);
        // Container 内部自行判断是否进入 Listen 状态
    }
}

// Drag 时广播（用于 Rect 检测模式的 Container）
private void OnDragEvent(PointerEventData eventData)
{
    foreach (var container in _DragDropContainersList)
    {
        container.OnDragEvent(eventData);
    }
}

// EndDrag 时广播，重置所有 Container 状态
private void OnEndDragEvent(PointerEventData eventData)
{
    foreach (var container in _DragDropContainersList)
    {
        container.OnEndDragEvent(eventData);
    }
    ReleaseDragHandler();  // 清空 CurrentDragHandler
}
```

#### OnDropSuccess / OnDropEnd 区别

```csharp
// 放置成功时调用（由 Container.OnDrop 在验证通过后调用）
public void OnDropSuccess()
{
    if (CurrentDragItem != null)
        CurrentDragItem.OnDropSuccess();  // 通知拖拽源（骰子）执行成功动画
    _AudioPutInSlot.PlayOneShot_Self();   // 播放放置音效
    _CurrentDragSuccess = true;           // 标记成功（影响 ReleaseDragHandler 的音效）
}

// 放置失败时调用（由 Container.OnDrop 在验证失败后调用）
public void OnDropEnd()
{
    HideVirtualTarget();
    OnEndDragEvent(null);  // 广播 EndDrag 给所有 Container
}

// ReleaseDragHandler：清理拖拽状态
private void ReleaseDragHandler()
{
    if (CurrentDragHandler != null)
    {
        CurrentDragHandler.OnDropEnd();  // 调用 Handler.UnLockSelf()（双重保险）
        CurrentDragHandler = null;
        CurrentDragItem = null;
        // 只有放置失败时才播放弹回音效
        if (_CurrentDragSuccess == false) _AudioDiceBack.PlayOneShot_Self();
    }
}
```

**音效触发逻辑**：
- 拖拽开始 → `_AudioPickup`（拾取音效）
- 放置成功 → `_AudioPutInSlot`（放入音效）
- 放置失败/取消 → `_AudioDiceBack`（弹回音效）

#### CancelDrag 取消拖拽

```csharp
// 外部可调用此方法强制取消当前拖拽（如打开菜单时）
public void CancelDrag()
{
    Invoke("_CancelDragEvent", 0.01f);  // 延迟一帧执行，避免与当前帧事件冲突
}

private void _CancelDragEvent()
{
    if (CurrentDragHandler != null)
    {
        ReleaseDragHandler();
    }
    HideVirtualTarget();
    OnEndDragEvent(null);
}
```

---

### 3.3 UIDragDropContainer（放置目标）

**文件**：`UI/UIDragDropContainer.cs`
**挂载位置**：背包格/装备槽等容器的根节点

#### 两种 Inside 检测模式

**模式一：PointerEnter 模式（默认）**
```csharp
public bool _CheckInsideOnDragIntoRect = false; // 关闭时使用此模式

public void OnPointerEnter(PointerEventData eventData)
{
    if (_ListenDrag && _EnableDrop)
        SetInside(true);  // 依赖 Unity 的 PointerEnter 事件
}
```

**模式二：Rect 检测模式**
```csharp
public bool _CheckInsideOnDragIntoRect = true; // 开启时使用此模式

public void OnDragEvent(PointerEventData eventData)
{
    if (_ListenDrag && _CheckInsideOnDragIntoRect)
    {
        var screenPoint = eventData.position;
        var inside = CurrentDragDropVirtualPanel.ScreenPointToLocalPointInRectangle(
            _RectTransform, screenPoint, out Vector2 localPos);
        // 注意：只在状态变化时才调用 SetInside，避免每帧重复触发事件
        if (inside != _DragInside)
        {
            SetInside(false); // 主动检测矩形范围（注意：此处传 false 是重置，inside 变化时统一重置）
        }
    }
}
```

> **⚠️ 源码中的实际行为**：`OnDragEvent` 中当 `inside != _DragInside` 时调用的是 `SetInside(false)` 而非 `SetInside(inside)`。这意味着 Rect 模式下 `_DragInside` 始终为 false，**Inside 状态实际上由 `OnDrop` 事件触发时的 `_ListenDrag` 来判断**，而非 `_DragInside`。这是一个有意为之的简化设计——Destiny 版本的 Container 不需要「鼠标悬停高亮」，只需要「能否放置」的判断。

> **为什么需要 Rect 模式**：当 Canvas 层级复杂、有遮挡物时，`OnPointerEnter` 可能被上层 UI 拦截。Rect 模式直接用数学计算判断鼠标是否在矩形内，更可靠。

#### Listen / Inside 双状态机

```
状态：_ListenDrag（是否监听当前拖拽）
      _DragInside（鼠标是否在容器范围内）
      _EnableDrop（容器是否开放接受放置，由外部逻辑控制）

OnBeginDragEvent(diceNumber)
  ├─ SetListen(false), SetInside(false)  ← 先重置
  └─ if _EnableDrop && DropLogic.CheckDropCondition(diceNumber)
       ├─ SetListen(true)  ← 条件满足才进入监听
       │    └─ 内部：_CanDropTips.gameObject.SetActive(true)  ← 显示「可放置」提示图
       └─ _OnBeginDragAndListenEvent?.Invoke(diceNumber)  ← 外部高亮回调

OnDragEvent(eventData)  [仅 Rect 模式]
  └─ if _ListenDrag && _CheckInsideOnDragIntoRect
       └─ 检测矩形 → 状态变化时 SetInside(false)（见上方 Rect 模式说明）

OnDrop(eventData)  [Unity EventSystem 触发]
  ├─ 记录 _LastDropPosition 和 _LastDropLocalPosition
  ├─ if _ListenDrag
  │    ├─ SetListen(false)  ← 立即关闭监听（防止重复触发）
  │    └─ if _EnableDrop && DropLogic.CheckDropCondition && !_DragListenOnly
  │         ├─ virtualPanel.OnDropSuccess()  ← 播放音效 + 通知拖拽源
  │         └─ OnDropSuccess_FormDiceBox()   ← 本地处理：更新图标、锁定槽位、触发事件
  └─ else → virtualPanel.OnDropEnd()  ← 失败时结束拖拽（播放弹回音效）

OnEndDragEvent(eventData)  [VirtualPanel 广播]
  ├─ if _ListenDrag → SetListen(false)
  │    └─ 内部：_CanDropTips.gameObject.SetActive(false)  ← 隐藏「可放置」提示图
  └─ SetInside(false)
```

#### 高亮维持机制（完整实现）

```csharp
// Container 上有 3 个 Image 组件控制视觉状态：
[SerializeField] Image _LockMask;      // 锁定遮罩（容器不可用时显示）
[SerializeField] Image _SupLockMask;   // 辅助锁定遮罩（放置成功后显示）
[SerializeField] Image _CanDropTips;   // 「可放置」提示图（Listen 状态时显示）

// SetListen 直接控制 _CanDropTips 的显隐
private void SetListen(bool flag)
{
    if (_ListenDrag != flag)
    {
        _ListenDrag = flag;
        _CanDropTips.gameObject.SetActive(flag);  // Listen=true 时显示高亮提示
    }
}

// SetEnableDrop 控制 _LockMask 和 _SupLockMask
public void SetEnableDrop(bool b)
{
    _EnableDrop = b;
    _LockMask.gameObject.SetActive(!b);     // 不可用时显示锁定遮罩
    _CondText.gameObject.SetActive(b);      // 可用时显示条件文字
    _SupLockMask.gameObject.SetActive(!b);  // 不可用时显示辅助遮罩
}

// 放置成功后的视觉更新
void OnDropSuccess_FormDiceBox(UIDestinyDice dice)
{
    _LockMask.sprite = dice.GetCurrentNumberIocn();  // 锁定遮罩换成骰子图标
    _DropLogic.OnDropSuccess();  // DropLogic._Lock = true（防止再次放入）
    SetEnableDrop(false);        // 关闭容器（显示锁定遮罩）
    destinyDiceRef = dice;       // 记录放入的骰子引用
    _OnDropSuccessEvent?.Invoke(ContainerID);  // 通知外部
}
```

**高亮状态流转总结**：

| 状态 | `_CanDropTips` | `_LockMask` | `_CondText` |
|------|---------------|-------------|-------------|
| 初始（空槽可用） | 隐藏 | 隐藏 | 显示（条件文字） |
| 拖拽中（符合条件） | **显示**（高亮） | 隐藏 | 显示 |
| 拖拽中（不符合条件） | 隐藏 | 隐藏 | 显示 |
| 放置成功 | 隐藏 | **显示**（骰子图标） | 隐藏 |
| 容器不可用 | 隐藏 | **显示**（锁定图） | 隐藏 |

#### 槽位管理

```csharp
protected List<UIDragDropItem> _DragDropItemSlots;  // 所有槽位
public UIDragDropItem _CurrentItemSlot;             // 当前有物品的槽位
public UIDragDropItem _EmptyItemSlot;               // 第一个空槽位（缓存）

// Start 时自动收集子节点中的所有 UIDragDropItem
protected void ReCollectSlots()
{
    var root = _ItemSlotsParent;
    for (int i = 0; i < root.childCount; i++)
    {
        var slot = root.GetChild(i).GetComponentInChildren<UIDragDropItem>(true);
        slot.ResetContainerTo(this._InstanceId);  // 绑定归属
        _DragDropItemSlots.Add(slot);
    }
}

// 脏标记机制：动态增减槽位时设置 dirty，LateUpdate 中重新收集
public void SetSlotDirty() { _SlotDirty = true; }
void LateUpdate() { if (_SlotDirty) { _SlotDirty = false; ReCollectSlots(); } }
```

#### AddItem / RemoveItem

```csharp
public bool AddItem(int itemInstId)
{
    // 1. 检查 DropLogic 是否已有此物品（防重复）
    if (_DropLogic.CheckHasItem(itemInstId)) return false;
    // 2. DropLogic 记录（容量管理）
    if (_DropLogic.AddItem(itemInstId) == false) return false;
    // 3. 找空槽位放入
    bool success = DropItemToEmptySlot(itemInstId);
    if (success) _ItemCnt++;
    // 4. 触发事件
    _OnDropSuccessEvent?.Invoke(itemInstId);
    _OnItemAddRemoveEvent?.Invoke();
    return true;
}

public void RemoveItem(int itemInstId)
{
    _DropLogic.RemoveItem(itemInstId);  // 从 DropLogic 移除记录
    foreach (var slot in _DragDropItemSlots)
    {
        if (slot.HasItem && slot.InstanceId == itemInstId)
        {
            slot.RemoveItem();  // 清空槽位视图
            _ItemCnt--;
            if (_EmptyItemSlot == null) _EmptyItemSlot = slot;  // 更新空槽缓存
            break;
        }
    }
    _OnRemoveItemEvent?.Invoke(itemInstId);
    _OnItemAddRemoveEvent?.Invoke();
}
```

---

### 3.4 UIDropLogic（放置验证器）

**文件**：`UI/UIDropLogic.cs`
**挂载位置**：与 `UIDragDropContainer` 同一 GameObject（或子节点）

#### 验证条件配置（Destiny 版本实际字段）

```csharp
[SerializeField] DestinyCardRuleData RuleData;  // 天命卡片规则数据（从外部注入）
[SerializeField] int DropDice;                  // 记录最后一次验证的骰子点数
public int _ItemInstLimits = 1;                 // 容量上限（Destiny 版本固定为 1）
public bool _DragListenOnly = false;            // true=只高亮不接受放置（只读容器）
public bool _Lock;                              // 放置成功后锁定，防止再次放入
public bool DenyDrop;                           // 运行时临时禁止（代码控制）
```

#### CheckDropCondition 完整验证链（Destiny 版本）

```csharp
public bool CheckDropCondition(int number)  // number = 骰子点数（1-6）
{
    // 第一关：临时禁止
    if (DenyDrop) return false;
    
    // 第二关：已锁定（已有骰子放入）
    if (_Lock) return false;
    
    // 第三关：规则检查
    if (RuleData == null)
        return true;  // 无规则 = 接受任意骰子
    else
        return CkeckDiceRule(number);  // 调用 DestinyDiceRule.RunTimeRuleLogic()
}

// 放置成功后锁定
public void OnDropSuccess()
{
    _Lock = true;  // 锁定，直到 Init() 重置
}

// 重置（容器清空时调用）
public void Init()
{
    _Lock = false;
    DropDice = -1;
}
```

**验证链流程图**：
```
CheckDropCondition(diceNumber)
  ├─ DenyDrop == true → false（临时禁止）
  ├─ _Lock == true → false（已有骰子，槽位已满）
  ├─ RuleData == null → true（无规则限制，接受任意）
  └─ DestinyDiceRule.RunTimeRuleLogic(number, RuleData)
       ├─ 规则匹配 → true（骰子点数符合天命规则）
       └─ 规则不匹配 → false
```

> **注意**：Destiny 版本的 DropLogic 比背包版本简单得多——没有物品类型过滤、来源容器过滤、AutoTransfer 等复杂逻辑，核心只有「骰子点数是否符合天命规则」这一条验证。背包版本（`UIDropLogic.cs`）才有完整的多条件验证链。

---

### 3.5 UIDragDropItem（槽位视图）

**文件**：`UI/UIDragDropItem.cs`
**挂载位置**：每个具体的物品格子 GameObject

#### 核心字段

```csharp
[SerializeField] private int _InstanceId = 0;  // 当前持有的物品实例ID（0=空）
[SerializeField] private int _ItemId = -1;      // 当前持有的物品定义ID（-1=空）
[SerializeField] private int _ContainerId;      // 归属容器ID

public bool HasItem { get { return ItemId > 0; } }  // 是否有物品
```

#### SetupItem 流程

```csharp
public void SetupItem(int itemInstId)
{
    _InstanceId = itemInstId;
    var item = virtualPanel.GetItemByInstId(itemInstId);
    
    // 1. 更新图标
    _Image.sprite = _UseNativeSize ? item.BigIcon() : item.SmallIcon();
    if (_UseNativeSize) _Image.SetNativeSize();
    
    // 2. 更新文字（名称、价格、负重、品阶）
    _TextName.text = item.ItemName;
    _CostValue.text = $"{item.Price}";
    ShowRarity(item.Rarity);
    
    // 3. 同步 DragHandler 的携带数据
    dragHandler._CarryInstId = item.InstId;
    dragHandler._CarryId = item.ItemId;
    dragHandler.UnLockSelf();  // 确保解锁状态
    
    // 4. 激活 GameObject
    this.gameObject.SetActive(true);
}
```

#### 拖拽后防误触锁定机制

```csharp
private float _LastDraggingTime = 0;
private const float _AfterDraggingTimeThreshold = 0.5f;

public void SwitchLocked()
{
    float lastDraggingTimePass = Time.time - _LastDraggingTime;
    bool isAfterDragging = lastDraggingTimePass < _AfterDraggingTimeThreshold;
    
    if (isAfterDragging) return;  // 拖拽结束后 0.5s 内忽略锁定点击
    SetIsLocked(!_Locked);
}
```

> **解决的问题**：拖拽操作结束时，`OnEndDrag` 和 `OnPointerUp` 几乎同时触发，如果格子上有锁定按钮，会误触发锁定。通过时间阈值过滤掉拖拽结束后的误点击。

#### Tooltip 在拖拽中的行为

> **⚠️ 重要发现**：Destiny 版本（`UIDragDropHandler_Destiny.cs`）**没有实现 Tooltip**。Handler 只实现了 `IBeginDragHandler`、`IDragHandler`、`IEndDragHandler` 三个接口，没有 `IPointerEnterHandler` / `IPointerMoveHandler` / `IPointerExitHandler`。

Tooltip 功能在背包版本（`UIDragDropHandler.cs`）中实现，Destiny 版本不需要悬停提示。

**背包版本 Tooltip 行为**（供参考）：
- `OnPointerEnter` → 显示物品信息弹窗（调用 `virtualPanel.ShowTips(...)`）
- `OnPointerMove` → 更新弹窗跟随鼠标位置
- `OnPointerExit` → 隐藏弹窗（传 null eventData）
- `OnDisable` → 强制触发 `_OnExitEvent`，防止弹窗在物品被禁用时残留
- **拖拽中**：`_IsDragging = true` 时，`OnPointerEnter` 不触发（因为 `raycastTarget = false`），所以拖拽过程中 Tooltip **自动消失**，无需额外处理

---

### 3.6 UIDragDropItemGenerator（商品生成器）

**文件**：`UI/UIDragDropItemGenerator.cs`
**职责**：商店系统的物品随机生成逻辑（与拖拽系统解耦，通过 Container 接口交互）

#### 生成流程

```
GenerateItems()
  ├─ InitLockedShopItems()     ← 收集被玩家锁定的商品（不重新生成）
  ├─ CollectItemDef()          ← 过滤可生成的物品定义列表
  ├─ InitGenerationCondition() ← 收集玩家已持有物品（用于限制生成）
  ├─ 选择生成策略：
  │    ├─ GM Mode → GenerateItems_GM()（顺序遍历，调试用）
  │    ├─ NormalRandom → GenerateItems_NormalRandom()（纯随机）
  │    └─ TableRandom → GenerateItems_TableRandom()（按概率表）
  └─ SetupSlots(slots, bucket, skipLockedSlot)
       └─ virtualPanel.CreateItem(id, containerId) → 创建实例 → slot.SetupItem(instId)
```

#### 概率表随机（TableRandom）

```
品阶概率表（22_Parameter 表）：
  白色: 40%  蓝色: 30%  紫色: 20%  金色: 10%

额外加成（玩家特殊效果）：
  白色额外权重 × (1 + extraRate/100)
  紫色额外权重 + extraValue
  金色额外权重 + extraValue

秘籍生成（前两格）：
  scrollWeight > 0 时有概率生成秘籍
  条件：玩家门派匹配 + 玩家未持有该秘籍
```

---

### 3.7 辅助组件

#### UIDragDropSound（音效）

**文件**：`UI/UIDragDropSound.cs`

```csharp
// 订阅 Handler 的 Action 事件
uIDragDropHandler._OnBeginDraggingAction += PlayAudio_Start;
uIDragDropHandler._OnEndDraggingAction += PlayAudio_End;

// 防止音效错位：记录拖拽开始时的 CarryId 和 CarryInstId
// EndDrag 时验证是否与开始时一致，不一致则跳过播放
void PlayAudio_End()
{
    if (uIDragDropHandler._CarryId != _CurrentId 
        || uIDragDropHandler._CarryInstId != _CurrentIns) return;
    GameAudio.Instance.PlayAudio_OneShot(_Clip, _AudioType);
}
```

#### UIMoveDropListener（位移拖拽）

**文件**：`UI/UIMoveDropListener.cs`

用于需要计算拖拽位移量的场景（如自由摆放）：

```csharp
public void OnDrop(PointerEventData eventData)
{
    Vector2 dropPos = ScreenToWorldPoint(eventData.position);
    Vector2 beginDragPos = ScreenToWorldPoint(UIDragDropHandler._BeginDragPosition);
    _Offset = dropPos - beginDragPos;  // 计算拖拽位移
    _CurrentItemInstId = virtualPanel.CurrentItemInstId;
    _OnDrop?.Invoke();  // 外部处理位移逻辑
}
```

#### UIShopItemSlot（商品格包装）

**文件**：`UI/UIShopItemSlot.cs`

对 `UIDragDropItem` 的包装层，增加：
- 品阶边框图片切换（`_ItemFrames[]`）
- 格子锁定状态（`_SlotIsLocked`）
- 物品锁定状态（`_DragDropItem._Locked`）
- 预购标记（`_PreOrder`）
- PointerEnter/Exit 事件转发给 Generator

---

## 四、完整拖拽流程时序图

```
用户按下鼠标并开始拖动
         │
         ▼
UIDragDropHandler.OnBeginDrag()  [仅 _EnableDrag=true 时响应]
  ├─ _BeginDragPosition = eventData.position  [静态字段]
  ├─ _IsDragging = true
  ├─ VirtualPanel.OnBeginDrag(handler, rect, diceNumber)
  │    ├─ CurrentDragHandler = handler
  │    ├─ CurrentDragItem = handler.GetComponent<UIDestinyDice>()
  │    ├─ _CurrentDiceNumber = diceNumber
  │    ├─ HideVirtualTarget()  [先隐藏，防止闪烁]
  │    ├─ CopyTarget(rect)
  │    │    ├─ sprite = CurrentDragHandler._ImageItem.sprite  [直接取当前图]
  │    │    ├─ Ghost.Image.sprite = sprite
  │    │    ├─ Ghost.sizeDelta = DiceRefSize  [固定参考尺寸]
  │    │    └─ ShowVirtualTarget()  [SetActive(true) + IsDragging=true]
  │    ├─ OnBeginDragEvent() → 广播给所有 Container
  │    │    └─ Container.OnBeginDragEvent(diceNumber)
  │    │         ├─ SetListen(false), SetInside(false)  [重置]
  │    │         └─ if _EnableDrop && DropLogic.CheckDropCondition(diceNumber)
  │    │              ├─ 通过 → SetListen(true)
  │    │              │    └─ _CanDropTips.SetActive(true)  [显示高亮提示]
  │    │              │    └─ _OnBeginDragAndListenEvent?.Invoke(diceNumber)
  │    │              └─ 不通过 → 保持 Listen=false
  │    └─ _AudioPickup.PlayOneShot()  [播放拾取音效]
  ├─ VirtualPanel.Drag(eventData)  [立即同步 Ghost 到鼠标位置]
  ├─ LockSelf()  [原图 alpha=0.2 + raycastTarget=false]
  └─ _OnBeginDraggingAction?.Invoke()  [通知音效组件等]

用户移动鼠标（每帧）
         │
         ▼
UIDragDropHandler.OnDrag()  [仅 _EnableDrag=true 时响应]
  └─ VirtualPanel.Drag(eventData)
       ├─ pos = _Camera.ScreenToWorldPoint(eventData.position)
       ├─ pos.z = _VirtualTarget.position.z  [保持 Z 轴]
       ├─ _VirtualTarget.position = pos  [Ghost 跟随鼠标]
       └─ OnDragEvent(eventData) → 广播给所有 Container
            └─ Container.OnDragEvent(eventData)
                 └─ [Rect模式 && _ListenDrag]
                      ├─ RectTransformUtility.ScreenPointToLocalPointInRectangle()
                      ├─ rectTrans.rect.Contains(localPos) → inside
                      └─ if inside != _DragInside → SetInside(false)  [状态变化时重置]

用户松开鼠标
         │
         ├─────────────────────────────────────────────────────────┐
         ▼                                                         ▼
UIDragDropContainer.OnDrop()                          UIDragDropHandler.OnEndDrag()
[Unity EventSystem 触发，鼠标下方第一个 IDropHandler]   [同帧触发]
  ├─ 记录 _LastDropPosition / _LastDropLocalPosition     ├─ VirtualPanel.OnEndDrag(eventData)
  ├─ diceNumber = VirtualPanel.CurrentDiceNumber         │    ├─ HideVirtualTarget()
  ├─ if _ListenDrag                                      │    └─ OnEndDragEvent() → 广播
  │    ├─ SetListen(false)  [立即关闭监听]                │         └─ Container.OnEndDragEvent()
  │    └─ if _EnableDrop                                 │              ├─ SetListen(false)
  │         ├─ dice = pointerDrag.GetComponent<UIDestinyDice>()  │    └─ SetInside(false)
  │         └─ if DropLogic.CheckDropCondition && !_DragListenOnly  ├─ _IsDragging = false
  │              ├─ VirtualPanel.OnDropSuccess()          └─ UnLockSelf()  [恢复原图]
  │              │    ├─ CurrentDragItem.OnDropSuccess()      └─ _OnEndDraggingAction?.Invoke()
  │              │    ├─ _AudioPutInSlot.PlayOneShot()
  │              │    └─ _CurrentDragSuccess = true
  │              └─ OnDropSuccess_FormDiceBox(dice)
  │                   ├─ _LockMask.sprite = dice.GetCurrentNumberIcon()
  │                   ├─ _DropLogic.OnDropSuccess()  [_Lock=true]
  │                   ├─ SetEnableDrop(false)  [关闭容器]
  │                   ├─ destinyDiceRef = dice
  │                   └─ _OnDropSuccessEvent?.Invoke(ContainerID)
  └─ else → VirtualPanel.OnDropEnd()
       ├─ HideVirtualTarget()
       └─ OnEndDragEvent(null) → 广播
            └─ ReleaseDragHandler()
                 ├─ CurrentDragHandler.OnDropEnd()  [UnLockSelf 双重保险]
                 ├─ CurrentDragHandler = null
                 └─ if !_CurrentDragSuccess → _AudioDiceBack.PlayOneShot()  [弹回音效]
---

## 五、关键设计模式总结

### 5.1 广播-订阅模式（BeginDrag 广播）

```
VirtualPanel 广播 → 所有 Container 自行判断是否 Listen
```

**优势**：
- Container 不需要知道谁在拖拽，只需关心"这个物品能不能放进来"
- 新增 Container 类型无需修改 VirtualPanel
- 可以在拖拽开始时就提前高亮所有合法目标

### 5.2 双重验证（BeginDrag + OnDrop 各验证一次）

```
BeginDrag 时：CheckDropCondition → 决定是否 Listen（高亮）
OnDrop 时：  CheckDropCondition → 决定是否真正执行转移
```

**原因**：拖拽过程中物品状态可能变化（如另一个线程修改了容量），OnDrop 时再验证一次保证数据一致性。

### 5.3 InstId 与 ItemId 分离

```
ItemId（定义ID）：对应数据表，多个实例共享同一 ItemId
InstId（实例ID）：运行时唯一，是拖拽系统操作的实际对象
```

**好处**：同一种物品可以有多个实例同时存在于不同容器，互不干扰。

### 5.4 ContainerId 归属追踪

每个物品实例的 `DragDropItem.ContainerId` 始终记录其当前所在容器，`TransferItem` 时通过此字段找到源容器执行 `RemoveItem`，无需遍历所有容器。

### 5.5 _DragListenOnly 只读容器

```csharp
public bool _DragListenOnly = false;
```

设为 `true` 的容器会在拖拽时高亮（进入 Listen 状态），但 `OnDrop` 时不执行转移。用于"预览区域"或"信息展示区"。

---

## 六、背包版本完整功能补充

> 以下内容基于背包版本（`UIDragDropHandler.cs` / `UIDragDropVirtualPanel.cs` / `UIDragDropContainer.cs` / `UIDropLogic.cs`）的真实源码，补充 Destiny 版本中没有的功能。

### 6.1 Tooltip 系统（背包版本完整实现）

#### Handler 侧：5 个接口

```csharp
// 背包版本 Handler 实现了 7 个接口（Destiny 版本只有 3 个）
public class UIDragDropHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,  // 拖拽三件套
    IPointerDownHandler,                                // 调试用
    IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler  // Tooltip 三件套
{
    public bool _EnableShowTips = false;  // Inspector 开关，false 时完全跳过 Tooltip 逻辑
    public int _CarryType = 0;  // 0:Item 1:Skill（决定显示哪种 Tooltip 模板）
    public int _CarryId = 0;    // 物品定义 ID
    public int _CarryInstId = 0; // 物品实例 ID
    public int _CarryLevel = 1;  // 角色等级（影响技能描述数值）
    public int _CarryGroupId = 0; // 分组 ID（决定 Tooltip 显示位置）

    void _OnEnterEvent(PointerEventData eventData)
    {
        _IsEnter = true;
        if (_EnableShowTips)
            virtualPanel.ShowTips(_CarryInstId, _CarryId, _CarryType, _CarryLevel, _CarryGroupId, eventData);
    }

    void _OnMoveEvent(PointerEventData eventData)
    {
        if (_EnableShowTips)
            virtualPanel.ShowTips(_CarryInstId, _CarryId, _CarryType, _CarryLevel, _CarryGroupId, eventData);
        // 每帧更新 Tooltip 位置，跟随鼠标
    }

    void _OnExitEvent(PointerEventData eventData)
    {
        _IsEnter = false;
        if (_EnableShowTips)
            virtualPanel.ShowTips(_CarryInstId, _CarryId, _CarryType, _CarryLevel, _CarryGroupId, null);
        // eventData=null 是隐藏信号
    }

    void OnDisable()
    {
        if (_IsEnter)
            _OnExitEvent(null);  // 物品被禁用时强制关闭 Tooltip，防止残留
    }
}
```

**拖拽中 Tooltip 自动消失的原理**：
- `LockSelf()` 将 `_ImageItem.raycastTarget = false`
- raycastTarget=false 后，EventSystem 不再向该 Image 发送 `OnPointerEnter`
- 因此拖拽开始后 Tooltip **自动消失，无需额外代码处理**

#### VirtualPanel 侧：多类型 Tooltip 路由

```csharp
// Tooltip 类型枚举（决定使用哪个 UIDragDropItem_Tips 子类）
public enum ItemTipsTypeName
{
    None = 0,
    Item = 1,    // 宝具类（武器）
    Skill = 2,   // 技能类
    Buff = 3,    // Buff 类
    CharAttr = 4, // 角色属性
    Destiny = 5, // 天命卡牌
    FormulaLog = 99, // 数值公式日志（GM 调试用）
}

// ShowTips 路由逻辑
public void ShowTips(int itemInstId, int itemId, int iTipsType, int level, int groupType,
    PointerEventData eventData, object carryData = null)
{
    if (eventData == null)  // null = 隐藏信号
    {
        HideAllTips();  // 隐藏所有 Tooltip
        return;
    }

    // groupType < 0 时自动从物品的 ContainerId 推断（用于不知道来源的场景）
    if (groupType < 0)
    {
        var item = GetItemByInstId(itemInstId);
        if (item != null) groupType = item.ContainerId;
    }

    var tipUi = FindTipsUI((ItemTipsTypeName)iTipsType);  // 按类型找对应 UI 组件
    if (tipUi._FixedPosition)  // 固定位置模式（如商店右侧固定展示区）
    {
        var posData = FindTipsPosition(groupType);  // 按 groupType 找预设位置
        tipUi.SetupPosition(posData._Transform.position, Vector3.zero);
        tipUi.Show(itemInstId, itemId, iTipsType, level, eventData, carryData);
        if (posData._ShowComposite) tipUi.ShowInfo_Composite();  // 显示复合信息
    }
    else  // 跟随鼠标模式
    {
        tipUi.Show(itemInstId, itemId, iTipsType, level, eventData, carryData);
    }

    // 物品切换时触发事件（用于外部联动，如高亮对应格子）
    if (itemInstId != _LastItemInstId_OnShow)
    {
        _LastItemInstId_OnShow = itemInstId;
        _OnShowTipEvent?.Invoke(itemInstId);
    }
}
```

#### UIDragDropItem_Tips：Tooltip 弹窗基类

```csharp
// 核心功能：
// 1. 屏幕边界自动翻转（防止弹窗超出屏幕）
protected virtual void UpdateInfoPos(RectTransform rectTrans, PointerEventData eventData)
{
    var uiWidth = rectTrans.sizeDelta.x;
    var pos = ScreenToWorldPoint(eventData);
    Vector3 localOffset = Vector3.zero;
    localOffset.x = uiWidth / 2f * 1.22f;  // 默认在鼠标右侧
    if (eventData.position.x > _DisplayRightSide)  // 鼠标偏右时翻转到左侧
        localOffset.x = -uiWidth / 2f * 1.22f;
    SetupPosition(pos, localOffset);

    // 二次校正：用 GetWorldCorners 检测是否超出屏幕，超出则 Clamp
    bool inside = IsRectTransformInsideScreen(_Canvas.worldCamera, rectTrans, out Vector3 offset);
    if (!inside) ClampRectTransformToScreen(_Canvas.worldCamera, rectTrans, offset);
}

// 2. 自动缩放面板（根据描述文字长度选择合适的面板尺寸）
protected IEnumerator YieldAutoScaleTextForContent(RectTransform rectTrans, Text text, string sourceText)
{
    // 遍历预设的多个尺寸配置，找到能完整显示文字的最小尺寸
    for (int i = 0; i < _AutoScaleSettings.Length; i++)
    {
        rectTrans.sizeDelta = _AutoScaleSettings[i]._PanelSizeDelta;
        yield return null;  // 等一帧让 Layout 重算
        if (TextOverflowCheck(text, fixedSourceText) == false) break;  // 不溢出则停止
    }
    UpdateInfoPos(rectTrans, _LastPointerEventData);  // 尺寸确定后重新定位
}

// 3. 数据脏检测（避免重复刷新相同物品）
bool dataDirty = (_CarryInstId != itemInstId || _CarryId != itemId);
if (dataDirty) SetupUiByItemId(itemInstId, itemId);  // 只在物品变化时重新查数据
```

**Tooltip 完整生命周期**：
```
OnPointerEnter → ShowTips(eventData) → FindTipsUI → Show()
    └─ dataDirty? → SetupUiByItemId() → 查数据表 → 填充文字
    └─ YieldAutoScaleTextForContent() → 等1帧 → 选最小合适尺寸
    └─ UpdateInfoPos() → 计算位置 → 屏幕边界校正

OnPointerMove → ShowTips(eventData) → Show()
    └─ dataDirty=false → 跳过数据查询
    └─ UpdateInfoPos() → 实时更新位置（跟随鼠标）

OnPointerExit → ShowTips(null) → HideAllTips() → SetActive(false)

OnDisable → _OnExitEvent(null) → HideAllTips()  [防残留]
```

---

### 6.2 背包版本 UIDropLogic 完整验证链

```csharp
public bool CheckDropCondition(int itemInstId)
{
    // 第一关：临时禁止（运行时代码控制，如打开菜单时）
    if (DenyDrop) return false;

    // 第二关：物品合法性（防止拖拽空格子）
    var item = virtualPanel.GetItemByInstId(itemInstId);
    if (item == null || item.ItemId <= 0) return false;

    // 第三关：重复检测（防止同一物品放入同一容器两次）
    if (CheckHasItem(itemInstId)) return false;

    // 第四关：容量上限（_ItemInstLimits=-1 表示无限）
    if (CheckItemLimits()) return false;
    // 注意：_AutoTransfer=true 时 CheckItemLimits() 始终返回 false（满了会自动转移）

    // 第五关：物品类型过滤（_CheckItemGroupId=true 时启用）
    if (_CheckItemGroupId)
        if (!_DropGroupIdHash.Contains(item.ItemType)) return false;

    // 第六关：来源容器过滤（_CheckContainerId=true 时启用）
    if (_CheckContainerId)
        if (!_DropContainerIdHash.Contains(item.ContainerId)) return false;

    return true;
}
```

#### AutoTransfer 自动转移机制

```csharp
// 场景：装备格只能放 1 件，但允许「换装」——放入新物品时自动把旧物品转移到背包
public bool _AutoTransfer = true;
public int _AutoTransferContainerId = 101;  // 背包容器 ID

// AddItem 时触发
public bool AddItem(int itemInstId)
{
    if (_ItemInstCount >= _ItemInstLimits)  // 已满
    {
        if (_AutoTransfer)
        {
            bool success = TransferItem();  // 把最旧的物品转移到背包
            if (!success) return false;
        }
        else return false;
    }
    _ItemInstList.Add(itemInstId);
    return true;
}

private bool TransferItem()
{
    var container = virtualPanel.GetContainerByInstId(_AutoTransferContainerId);
    var itemInstId = _ItemInstList[0];  // 取最旧的（FIFO）
    virtualPanel.TransferItem(itemInstId, container.InstanceId);
    return true;
}
```

> **与 Project Ark 的对应**：Project Ark 的 `EvictBlockingItems()` 实现了类似功能（强制替换时卸载阻碍部件），但逻辑在 `DragDropManager` 层，而非 DropLogic 层。WkecWulin 的 AutoTransfer 更通用——任何容器都可以配置，不需要修改管理器代码。

---

### 6.3 商店来源检测（从背包拖出 vs 从商店拖出）

```csharp
// VirtualPanel 维护一个商店容器 ID 集合
[Header("商品格拖曳")]
public int[] _ShopContainerIds;  // Inspector 中配置哪些 Container 是商店
private HashSet<int> _ShopIds = new HashSet<int>();

// 事件
public UnityEvent<int> _OnBeginDragItemFromShop;  // 开始从商店拖出（可用于显示「购买确认」UI）
public UnityEvent<int> _OnMoveItemFromShop;        // 成功放入目标（触发购买逻辑）
public UnityEvent<int[]> _OnEvent_ItemTransfer;    // 任意转移事件 [instId, fromId, toId]

// OnBeginDrag 时检测
if (_ShopIds.Contains(item.ContainerId))
    _OnBeginDragItemFromShop?.Invoke(itemInstId);

// OnDropSuccess 时检测
if (_ShopIds.Contains(item.ContainerId))
    _OnMoveItemFromShop?.Invoke(itemInstId);
```

---

### 6.4 音效系统（UIDragDropSound 独立组件）

背包版本的音效不在 VirtualPanel 里硬编码，而是通过 Handler 的 `Action` 回调注入：

```csharp
// Handler 暴露两个 Action
public Action _OnBeginDraggingAction;  // 拖拽开始时
public Action _OnEndDraggingAction;    // 拖拽结束时

// UIDragDropSound 组件（挂在同一 GameObject 上）
public class UIDragDropSound : MonoBehaviour
{
    private UIDragDropHandler _handler;

    void Awake()
    {
        _handler = GetComponent<UIDragDropHandler>();
        _handler._OnBeginDraggingAction += PlayAudio_Start;
        _handler._OnEndDraggingAction += PlayAudio_End;
    }

    void OnDestroy()
    {
        _handler._OnBeginDraggingAction -= PlayAudio_Start;
        _handler._OnEndDraggingAction -= PlayAudio_End;
    }

    void PlayAudio_Start() { /* 播放拾取音效 */ }
    void PlayAudio_End()   { /* 播放放置/弹回音效 */ }
}
```

> **设计亮点**：音效组件完全独立，不修改 Handler 代码即可添加/移除音效。符合开闭原则。

---

### 6.5 Container 的 OnDrop 完整流程（背包版本）

```csharp
public void OnDrop(PointerEventData eventData)
{
    // 1. 记录精确落点（用于动画定位）
    _LastDropPosition = eventData.position;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        _RectTransform, _LastDropPosition, eventData.enterEventCamera, out _LastDropLocalPosition);

    int itemInstId = virtualPanel.CurrentItemInstId;

    if (_ListenDrag)
    {
        SetListen(false);  // 立即关闭监听（防止重复触发）

        if (_EnableDrop)
        {
            bool dropSuccess = _DropLogic != null
                && _DropLogic.CheckDropCondition(itemInstId)  // 再次验证（双重保险）
                && _DropLogic._DragListenOnly == false;       // 非只读容器

            if (dropSuccess)
            {
                // 成功路径：数据转移
                virtualPanel.OnDropSuccess(eventData, itemInstId, this.InstanceId);
                // OnDropSuccess 内部：
                //   1. 检测商店来源 → _OnMoveItemFromShop
                //   2. TransferItem(from → to)
                //      → fromContainer.RemoveItem()
                //      → item.SetContainerTo(toId)
                //      → toContainer.AddItem()  ← 触发 DropItemToEmptySlot
                //      → _OnEvent_ItemTransfer?.Invoke([instId, from, to])
            }
        }
    }

    // 无论成功失败，都调用 OnDropEnd 结束拖拽
    virtualPanel.OnDropEnd();
    SetInside(false);
    // OnDropEnd 内部：HideVirtualTarget + OnEndDragEvent(null) → ReleaseDragHandler
}
```

---

### 6.6 Container 的槽位管理（背包版本）

```csharp
// Container 持有一个 UIDragDropItem 列表（每个 Item 是一个格子视图）
protected List<UIDragDropItem> _DragDropItemSlots;

// Start 时扫描子节点自动收集
void ReCollectSlots()
{
    _DragDropItemSlots.Clear();
    var root = _ItemSlotsParent;
    for (int i = 0; i < root.childCount; i++)
    {
        var slot = root.GetChild(i).GetComponentInChildren<UIDragDropItem>(true);
        slot.ResetContainerTo(this._InstanceId);  // 将格子的归属 ContainerId 设为本容器
        _DragDropItemSlots.Add(slot);
        if (slot.HasItem) _ItemCnt++;
        else if (_EmptyItemSlot == null) _EmptyItemSlot = slot;  // 缓存第一个空格
    }
}

// 放入物品：找第一个空格
private bool DropItemToEmptySlot(int itemInstId)
{
    if (_EmptyItemSlot != null && !_EmptyItemSlot.HasItem)
    {
        _EmptyItemSlot.SetupItem(itemInstId);  // 格子显示物品图标
        SetCurrentSlot(_EmptyItemSlot);
        _EmptyItemSlot = null;  // 清空缓存，下次重新找
        return true;
    }
    // 缓存失效时遍历找下一个空格
    foreach (var slot in _DragDropItemSlots)
        if (!slot.HasItem) { _EmptyItemSlot = slot; break; }
    return false;
}

// _SlotDirty 机制：动态添加格子后标记脏，LateUpdate 中重新收集
public void SetSlotDirty() { _SlotDirty = true; }
void LateUpdate() { if (_SlotDirty) { _SlotDirty = false; ReCollectSlots(); } }
```

---

## 七、与 Project Ark 现有系统完整对比

> 基于 Project Ark 当前实现（`DragDropManager.cs` / `SlotCellView.cs` / `InventoryItemView.cs` / `DragGhostView.cs` / `FlyBackAnimator.cs`）

### 7.1 功能对比总表

| 特性 | WkecWulin 背包版本 | Project Ark 当前 | 差距评估 |
|------|-------------------|-----------------|----------|
| **Ghost 图像** | ✅ VirtualTarget（SetActive切换） | ✅ DragGhostView（PrimeTween动画） | PA 更优 |
| **Ghost 形状** | ❌ 固定单格 | ✅ SetShape(N) 多格动态网格 | PA 更优 |
| **拖拽源半透明** | ✅ alpha=0.5 + raycastTarget=false | ✅ CanvasGroup alpha 控制 | 相当 |
| **广播机制** | ✅ BeginDrag广播给所有Container | ⚠️ HighlightMatchingColumns（硬编码类型匹配） | WK 更优 |
| **验证器分离** | ✅ UIDropLogic 独立组件（Inspector配置） | ⚠️ 验证逻辑内联在 SlotCellView.IsTypeMatch | WK 更优 |
| **放置预览** | ⚠️ Listen/Inside 双状态（无颜色区分） | ✅ Valid/Replace/Invalid 三色预览 | PA 更优 |
| **放置动画** | ❌ 无 | ✅ PrimeTween snap-in（scale 1.18→0.96→1.0） | PA 更优 |
| **弹回动画** | ❌ 无 | ✅ FlyBackAnimator（飞行克隆体+落地弹跳） | PA 更优 |
| **Tooltip 悬停** | ✅ 完整实现（多类型/跟随/边界校正/自动缩放） | ✅ ItemTooltipView（150ms延迟/淡入淡出/边界翻转） | 相当 |
| **Tooltip 拖拽中** | ✅ 自动消失（raycastTarget=false） | ✅ 拖拽期间屏蔽（IsDragging 检测） | 相当 |
| **音效集成** | ✅ UIDragDropSound 独立组件（Action回调注入） | ❌ 未实现 | **WK 有，PA 缺** |
| **AutoTransfer** | ✅ 满了自动把旧物品转移到指定容器 | ⚠️ EvictBlockingItems（在Manager层，非通用） | WK 更通用 |
| **只读容器** | ✅ _DragListenOnly（高亮但不接受放置） | ❌ 无 | **WK 有，PA 缺** |
| **来源容器检测** | ✅ _CheckContainerId（限制只能从指定容器拖入） | ❌ 无 | **WK 有，PA 缺** |
| **商店来源事件** | ✅ _OnBeginDragItemFromShop / _OnMoveItemFromShop | ❌ 无（星图无商店概念） | 不适用 |
| **防误触机制** | ✅ 拖拽后0.5s内忽略点击 | ❌ 无 | **WK 有，PA 缺** |
| **多场景支持** | ✅ scene.name 作 key 的字典注册 | ⚠️ 单例（仅支持单场景） | WK 更通用 |
| **Container 注销** | ✅ OnDestroy 自动注销 | ❌ 无（单例不需要） | WK 更严谨 |
| **槽位脏标记** | ✅ _SlotDirty + LateUpdate 延迟重收集 | ❌ 无（固定槽位不需要） | 不适用 |
| **多格支持** | ❌ 无 | ✅ SlotSize + SetMultiCellHighlight | PA 更优 |
| **TypeColumn 候选高亮** | ❌ 无 | ✅ SetDropCandidate() 呼吸脉冲动画 | PA 更优 |
| **强制替换** | ⚠️ AutoTransfer（FIFO转移） | ✅ EvictBlockingItems（精确卸载阻碍部件） | PA 更优 |

---

### 7.2 Project Ark 缺失的功能详细分析

#### ❌ 缺失 1：音效系统

**WkecWulin 方案**：`UIDragDropSound` 独立组件，通过 `Action` 回调注入，不修改 Handler 代码。

**Project Ark 现状**：`DragDropManager` 中没有任何音效调用，`InventoryItemView` 和 `SlotCellView` 也没有。

**建议方案**（符合 Project Ark 架构）：
```csharp
// 在 DragDropManager 中添加事件
public event Action OnDragBegan;    // BeginDrag() 末尾触发
public event Action OnDropSuccess;  // EndDrag(true) 时触发
public event Action OnDropFailed;   // CancelDrag() 时触发

// 独立的 StarChartDragSound 组件订阅这些事件
// 符合 CLAUDE.md 的「事件驱动通信」原则
```

---

#### ❌ 缺失 2：防误触机制

**WkecWulin 方案**：
```csharp
private float _LastDraggingTime = 0;
private const float _AfterDraggingTimeThreshold = 0.5f;

public void SwitchLocked()
{
    if (Time.time - _LastDraggingTime < _AfterDraggingTimeThreshold) return;
    SetIsLocked(!_Locked);
}
```

**Project Ark 现状**：`OnEndDrag` 和 `OnPointerUp` 同帧触发时，如果槽位上有交互按钮（如「卸载」按钮），可能误触发。

**建议方案**：在 `DragDropManager.CancelDrag()` / `EndDrag()` 中记录时间戳，`SlotCellView.OnPointerClick()` 中检测。

---

#### ❌ 缺失 3：_DragListenOnly 只读容器

**WkecWulin 方案**：Container 设置 `_DragListenOnly=true` 后，拖拽时会高亮（进入 Listen 状态），但 `OnDrop` 时不执行转移。

**Project Ark 潜在需求**：
- 星图面板的「物品详情区」：拖拽时高亮显示「可查看详情」，但不接受放置
- 「已锁定轨道」：视觉上响应拖拽（显示锁定图标），但不接受放置

**建议方案**：在 `SlotCellView` 中添加 `bool _PreviewOnly` 字段，`OnDrop` 时检测。

---

#### ⚠️ 待改进：广播机制

**WkecWulin 方案**：VirtualPanel 在 `BeginDrag` 时广播给所有注册的 Container，Container 自己判断是否进入 Listen 状态。

**Project Ark 现状**：`DragDropManager.HighlightMatchingColumns()` 通过 `GetComponentsInChildren<TrackView>()` 遍历，再调用 `GetColumn(matchType).SetDropHighlight()`。

**问题**：
1. 每次 `BeginDrag` 都调用 `GetComponentsInChildren`（有 GC 分配）
2. 新增 SlotType 需要修改 `HighlightMatchingColumns` 的 switch 逻辑

**建议方案**：
```csharp
// DragDropManager 维护注册列表
private List<IDropTarget> _registeredTargets = new List<IDropTarget>();

public void RegisterTarget(IDropTarget target) => _registeredTargets.Add(target);
public void UnregisterTarget(IDropTarget target) => _registeredTargets.Remove(target);

// BeginDrag 时广播
foreach (var target in _registeredTargets)
    target.OnBeginDragEvent(payload);  // 各 target 自行判断是否高亮
```

---

#### ⚠️ 待改进：AutoTransfer 通用化

**WkecWulin 方案**：`UIDropLogic._AutoTransfer` 是 Container 级别的配置，任何容器都可以独立配置「满了转移到哪里」。

**Project Ark 现状**：`DragDropManager.EvictBlockingItems()` 是 Manager 级别的逻辑，只服务于「强制替换」场景，不通用。

**潜在需求**：如果未来星图有「快速装备」功能（拖到轨道区域自动找空槽），需要类似 AutoTransfer 的机制。

---

### 7.3 不建议借鉴的部分

| WkecWulin 特性 | 不借鉴原因 |
|----------------|------------|
| `SetActive` 控制显隐 | CLAUDE.md 禁止，统一用 CanvasGroup |
| `ItemUnitManager` 全局静态实例管理 | Project Ark 用 ServiceLocator，不需要全局静态管理器 |
| `UIDragDropItemGenerator` 随机生成 | 星图部件是固定列表，不需要随机生成 |
| `UIShopItemSlot` 商店格 | 星图无商店概念 |
| `scene.name` 多场景注册 | 当前单场景，单例足够；多场景时再重构 |
| `Coroutine` 异步（AutoScale） | CLAUDE.md 规定新代码用 UniTask，不用 Coroutine |
| `UnityEngine.UI.Text` | Project Ark 应使用 TextMeshPro |

---

### 7.4 优先级建议

| 优先级 | 功能 | 工作量 | 收益 |
|--------|------|--------|------|
| 🔴 高 | **音效系统**（拾取/放置/弹回三个音效事件） | 小（1-2h） | 手感提升显著 |
| 🔴 高 | **防误触机制**（拖拽后0.5s忽略点击） | 小（30min） | 防止 bug |
| 🟡 中 | **_DragListenOnly 只读容器** | 中（2-3h） | 为锁定轨道/预览区做准备 |
| 🟡 中 | **广播机制重构**（注册列表替代 GetComponentsInChildren） | 中（3-4h） | 架构更健壮，消除 GC |
| 🟢 低 | **AutoTransfer 通用化** | 大（1天） | 仅在「快速装备」功能需要时实现 |
| 🟢 低 | **来源容器过滤** | 小（1h） | 仅在有「只能从特定区域拖入」需求时实现 |
星图面板中的"信息预览区"可以用此模式实现：拖拽时高亮但不接受放置。

**4. 拖拽后防误触机制**

```csharp
// 拖拽结束后 0.5s 内忽略点击事件，防止误触发其他交互
private const float _AfterDraggingTimeThreshold = 0.5f;
```

### 无需借鉴（Project Ark 已更优）

- Ghost 动画（PrimeTween 更流畅）
- 放置预览三色反馈（更直观）
- FlyBackAnimator 弹回动画（更有手感）
- 多格 SlotSize 支持（星图部件需要）
