using Astrolabe.Data;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Runs;


namespace Astrolabe.Core;

/// <summary>
/// 从游戏战斗运行时构建 CombatSnapshot。
/// 已通过 ILSpy 反编译 sts2.dll 验证所有字段路径。
///
/// 访问链（已确认）：
///   CombatManager.Instance.DebugOnlyGetState() → CombatState
///   CombatState.Enemies → IReadOnlyList&lt;Creature&gt;
///   CombatState.Players[0] → Player
///   Player.PlayerCombatState.Energy / Hand / DrawPile / DiscardPile
///   Creature.Monster.NextMove.Intents → IReadOnlyList&lt;AbstractIntent&gt;
///   AttackIntent.GetSingleDamage() + AttackIntent.Repeats → 伤害计算
/// </summary>
public static class CombatStateReader
{
    private static readonly Logger _log = new("Astrolabe.CombatStateReader", LogType.Generic);

    /// <summary>
    /// 构建战斗内实时状态快照。
    /// </summary>
    public static CombatSnapshot Capture()
    {
        var snapshot = new CombatSnapshot();
        try
        {
            // 先填充跑分全局状态
            RunSnapshot runSnapshot = RunStateReader.Capture();
            snapshot.HP = runSnapshot.HP;
            snapshot.MaxHP = runSnapshot.MaxHP;
            snapshot.Gold = runSnapshot.Gold;
            snapshot.Floor = runSnapshot.Floor;
            snapshot.Act = runSnapshot.Act;
            snapshot.CharacterId = runSnapshot.CharacterId;
            snapshot.DeckCardIds = runSnapshot.DeckCardIds;
            snapshot.RelicIds = runSnapshot.RelicIds;
            snapshot.PotionIds = runSnapshot.PotionIds;
            snapshot.ActBossId = runSnapshot.ActBossId;

            CombatState? combatState = CombatManager.Instance.DebugOnlyGetState();
            if (combatState == null)
            {
                _log.Warn("[CombatStateReader] No active combat state.");
                return snapshot;
            }

            snapshot.TurnNumber = combatState.RoundNumber;
            snapshot.IsPlayerTurn = combatState.CurrentSide == CombatSide.Player;

            if (combatState.Players.Count > 0)
            {
                Player player = combatState.Players[0];
                TryCapturePlayerCombatState(snapshot, player);
                TryCaptureHand(snapshot, player);
                TryCapturePiles(snapshot, player);
                TryCapturePlayerStatuses(snapshot, player);
            }

            TryCaptureEnemies(snapshot, combatState);
        }
        catch (Exception ex)
        {
            _log.Error($"[CombatStateReader] Failed to capture combat state: {ex.Message}");
        }

        return snapshot;
    }

    /// <summary>
    /// 检测当前是否正处于战斗中的玩家回合。
    /// </summary>
    public static bool IsInCombat()
    {
        try
        {
            return CombatManager.Instance.IsInProgress;
        }
        catch
        {
            return false;
        }
    }

    private static void TryCapturePlayerCombatState(CombatSnapshot snapshot, Player player)
    {
        try
        {
            PlayerCombatState? pcs = player.PlayerCombatState;
            if (pcs == null) return;

            snapshot.Energy = pcs.Energy;
            snapshot.MaxEnergy = pcs.MaxEnergy;
            snapshot.Block = player.Creature.Block;
        }
        catch (Exception ex)
        {
            _log.Warn($"[CombatStateReader] TryCapturePlayerCombatState failed: {ex.Message}");
        }
    }

    private static void TryCaptureHand(CombatSnapshot snapshot, Player player)
    {
        try
        {
            PlayerCombatState? pcs = player.PlayerCombatState;
            if (pcs == null) return;

            snapshot.HandCardIds = pcs.Hand.Cards
                .Select(c => IdNormalizer.NormalizeModelId(c.IsUpgraded ? c.Id.Entry + "+" : c.Id.Entry))
                .ToList();

        }
        catch (Exception ex)
        {
            _log.Warn($"[CombatStateReader] TryCaptureHand failed: {ex.Message}");
            snapshot.HandCardIds = new List<string>();
        }
    }

    private static void TryCapturePiles(CombatSnapshot snapshot, Player player)
    {
        try
        {
            PlayerCombatState? pcs = player.PlayerCombatState;
            if (pcs == null) return;

            snapshot.DrawPileCardIds = pcs.DrawPile.Cards
                .Select(c => IdNormalizer.NormalizeModelId(c.IsUpgraded ? c.Id.Entry + "+" : c.Id.Entry))
                .ToList();

            snapshot.DiscardPileCardIds = pcs.DiscardPile.Cards
                .Select(c => IdNormalizer.NormalizeModelId(c.IsUpgraded ? c.Id.Entry + "+" : c.Id.Entry))
                .ToList();

        }
        catch (Exception ex)
        {
            _log.Warn($"[CombatStateReader] TryCapturePiles failed: {ex.Message}");
            snapshot.DrawPileCardIds = new List<string>();
            snapshot.DiscardPileCardIds = new List<string>();
        }
    }

    private static void TryCaptureEnemies(CombatSnapshot snapshot, CombatState combatState)
    {
        try
        {
            snapshot.Enemies = combatState.Enemies
                .Where(e => e.IsAlive)
                .Select(e => BuildEnemyState(e, combatState))
                .ToList();
        }
        catch (Exception ex)
        {
            _log.Warn($"[CombatStateReader] TryCaptureEnemies failed: {ex.Message}");
            snapshot.Enemies = new List<EnemyState>();
        }
    }

    private static EnemyState BuildEnemyState(Creature enemy, CombatState combatState)
    {
        var state = new EnemyState
        {
            EnemyId = enemy.IsMonster ? IdNormalizer.NormalizeLookupId(enemy.Monster!.Id.Entry) : "unknown",

            EnemyName = enemy.Name,
            HP = enemy.CurrentHp,
            MaxHP = enemy.MaxHp,
            Block = enemy.Block,
        };

        try
        {
            if (enemy.IsMonster)
            {
                var nextMove = enemy.Monster!.NextMove;
                // 从 Intents 列表提取伤害意图
                foreach (AbstractIntent intent in nextMove.Intents)
                {
                    state.Intent = intent.IntentType.ToString();

                    if (intent is AttackIntent attackIntent)
                    {
                        // GetSingleDamage 需要目标列表，传入玩家阵营
                        var playerCreatures = combatState.PlayerCreatures;
                        state.IntentDamage = attackIntent.GetSingleDamage(playerCreatures, enemy);
                        state.IntentTimes = attackIntent.Repeats > 0 ? attackIntent.Repeats : 1;
                        break; // 取第一个攻击意图
                    }
                }
            }
        }
        catch
        {
            state.Intent = "Unknown";
        }

        return state;
    }

    private static void TryCapturePlayerStatuses(CombatSnapshot snapshot, Player player)
    {
        try
        {
            // PowerModel.Type = Buff/Debuff, PowerModel.Amount = 层数
            snapshot.PlayerStatuses = player.Creature.Powers
                .Select(p => new StatusEffect
                {
                    StatusId = IdNormalizer.NormalizeLookupId(p.Id.Entry),

                    StatusName = p.Title.GetFormattedText(),
                    Stacks = p.Amount,
                    IsPositive = p.TypeForCurrentAmount == PowerType.Buff,
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _log.Warn($"[CombatStateReader] TryCapturePlayerStatuses failed: {ex.Message}");
            snapshot.PlayerStatuses = new List<StatusEffect>();
        }
    }
}
