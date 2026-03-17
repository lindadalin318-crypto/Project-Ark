using System.Reflection;
using Astrolabe.Data;
using Astrolabe.Engine;
using Astrolabe.UI;

using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace Astrolabe.Hooks;

/// <summary>
/// Hook 战斗系统，订阅 CombatManager.TurnStarted 事件。
/// 
/// 每次玩家回合开始时：
///   1. 从 CombatState 构建 CombatSnapshot（手牌、能量、敌人意图、牌堆状态）
///   2. 调用 CombatAdvisor.Analyze() 生成出牌建议
///   3. 推送到 OverlayHUD.ShowCombatAdvice() 更新面板
/// 
/// 出牌中实时更新：
///   - 订阅 Hand.ContentsChanged + EnergyChanged 事件
///   - 去抖动（同帧多次触发合并为一次重算）
///   - 保留最新 CombatState 引用，重算时直接用
/// 
/// 同时订阅 TurnEnded 事件，回合结束时隐藏面板。
/// </summary>
public static class CombatHook
{
    private static readonly Logger _log = new("Astrolabe.CombatHook", LogType.Generic);
    private static bool _subscribed = false;

    // ── 实时重算状态 ──────────────────────────────────────────────────
    /// <summary>当前玩家回合的 CombatState，出牌后实时重算用</summary>
    private static CombatState? _currentCombatState;
    /// <summary>去抖动：标记是否已安排了重算，避免同帧多次触发</summary>
    private static bool _reanalyzePending = false;
    /// <summary>是否处于玩家回合中（防止敌人回合误触发）</summary>
    private static bool _isPlayerTurn = false;
    /// <summary>本回合已出牌数（Hand 每次 Remove 时递增）</summary>
    private static int _cardsPlayedThisTurn = 0;

