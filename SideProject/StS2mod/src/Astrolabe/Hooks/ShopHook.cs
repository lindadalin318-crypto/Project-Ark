using System.Reflection;
using Astrolabe.Core;
using Astrolabe.Data;
using Astrolabe.Engine;
using Astrolabe.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace Astrolabe.Hooks;

/// <summary>
/// Hook 商店界面（NMerchantInventory），触发购买建议并更新 HUD，
/// 同时在商店关闭时按同一 trace 回写玩家实际购买/删牌结果。
/// </summary>
public static class ShopHook
{
    private static readonly Logger _log = new("Astrolabe.ShopHook", LogType.Generic);
    private static readonly Dictionary<NMerchantInventory, ShopSession> ActiveSessions = new();

    public static void Register(Harmony harmony)
    {
        try
        {
            var inventoryType = typeof(NMerchantInventory);
            var readyMethod = inventoryType.GetMethod("_Ready",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var exitTreeMethod = inventoryType.GetMethod("_ExitTree",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (readyMethod != null)
            {
                var postfixMethod = typeof(ShopHook).GetMethod(
                    nameof(OnMerchantInventoryReady),
                    BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(readyMethod, postfix: new HarmonyMethod(postfixMethod));
                _log.Info("[ShopHook] Patched NMerchantInventory._Ready");
            }
            else
            {
                _log.Error("[ShopHook] Cannot find _Ready on NMerchantInventory");
            }

            if (exitTreeMethod != null)
            {
                var exitPostfix = typeof(ShopHook).GetMethod(
                    nameof(OnMerchantInventoryExitTree),
                    BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(exitTreeMethod, postfix: new HarmonyMethod(exitPostfix));
                _log.Info("[ShopHook] Patched NMerchantInventory._ExitTree");
            }
            else
            {
                _log.Error("[ShopHook] Cannot find _ExitTree on NMerchantInventory");
            }
        }
        catch (Exception ex)
        {
            _log.Error($"[ShopHook] Register failed: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    private static void OnMerchantInventoryReady(NMerchantInventory __instance)
    {
        try
        {
            _log.Info("[ShopHook] Merchant inventory ready.");

            RunSnapshot snapshot = RunStateReader.Capture();
            if (!snapshot.IsValid)
            {
                _log.Warn("[ShopHook] Invalid snapshot, skipping advice.");
                return;
            }

            BuildPathManager.UpdateViability(snapshot);

            MerchantInventory? merchantInventory = ExtractInventory(__instance);
            if (merchantInventory == null)
            {
                _log.Warn("[ShopHook] Could not extract MerchantInventory from NMerchantInventory.");
                return;
            }

            ShopItems shopItems = BuildShopItems(merchantInventory);
            var envelope = AdvisorEngine.AnalyzeShop(shopItems, snapshot);
            OverlayHUD.ShowShopAdvice(envelope);

            ActiveSessions[__instance] = new ShopSession(snapshot, shopItems, envelope);
            _log.Info($"[ShopHook] Shop advice generated: {envelope.Payload.Summary} / Trace: {envelope.TraceId}");
        }
        catch (Exception ex)
        {
            _log.Error($"[ShopHook] OnMerchantInventoryReady failed: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    private static void OnMerchantInventoryExitTree(NMerchantInventory __instance)
    {
        try
        {
            if (!ActiveSessions.TryGetValue(__instance, out var session))
                return;

            RunSnapshot exitSnapshot = RunStateReader.Capture();
            if (!exitSnapshot.IsValid)
                return;

            var choiceIds = InferPlayerChoiceIds(session, exitSnapshot);
            string playerChoiceId = choiceIds.Count == 0 ? "leave" : string.Join("|", choiceIds);
            int goldSpent = Math.Max(0, session.EntrySnapshot.Gold - exitSnapshot.Gold);

            var record = DecisionRecordFactory.CreatePlayerChoiceRecord(
                session.Envelope,
                exitSnapshot,
                playerChoiceId,
                source: "ShopHook.OnMerchantInventoryExitTree",
                extraMetadata: new Dictionary<string, string>
                {
                    ["screen"] = "shop",
                    ["choiceInference"] = choiceIds.Count == 0 ? "no-diff" : "snapshot-diff",
                    ["goldSpent"] = goldSpent.ToString(),
                },
                recommendedChoiceIds: BuildRecommendedChoiceIds(session.Envelope));

            DecisionRecorder.Record(record);
            _log.Info($"[ShopHook] Player shop choices recorded. Trace: {session.Envelope.TraceId}, Choice: {playerChoiceId}, Followed: {record.PlayerFollowedAdvice}");
        }
        catch (Exception ex)
        {
            _log.Error($"[ShopHook] OnMerchantInventoryExitTree failed: {ex.Message}");
        }
        finally
        {
            ActiveSessions.Remove(__instance);
        }
    }

    private static List<string> InferPlayerChoiceIds(ShopSession session, RunSnapshot exitSnapshot)
    {
        var choiceIds = new List<string>();

        AppendAddedChoices(
            choiceIds,
            session.EntrySnapshot.DeckCardIds,
            exitSnapshot.DeckCardIds,
            session.EntryShopItems.Cards.Select(card => card.CardId),
            static cardId => BuildShopOptionId(ShopItemType.Card, cardId),
            IdNormalizer.NormalizeModelId);

        AppendAddedChoices(
            choiceIds,
            session.EntrySnapshot.RelicIds,
            exitSnapshot.RelicIds,
            session.EntryShopItems.Relics.Select(relic => relic.RelicId),
            static relicId => BuildShopOptionId(ShopItemType.Relic, relicId),
            IdNormalizer.NormalizeLookupId);

        AppendAddedChoices(
            choiceIds,
            session.EntrySnapshot.PotionIds,
            exitSnapshot.PotionIds,
            session.EntryShopItems.Potions.Select(potion => potion.PotionId),
            static potionId => BuildShopOptionId(ShopItemType.Potion, potionId),
            IdNormalizer.NormalizeLookupId);

        AppendRemovedDeckChoices(choiceIds, session.EntrySnapshot.DeckCardIds, exitSnapshot.DeckCardIds);

        return choiceIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AppendAddedChoices(
        List<string> choiceIds,
        IEnumerable<string> beforeIds,
        IEnumerable<string> afterIds,
        IEnumerable<string> candidateIds,
        Func<string, string> optionIdFactory,
        Func<string, string> normalize)
    {
        var beforeCounts = BuildCounts(beforeIds, normalize);
        var afterCounts = BuildCounts(afterIds, normalize);

        foreach (var rawCandidateId in candidateIds)
        {
            string candidateId = normalize(rawCandidateId);
            if (string.IsNullOrWhiteSpace(candidateId))
                continue;

            int addedCount = GetCount(afterCounts, candidateId) - GetCount(beforeCounts, candidateId);
            for (int i = 0; i < addedCount; i++)
                choiceIds.Add(optionIdFactory(candidateId));
        }
    }

    private static void AppendRemovedDeckChoices(
        List<string> choiceIds,
        IEnumerable<string> beforeDeckIds,
        IEnumerable<string> afterDeckIds)
    {
        var beforeCounts = BuildCounts(beforeDeckIds, IdNormalizer.NormalizeModelId);
        var afterCounts = BuildCounts(afterDeckIds, IdNormalizer.NormalizeModelId);

        foreach (var pair in beforeCounts)
        {
            int removedCount = pair.Value - GetCount(afterCounts, pair.Key);
            for (int i = 0; i < removedCount; i++)
                choiceIds.Add(BuildShopRemoveChoiceId(pair.Key));
        }
    }

    private static List<string> BuildRecommendedChoiceIds(AdviceEnvelope<ShopAdvice> envelope)
    {
        var recommendedIds = new List<string>(envelope.RecommendedOptionIds);

        if (envelope.Metadata.TryGetValue("removeCardId", out var removeCardId)
            && !string.IsNullOrWhiteSpace(removeCardId))
        {
            recommendedIds.Add(BuildShopRemoveChoiceId(removeCardId));
        }

        return recommendedIds
            .Where(optionId => !string.IsNullOrWhiteSpace(optionId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, int> BuildCounts(IEnumerable<string> ids, Func<string, string> normalize)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawId in ids)
        {
            string id = normalize(rawId);
            if (string.IsNullOrWhiteSpace(id))
                continue;

            counts[id] = GetCount(counts, id) + 1;
        }

        return counts;
    }

    private static int GetCount(IReadOnlyDictionary<string, int> counts, string id)
        => counts.TryGetValue(id, out int count) ? count : 0;

    private static string BuildShopOptionId(ShopItemType itemType, string itemId)
        => $"{itemType.ToString().ToLowerInvariant()}:{itemId}";

    private static string BuildShopRemoveChoiceId(string cardId)
        => $"remove:{IdNormalizer.NormalizeModelId(cardId)}";

    private static MerchantInventory? ExtractInventory(NMerchantInventory node)
    {
        try
        {
            var property = typeof(NMerchantInventory).GetProperty(
                "Inventory",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property?.GetValue(node) is MerchantInventory inventoryFromProperty)
                return inventoryFromProperty;

            var field = typeof(NMerchantInventory).GetField(
                "_inventory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(node) is MerchantInventory inventoryFromField)
                return inventoryFromField;

            field = typeof(NMerchantInventory).GetField(
                "Inventory",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(node) as MerchantInventory;
        }
        catch
        {
            return null;
        }
    }

    private static ShopItems BuildShopItems(MerchantInventory inventory)
    {
        var items = new ShopItems();

        foreach (MerchantCardEntry entry in inventory.CharacterCardEntries.Concat(inventory.ColorlessCardEntries))
        {
            if (entry.CreationResult?.Card == null) continue;
            var card = entry.CreationResult.Card;
            items.Cards.Add(new ShopCardItem
            {
                CardId = IdNormalizer.NormalizeModelId(
                    card.IsUpgraded ? card.Id.Entry + "+" : card.Id.Entry),
                Price = entry.Cost,
                IsSale = entry.IsOnSale,
                IsSold = !entry.IsStocked,
            });
        }

        foreach (MerchantRelicEntry entry in inventory.RelicEntries)
        {
            items.Relics.Add(new ShopRelicItem
            {
                RelicId = entry.IsStocked ? GetRelicId(entry) : "sold",
                Price = entry.Cost,
                IsSold = !entry.IsStocked,
            });
        }

        foreach (MerchantPotionEntry entry in inventory.PotionEntries)
        {
            items.Potions.Add(new ShopPotionItem
            {
                PotionId = entry.IsStocked ? GetPotionId(entry) : "sold",
                Price = entry.Cost,
                IsSold = !entry.IsStocked,
            });
        }

        return items;
    }

    private static string GetRelicId(MerchantRelicEntry entry)
    {
        try
        {
            foreach (string fieldName in new[] { "_relic", "Relic", "relic" })
            {
                var field = typeof(MerchantRelicEntry).GetField(
                    fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field?.GetValue(entry) is MegaCrit.Sts2.Core.Models.RelicModel relic)
                    return IdNormalizer.NormalizeLookupId(relic.Id.Entry);
            }
        }
        catch
        {
        }

        return "unknown_relic";
    }

    private static string GetPotionId(MerchantPotionEntry entry)
    {
        try
        {
            foreach (string fieldName in new[] { "_potion", "Potion", "potion" })
            {
                var field = typeof(MerchantPotionEntry).GetField(
                    fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field?.GetValue(entry) is MegaCrit.Sts2.Core.Models.PotionModel potion)
                    return IdNormalizer.NormalizeLookupId(potion.Id.Entry);
            }
        }
        catch
        {
        }

        return "unknown_potion";
    }

    private sealed class ShopSession
    {
        public ShopSession(
            RunSnapshot entrySnapshot,
            ShopItems entryShopItems,
            AdviceEnvelope<ShopAdvice> envelope)
        {
            EntrySnapshot = entrySnapshot;
            EntryShopItems = entryShopItems;
            Envelope = envelope;
        }

        public RunSnapshot EntrySnapshot { get; }
        public ShopItems EntryShopItems { get; }
        public AdviceEnvelope<ShopAdvice> Envelope { get; }
    }
}
