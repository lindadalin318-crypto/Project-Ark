using Astrolabe.Core;
using Astrolabe.Data;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;


namespace Astrolabe.Core;

/// <summary>
/// 从游戏运行时状态构建 RunSnapshot。
/// 已通过 ILSpy 反编译 sts2.dll 验证所有字段路径。
/// 
/// 访问链（已确认）：
///   RunManager.Instance.DebugOnlyGetState() → RunState
///   RunState.Players[0] → Player
///   Player.Creature → Creature (HP/Block)
///   Player.Deck.Cards → IReadOnlyList&lt;CardModel&gt;
///   Player.Relics → IReadOnlyList&lt;RelicModel&gt;
/// </summary>
public static class RunStateReader
{
    private static readonly Logger _log = new("Astrolabe.RunStateReader", LogType.Generic);

    /// <summary>
    /// 构建当前跑分状态快照。战斗外任意时刻均可调用。
    /// </summary>
    public static RunSnapshot Capture()
    {
        var snapshot = new RunSnapshot();
        try
        {
            RunState? runState = RunManager.Instance.DebugOnlyGetState();
            if (runState == null || runState.Players.Count == 0)
            {
                _log.Warn("[RunStateReader] No active run state or no players found.");
                return snapshot;
            }

            Player player = runState.Players[0];

            TryCapturePlayerStats(snapshot, player, runState);
            TryCaptureDeck(snapshot, player);
            TryCaptureRelics(snapshot, player);
            TryCaptureMapInfo(snapshot, runState);
        }
        catch (Exception ex)
        {
            _log.Error($"[RunStateReader] Failed to capture run state: {ex.Message}");
        }

        return snapshot;
    }

    private static void TryCapturePlayerStats(RunSnapshot snapshot, Player player, RunState runState)
    {
        try
        {
            snapshot.HP = player.Creature.CurrentHp;
            snapshot.MaxHP = player.Creature.MaxHp;
            snapshot.Gold = player.Gold;
            // Floor = 当前幕内楼层（ActFloor）+ 历史总楼层偏移
            snapshot.Floor = runState.ActFloor;
            snapshot.Act = runState.CurrentActIndex + 1; // 转为 1-based
            snapshot.CharacterId = IdNormalizer.NormalizeCharacterId(player.Character.Id.Entry);

        }
        catch (Exception ex)
        {
            _log.Warn($"[RunStateReader] TryCapturePlayerStats failed: {ex.Message}");
        }
    }

    private static void TryCaptureDeck(RunSnapshot snapshot, Player player)
    {
        try
        {
            // CardModel.Id.Entry 是游戏内 canonical 业务键（如 "STRIKE_IRONCLAD" / "LIMIT_BREAK"）。
            // 升级形态只在运行时追加 '+'，静态数据库仍按基础条目查询。
            snapshot.DeckCardIds = player.Deck.Cards
                .Select(c => IdNormalizer.NormalizeModelId(c.IsUpgraded ? c.Id.Entry + "+" : c.Id.Entry))
                .ToList();
        }

        catch (Exception ex)
        {
            _log.Warn($"[RunStateReader] TryCaptureDeck failed: {ex.Message}");
            snapshot.DeckCardIds = new List<string>();
        }
    }

    private static void TryCaptureRelics(RunSnapshot snapshot, Player player)
    {
        try
        {
            snapshot.RelicIds = player.Relics
                .Select(r => IdNormalizer.NormalizeLookupId(r.Id.Entry))
                .ToList();

            snapshot.PotionIds = player.Potions
                .Select(p => IdNormalizer.NormalizeLookupId(p.Id.Entry))
                .ToList();

        }
        catch (Exception ex)
        {
            _log.Warn($"[RunStateReader] TryCaptureRelics failed: {ex.Message}");
            snapshot.RelicIds = new List<string>();
            snapshot.PotionIds = new List<string>();
        }
    }

    private static void TryCaptureMapInfo(RunSnapshot snapshot, RunState runState)
    {
        try
        {
            // 路线规划需要的是当前幕实际分配到的 BossEncounter.Id.Entry，
            // 不是 Act 自身的 Id.Entry（如 "EXORDIUM" / "THE_CITY"）。
            snapshot.ActBossId = IdNormalizer.NormalizeLookupId(runState.Act.BossEncounter.Id.Entry);
        }

        catch (Exception ex)
        {
            _log.Warn($"[RunStateReader] TryCaptureMapInfo failed: {ex.Message}");
            snapshot.ActBossId = null;
        }
    }
}