    public static void Register(Harmony harmony)
    {
        try
        {
            // Hook NCombatRoom._Ready() 来完成 CombatManager 事件订阅
            // NCombatRoom 是战斗场景的根节点，_Ready 时 CombatManager 已初始化
            var combatRoomType = GetCombatRoomType();
            if (combatRoomType == null)
            {
                _log.Error("[CombatHook] Cannot find NCombatRoom type.");
                return;
            }

            var readyMethod = combatRoomType.GetMethod("_Ready",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var exitMethod = combatRoomType.GetMethod("_ExitTree",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (readyMethod != null)
            {
                harmony.Patch(readyMethod, postfix: new HarmonyMethod(
                    typeof(CombatHook).GetMethod(nameof(OnCombatRoomReady),
                        BindingFlags.Static | BindingFlags.NonPublic)));
                _log.Info("[CombatHook] Patched NCombatRoom._Ready");
            }

            if (exitMethod != null)
            {
                harmony.Patch(exitMethod, postfix: new HarmonyMethod(
                    typeof(CombatHook).GetMethod(nameof(OnCombatRoomExit),
                        BindingFlags.Static | BindingFlags.NonPublic)));
                _log.Info("[CombatHook] Patched NCombatRoom._ExitTree");
            }
        }
        catch (Exception ex)
        {
            _log.Error($"[CombatHook] Register failed: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    private static void OnCombatRoomReady()
    {
        if (_subscribed) return;
        try
        {
            CombatManager.Instance.TurnStarted += OnTurnStarted;
            CombatManager.Instance.TurnEnded   += OnTurnEnded;
            _subscribed = true;
            _log.Info("[CombatHook] Subscribed to CombatManager.TurnStarted/TurnEnded");
        }
        catch (Exception ex)
        {
            _log.Error($"[CombatHook] OnCombatRoomReady failed: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    private static void OnCombatRoomExit()
    {
        try
        {
            UnsubscribeCardPileEvents();
            CombatManager.Instance.TurnStarted -= OnTurnStarted;
            CombatManager.Instance.TurnEnded   -= OnTurnEnded;
            _subscribed         = false;
            _isPlayerTurn       = false;
            _currentCombatState = null;
            OverlayHUD.HideCombatAdvice();
            _log.Info("[CombatHook] Unsubscribed from CombatManager events.");
        }
        catch (Exception ex)
        {
            _log.Warn($"[CombatHook] OnCombatRoomExit cleanup failed: {ex.Message}");
        }
    }

    private static void OnTurnStarted(CombatState combatState)
    {
        if (combatState.CurrentSide != CombatSide.Player) return;

        try
        {
            _isPlayerTurn       = true;
            _cardsPlayedThisTurn = 0;
            _currentCombatState = combatState;

            // 订阅玩家手牌和能量变化，用于出牌后实时重算
            SubscribeCardPileEvents(combatState);

            var snap   = BuildSnapshot(combatState);
            var advice = CombatAdvisor.Analyze(snap);
            OverlayHUD.ShowCombatAdvice(advice);
            _log.Info($"[CombatHook] Turn {snap.RoundNumber}: {snap.HandCards.Count} cards, " +
                      $"{snap.CurrentEnergy} energy, draw={snap.DrawPileCount}, discard={snap.DiscardPileCount}. {advice.SummaryText}");
        }
        catch (Exception ex)
        {
            _log.Error($"[CombatHook] OnTurnStarted failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void OnTurnEnded(CombatState _)
    {
        _isPlayerTurn = false;
        UnsubscribeCardPileEvents();
        OverlayHUD.HideCombatAdvice();
    }

    // ── 实时出牌监听 ──────────────────────────────────────────────────

    private static PlayerCombatState? _subscribedPcs = null;

    private static void SubscribeCardPileEvents(CombatState combatState)
    {
        UnsubscribeCardPileEvents();
        var player = combatState.Players.FirstOrDefault();
        var pcs = player?.PlayerCombatState;
        if (pcs == null) return;

        _subscribedPcs = pcs;
        // 手牌内容变化（出牌、摸牌、弃牌 均触发）
        pcs.Hand.ContentsChanged  += OnHandContentsChanged;
        // 能量变化（出牌消耗能量时触发）
        pcs.EnergyChanged         += OnEnergyChanged;
        // 手牌有牌移除时计入已出牌数
        pcs.Hand.CardRemoved      += OnHandCardRemoved;
    }

    private static void UnsubscribeCardPileEvents()
    {
        if (_subscribedPcs == null) return;
        _subscribedPcs.Hand.ContentsChanged -= OnHandContentsChanged;
        _subscribedPcs.EnergyChanged        -= OnEnergyChanged;
        _subscribedPcs.Hand.CardRemoved     -= OnHandCardRemoved;
        _subscribedPcs = null;
    }

    /// <summary>手牌有牌被移除（打出/弃掉）时，累计已出牌数</summary>
    private static void OnHandCardRemoved(CardModel card)
    {
        if (!_isPlayerTurn) return;
        _cardsPlayedThisTurn++;
    }

    /// <summary>手牌内容变化时触发重算（去抖动：同帧多次变化只算一次）</summary>
    private static void OnHandContentsChanged()
    {
        if (!_isPlayerTurn || _reanalyzePending) return;
        _reanalyzePending = true;
        // 用 Godot 的 SceneTree.ProcessFrame 延迟到当帧末尾执行，合并同帧的多次触发
        if (Godot.Engine.GetMainLoop() is Godot.SceneTree tree)
        {
            tree.Connect(
                Godot.SceneTree.SignalName.ProcessFrame,
                Godot.Callable.From(OnDeferredReanalyze),
                (uint)Godot.GodotObject.ConnectFlags.OneShot);
        }
    }

    /// <summary>能量变化时也触发重算（X 费牌等对能量敏感）</summary>
    private static void OnEnergyChanged(int oldEnergy, int newEnergy)
    {
        if (!_isPlayerTurn) return;
        OnHandContentsChanged(); // 复用同一个去抖动路径
    }

    /// <summary>帧末统一执行重算</summary>
    private static void OnDeferredReanalyze()
    {
        _reanalyzePending = false;
        if (_currentCombatState == null || !_isPlayerTurn) return;

        try
        {
            var snap   = BuildSnapshot(_currentCombatState);
            var advice = CombatAdvisor.Analyze(snap);
            OverlayHUD.ShowCombatAdvice(advice);
        }
        catch (Exception ex)
        {
            _log.Warn($"[CombatHook] OnDeferredReanalyze failed: {ex.Message}");
        }
    }

    // ── 构建战斗快照 ──────────────────────────────────────────────────

    private static CombatSnapshot BuildSnapshot(CombatState combatState)
    {
        var snap = new CombatSnapshot
        {
            RoundNumber          = combatState.RoundNumber,
            CardsPlayedThisTurn  = _cardsPlayedThisTurn,
        };

        // 玩家状态
        var player = combatState.Players.FirstOrDefault();
        if (player != null)
        {
            var pcs = player.PlayerCombatState;
            if (pcs != null)
            {
                snap.CurrentEnergy   = pcs.Energy;
                snap.MaxEnergy       = pcs.MaxEnergy;
                snap.CurrentHP       = player.Creature.CurrentHp;
                snap.MaxHP           = player.Creature.MaxHp;
                snap.CurrentBlock    = player.Creature.Block;

                // 牌堆状态
                snap.DrawPileCount    = pcs.DrawPile.Cards.Count;
                snap.DiscardPileCount = pcs.DiscardPile.Cards.Count;
                snap.ExhaustPileCount = pcs.ExhaustPile.Cards.Count;

                // 手牌
                foreach (var card in pcs.Hand.Cards)
                {
                    string runtimeCardId = IdNormalizer.NormalizeModelId(
                        card.IsUpgraded ? card.Id.Entry + "+" : card.Id.Entry);
                    var cardData = DataLoader.GetCard(runtimeCardId);
                    bool isXCost = card.EnergyCost?.CostsX ?? false;
                    int cost     = isXCost ? -1 : (card.EnergyCost?.Canonical ?? 1);

                    string nameZh;
                    try { nameZh = card.Title; }
                    catch { nameZh = cardData?.NameZh ?? runtimeCardId; }

                    var tags = BuildCardTags(card, cardData);
                    snap.HandCards.Add(new CombatCardInfo
                    {
                        CardId     = runtimeCardId,
                        CardNameZh = nameZh,
                        CardType   = card.Type.ToString(),
                        Cost       = cost,
                        Tags       = tags,
                        Rarity     = card.Rarity.ToString(),
                    });


                    // 汇总手牌特征（供 Advisor 快速判断）
                    if (tags.Contains("block")) snap.HasBlockInHand = true;
                    if (card.Type.ToString() == "Power") snap.HasPowerInHand = true;
                }

                // 玩家 Powers（遗物效果已折算进去）
                foreach (var power in player.Creature.Powers)
                {
                    string nameZh;
                    try { nameZh = power.Title.GetFormattedText(); }
                    catch { nameZh = power.Id.Entry; }

                    snap.PlayerPowers.Add(new PowerInfo
                    {
                        PowerId  = power.Id.Entry,
                        NameZh   = nameZh,
                        Amount   = power.Amount,
                        IsDebuff = power.Type == PowerType.Debuff,
                    });
                }
            }
        }

        // 敌人 Powers（虚弱/易伤等）—— combatState.Enemies 元素本身是 Creature
        foreach (var enemyCreature in combatState.Enemies)
        {
            if (!enemyCreature.IsAlive || enemyCreature.Monster == null) continue;
            string enemyName = enemyCreature.Name ?? "敌人";
            foreach (var power in enemyCreature.Powers)
            {
                string nameZh;
                try { nameZh = power.Title.GetFormattedText(); }
                catch { nameZh = power.Id.Entry; }

                snap.EnemyPowers.Add(new EnemyPowerInfo
                {
                    EnemyName = enemyName,
                    PowerId   = power.Id.Entry,
                    NameZh    = nameZh,
                    Amount    = power.Amount,
                    IsDebuff  = power.Type == PowerType.Debuff,
                });
            }
        }

        // 敌人意图
        foreach (var enemy in combatState.Enemies)
        {
            if (!enemy.IsAlive || enemy.Monster == null) continue;

            var intentInfo = new EnemyIntentInfo
            {
                EnemyName = enemy.Name ?? "敌人",
            };

            // 读取所有意图，汇总攻击伤害
            var intents = enemy.Monster.NextMove?.Intents ?? (IReadOnlyList<AbstractIntent>)Array.Empty<AbstractIntent>();
            foreach (var intent in intents)
            {
                if (intent is AttackIntent attackIntent)
                {
                    intentInfo.WillAttack = true;
                    // 通过 Reflection 读取 Damage 字段（避免复杂的 GetTotalDamage 参数）
                    intentInfo.AttackDamage += TryGetAttackDamage(attackIntent);
                    intentInfo.IntentDesc    = "攻击";
                }
                else
                {
                    intentInfo.IntentDesc = intent.IntentType switch
                    {
                        IntentType.Buff    => "增益",
                        IntentType.Debuff  => "减益",
                        IntentType.Defend  => "格挡",
                        IntentType.Heal    => "治疗",
                        _                  => intent.IntentType.ToString(),
                    };
                }
            }

            snap.EnemyIntents.Add(intentInfo);
        }

        return snap;
    }

    private static int TryGetAttackDamage(AttackIntent intent)
    {
        try
        {
            // DamageCalc 是 Func<decimal>，直接调用无需传参
            if (intent.DamageCalc != null)
                return (int)Math.Round((double)intent.DamageCalc());
        }
        catch { }

        // Reflection 兜底：读取私有 _damage 或 damage 字段
        try
        {
            var field = intent.GetType().GetField("_damage", BindingFlags.Instance | BindingFlags.NonPublic)
                     ?? intent.GetType().GetField("damage",  BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null) return Convert.ToInt32(field.GetValue(intent));
        }
        catch { }

        return 0;
    }

    private static List<string> BuildCardTags(CardModel card, Data.CardData? data)
    {
        var tags = new List<string>();
        if (data != null)
            tags.AddRange(data.SynergyTags);

        // 使用游戏原生字段精确判断，替代字符串关键词匹配

        // 防御牌：GainsBlock 是 CardModel 虚方法，防御牌子类 override 为 true
        if (card.GainsBlock)
            tags.Add("block");

        // 摸牌效果：DynamicVars 含 "Cards" key（如 ShrugItOff、Acrobatics 等）
        try
        {
            if (card.DynamicVars.ContainsKey("Cards"))
                tags.Add("draw");
        }
        catch { }

        // 消耗牌：CardKeyword.Exhaust
        try
        {
            if (card.Keywords.Contains(CardKeyword.Exhaust))
                tags.Add("exhaust");
        }
        catch { }

        // 增益牌（给自己加力量/敏捷）：DynamicVars 含对应 PowerVar 的 key
        // PowerVar<T> 的 key = typeof(T).Name，所以 StrengthPower/DexterityPower 精确覆盖所有职业增益牌
        try
        {
            if (card.DynamicVars.ContainsKey("StrengthPower") || card.DynamicVars.ContainsKey("DexterityPower"))
                if (!tags.Contains("buff")) tags.Add("buff");
        }
        catch { }

        return tags;
    }

    private static Type? GetCombatRoomType()
    {
        // 搜索已加载的程序集找到 NCombatRoom
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType("MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom");
            if (t != null) return t;
        }
        return null;
    }
}
