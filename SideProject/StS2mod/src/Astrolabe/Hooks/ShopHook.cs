using System.Reflection;
using Astrolabe.Core;
using Astrolabe.Engine;
using Astrolabe.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace Astrolabe.Hooks;

/// <summary>
/// Hook 商店界面（NMerchantInventory），触发购买建议并更新 HUD。
///
/// 已通过 ILSpy 确认：
///   - 商品节点：MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantInventory
///   - 数据类：MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory
///   - 卡牌列表：MerchantInventory.CharacterCardEntries（IReadOnlyList&lt;MerchantCardEntry&gt;）
///   - 遗物列表：MerchantInventory.RelicEntries（IReadOnlyList&lt;MerchantRelicEntry&gt;）
///   - 药水列表：MerchantInventory.PotionEntries（IReadOnlyList&lt;MerchantPotionEntry&gt;）
///   - MerchantCardEntry 包含 CardModel Card 和 int Price 字段
/// </summary>
public static class ShopHook
{
    private static readonly Logger _log = new("Astrolabe.ShopHook", LogType.Generic);

    public static void Register(Harmony harmony)
    {
        try
        {
            var inventoryType = typeof(NMerchantInventory);
            var readyMethod = inventoryType.GetMethod("_Ready",
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

            // 从 NMerchantInventory 中通过 Reflection 读取 MerchantInventory 数据
            MerchantInventory? merchantInventory = ExtractInventory(__instance);
            if (merchantInventory == null)
            {
                _log.Warn("[ShopHook] Could not extract MerchantInventory from NMerchantInventory.");
                return;
            }

            ShopItems shopItems = BuildShopItems(merchantInventory);
            var advice = AdvisorEngine.AnalyzeShop(shopItems, snapshot);
            OverlayHUD.ShowShopAdvice(advice);

            _log.Info($"[ShopHook] Shop advice generated: {advice.Summary}");
        }
        catch (Exception ex)
        {
            _log.Error($"[ShopHook] OnMerchantInventoryReady failed: {ex.Message}");
        }
    }

    private static MerchantInventory? ExtractInventory(NMerchantInventory node)
    {
        try
        {
            // NMerchantInventory 内部持有 MerchantInventory 数据对象
            // 字段名需要通过反编译 NMerchantInventory 确认（可能是 _inventory / Inventory 等）
            var field = typeof(NMerchantInventory).GetField(
                "_inventory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                field = typeof(NMerchantInventory).GetField(
                    "Inventory",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
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

        // 卡牌（角色卡 + 无色卡）
        // CreationResult?.Card 是 CardModel；IsStocked=true 表示有货，IsOnSale 表示打折
        foreach (MerchantCardEntry entry in inventory.CharacterCardEntries.Concat(inventory.ColorlessCardEntries))
        {
            if (entry.CreationResult?.Card == null) continue;
            var card = entry.CreationResult.Card;
            items.Cards.Add(new ShopCardItem
            {
                CardId = card.IsUpgraded ? card.Id.Entry + "+" : card.Id.Entry,
                Price = entry.Cost,
                IsSale = entry.IsOnSale,
                IsSold = !entry.IsStocked, // IsStocked=false 时已售出
            });
        }

        // 遗物 — MerchantRelicEntry 继承 MerchantEntry，字段需反编译确认
        foreach (MerchantRelicEntry entry in inventory.RelicEntries)
        {
            items.Relics.Add(new ShopRelicItem
            {
                RelicId = entry.IsStocked ? GetRelicId(entry) : "sold",
                Price = entry.Cost,
                IsSold = !entry.IsStocked,
            });
        }

        // 药水 — MerchantPotionEntry 继承 MerchantEntry
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

    // 通过 Reflection 获取 RelicEntry 中的 Relic ID
    // MerchantRelicEntry.Relic 字段名需要反编译确认，此处用反射安全访问
    private static string GetRelicId(MerchantRelicEntry entry)
    {
        try
        {
            // 尝试常见字段名
            foreach (string fieldName in new[] { "_relic", "Relic", "relic" })
            {
                var field = typeof(MerchantRelicEntry).GetField(
                    fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field?.GetValue(entry) is MegaCrit.Sts2.Core.Models.RelicModel relic)
                    return relic.Id.Entry;
            }
        }
        catch { }
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
                    return potion.Id.Entry;
            }
        }
        catch { }
        return "unknown_potion";
    }
}
