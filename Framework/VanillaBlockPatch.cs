using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 阻止原版武器/弹药/弹匣在世界生成、商人出售和合成配方中出现。
///
/// 需要拦截的三个系统：
/// 1. ItemLootPool.InitializePool — 物品战利池（世界容器和随机掉落的物品来源）
///    池是 Dictionary&lt;category, List&lt;itemId&gt;&gt;，InitializePool 从 Item.GlobalItems 按 category 分组
///    Postfix 在池初始化后移除被封禁的物品ID
///
/// 2. TraderScript.GenerateInventory — 商人库存生成
///    character==1（武器商）硬编码添加 smallmagazine/riflemagazine/boxof12gauge + pistol/rifle/shotgun
///    其他商人通过 GenerateSingleItemList 从 ItemLootPool 按类别随机选
///    Postfix 在库存生成后移除被封禁的 TraderItem
///
/// 3. Recipes.SetUpRecipes — 合成配方
///    Postfix 在配方加载后移除 result.id 为被封禁物品的配方
///
/// 被封禁的原版物品ID：
/// - 弹药：556round, 9mmround, 12gauge, boxof12gauge
/// - 武器：pistol, rifle, shotgun, makeshiftrifle
/// - 弹匣：smallmagazine, riflemagazine
/// - 头盔：bikehelmet, riothelmet
/// - 护甲/弹挂：traumarig
/// </summary>
public static class VanillaBlockPatch
{
    /// <summary>是否启用原版物品封禁。默认 true，可通过控制台 toggle</summary>
    internal static bool BlockEnabled = true;

    /// <summary>
    /// 标志位：当前帧正在由配方系统生成 HiddenFromLootPoolIds 物品。
    /// Item.Start 补丁检测到此标志为 true 时，不销毁隐藏物品。
    /// 仅设置一个很短的窗口（在同一帧的 Prefix → Postfix 之间），
    /// 避免误放行来自食物箱等非配方来源的隐藏物品。
    /// </summary>
    internal static bool IsCraftingHiddenItem = false;

    /// <summary>被封禁的原版物品ID集合</summary>
    internal static readonly HashSet<string> BlockedVanillaIds = new(StringComparer.OrdinalIgnoreCase)
    {
        // 弹药
        "556round",
        "9mmround",
        "12gauge",
        "boxof12gauge",
        // 武器
        "pistol",
        "rifle",
        "shotgun",
        "makeshiftrifle",
        // 弹匣
        "smallmagazine",
        "riflemagazine",
        // 头盔
        "bikehelmet",
        "riothelmet",
        // 护甲/弹挂
        "traumarig",
    };

    /// <summary>
    /// 自定义物品ID集合：从战利池和商人库存中隐藏（不影响 Utils.Create 和 Item.Start）。
    /// 用于不应在世界生成或商人交易界面出现的自定义物品。
    /// </summary>
    internal static readonly HashSet<string> HiddenFromLootPoolIds = new(StringComparer.OrdinalIgnoreCase)
    {
        Pvs31aItemSystem.ItemKey,
        VSSItemSystem.ItemKey,
        CookedNoodlesItemSystem.ItemKey, // 仅合成获取
        "duffelbag",
        "smallpack",
        "bigpack",
    };

    /// <summary>食物物品ID集合：使用原版战利池生成，不隐藏</summary>
    internal static readonly HashSet<string> FoodItemIds = new(StringComparer.OrdinalIgnoreCase)
    {
        CrackersItemSystem.ItemKey,
        CroutonsItemSystem.ItemKey,
        SlickersItemSystem.ItemKey,
        TarkerItemSystem.ItemKey,
        AlyonkaItemSystem.ItemKey,
        SugarItemSystem.ItemKey,
        IskraItemSystem.ItemKey,
        MreItemSystem.ItemKey,
        PeasItemSystem.ItemKey,
        NoodlesItemSystem.ItemKey,
    };

    /// <summary>判断物品ID是否被封禁（完全阻止创建）</summary>
    public static bool IsBlocked(string itemId) => BlockedVanillaIds.Contains(itemId);

    /// <summary>判断物品ID是否应从战利池/商人库存中隐藏</summary>
    public static bool IsHiddenFromLoot(string itemId)
        => (BlockedVanillaIds.Contains(itemId)
           || HiddenFromLootPoolIds.Contains(itemId)
           || WeaponItemRegistration.WeaponItemIds.Contains(itemId))
           && !FoodItemIds.Contains(itemId); // 食物使用原版战利池

    // === 1. 物品战利池拦截 ===

    [HarmonyPatch(typeof(ItemLootPool), nameof(ItemLootPool.InitializePool))]
    public static class ItemLootPoolPatch
    {
        private static System.Reflection.FieldInfo _cachedPoolField;
        private static bool _fieldSearchDone;

        [HarmonyPostfix]
        public static void Postfix()
        {
            if (!BlockEnabled) return;
            try
            {
                // ItemLootPool.pool 是 static Dictionary<string, List<string>>
                // key = category, value = 该类别下所有物品ID列表
                var pool = GetPoolDictionary();
                if (pool == null)
                {
                    Plugin.Log.LogWarning("[VanillaBlock] Could not access ItemLootPool pool dictionary.");
                    return;
                }

                int removedTotal = 0;
                foreach (var categoryList in pool.Values)
                {
                    removedTotal += categoryList.RemoveAll(id => IsHiddenFromLoot(id));
                }

                Plugin.Log.LogInfo($"[VanillaBlock] Removed {removedTotal} blocked/hidden items from ItemLootPool.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[VanillaBlock] ItemLootPool patch failed: {ex}");
            }
        }

        /// <summary>
        /// 获取 ItemLootPool 的战利池字典。
        /// 先尝试 "pool" 字段名，失败则按类型搜索所有静态字段。
        /// </summary>
        private static Dictionary<string, List<string>> GetPoolDictionary()
        {
            if (_cachedPoolField != null)
                return _cachedPoolField.GetValue(null) as Dictionary<string, List<string>>;

            if (_fieldSearchDone)
                return null;
            _fieldSearchDone = true;

            // 1. 尝试已知字段名
            var field = AccessTools.Field(typeof(ItemLootPool), "pool");
            if (field != null)
            {
                var dict = field.GetValue(null) as Dictionary<string, List<string>>;
                if (dict != null)
                {
                    _cachedPoolField = field;
                    return dict;
                }
            }

            // 2. 按类型搜索所有静态字段
            var targetType = typeof(Dictionary<string, List<string>>);
            foreach (var f in typeof(ItemLootPool).GetFields(
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic))
            {
                if (f.FieldType == targetType || f.FieldType.IsAssignableFrom(targetType))
                {
                    var dict = f.GetValue(null) as Dictionary<string, List<string>>;
                    if (dict != null)
                    {
                        _cachedPoolField = f;
                        Plugin.Log.LogInfo($"[VanillaBlock] Found ItemLootPool pool via type search: field '{f.Name}'.");
                        return dict;
                    }
                }
            }

            // 3. 列出所有字段名用于调试
            var allFields = string.Join(", ",
                typeof(ItemLootPool).GetFields(
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic)
                .Select(f => $"{f.Name}({f.FieldType.Name})"));
            Plugin.Log.LogWarning($"[VanillaBlock] Could not find pool dictionary in ItemLootPool. Fields: {allFields}");

            return null;
        }
    }

    // === 2. 商人库存拦截 ===

    /// <summary>根据物品 ItemInfo 返回细粒度类型（用于商人每类限量）</summary>
    private static string GetLootType(string itemId)
    {
        if (!Item.GlobalItems.TryGetValue(itemId, out var info)) return "unknown";
        if (!string.IsNullOrEmpty(info.wearSlotId))
        {
            switch (info.wearSlotId)
            {
                case "hat": return "helmet";
                case "eyes": return "nvg";
                case "ear": return "headset";
                case "back": return "backpack";
                case "outertorso": return "armor";
                case "bandolier": return "rig";
            }
        }
        if (!string.IsNullOrEmpty(info.tags))
        {
            if (info.tags.Contains("gun")) return "gun";
            if (info.tags.Contains("cutting") || info.tags.Contains("hammering") || info.tags.Contains("tool")) return "melee";
            if (info.tags.Contains("combine")) return "repairkit";
        }
        return "other";
    }

    [HarmonyPatch(typeof(TraderScript), nameof(TraderScript.GenerateInventory))]
    public static class TraderInventoryPatch
    {
        [HarmonyPostfix]
        public static void Postfix(TraderScript __instance)
        {
            if (!BlockEnabled) return;
            try
            {
                var itemsField = AccessTools.Field(typeof(TraderScript), "items");
                if (itemsField == null) return;

                var items = itemsField.GetValue(__instance) as List<TraderItem>;
                if (items == null) return;

                var idField = AccessTools.Field(typeof(TraderItem), "id");
                if (idField == null) return;

                var seenTypes = new HashSet<string>();
                var toRemove = new List<int>();
                int removed = 0;

                for (int i = 0; i < items.Count; i++)
                {
                    var traderItem = items[i];
                    var itemId = idField.GetValue(traderItem) as string;
                    if (itemId == null) continue;

                    // 封禁的原版物品：移除
                    if (BlockedVanillaIds.Contains(itemId) || HiddenFromLootPoolIds.Contains(itemId))
                    {
                        toRemove.Add(i);
                        removed++;
                        continue;
                    }

                    // 自定义物品（非食物）：每类最多 1 个
                    if (WeaponItemRegistration.WeaponItemIds.Contains(itemId) && !FoodItemIds.Contains(itemId))
                    {
                        var lootType = GetLootType(itemId);
                        if (seenTypes.Contains(lootType))
                        {
                            toRemove.Add(i);
                            removed++;
                        }
                        else
                        {
                            seenTypes.Add(lootType);
                        }
                    }
                }

                // 从后往前删除以保持索引正确
                for (int j = toRemove.Count - 1; j >= 0; j--)
                    items.RemoveAt(toRemove[j]);

                if (removed > 0)
                    Plugin.Log.LogInfo($"[VanillaBlock] Trader {__instance.character}: removed {removed} items (blocked + duplicate types). Kept types: {string.Join(", ", seenTypes)}.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[VanillaBlock] TraderInventory patch failed: {ex}");
            }
        }
    }

    // Note: Recipe blocking is handled inside RecipePatch.Postfix (same SetUpRecipes target),
    // to ensure correct execution order (block first, then add custom recipes, then re-index).

    // === 3. Utils.Create 拦截（安全网） ===
    // Utils.Create(string id, Vector2 pos, float rot) 通过 Resources.Load 加载预制体并实例化
    // 拦截被封禁的 ID，直接返回 null 不创建

    [HarmonyPatch(typeof(Utils), "Create", typeof(string), typeof(Vector2), typeof(float))]
    public static class UtilsCreateBlockPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(string id, ref GameObject __result)
        {
            if (!BlockEnabled) return true;
            if (id != null && IsBlocked(id))
            {
                Plugin.Log.LogInfo($"[VanillaBlock] Blocked Utils.Create for '{id}'.");
                __result = null!;
                return false;
            }
            return true;
        }
    }

    // === 4. 控制台命令: spawn vanilla_on / spawn vanilla_off ===
    // 也支持直接输入 vanilla_on / vanilla_off

    [HarmonyPatch(typeof(ConsoleScript), nameof(ConsoleScript.TryExecuteCommand))]
    public static class VanillaSpawnCommandPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ConsoleScript __instance, string[] args, bool addToLog)
        {
            if (args == null || args.Length < 1) return true;

            bool? enable = null;
            // 格式1: spawn vanilla_on / spawn vanilla_off
            if (args.Length >= 2 && args[0].Equals("spawn", StringComparison.OrdinalIgnoreCase))
            {
                if (args[1].Equals("vanilla_on", StringComparison.OrdinalIgnoreCase))
                    enable = true;
                else if (args[1].Equals("vanilla_off", StringComparison.OrdinalIgnoreCase))
                    enable = false;
            }
            // 格式2: vanilla_on / vanilla_off (直接输入)
            if (enable == null)
            {
                if (args[0].Equals("vanilla_on", StringComparison.OrdinalIgnoreCase))
                    enable = true;
                else if (args[0].Equals("vanilla_off", StringComparison.OrdinalIgnoreCase))
                    enable = false;
            }

            if (enable == null) return true;

            VanillaBlockPatch.BlockEnabled = !enable.Value;

            var logMethod = AccessTools.Method(typeof(ConsoleScript), "LogToConsole");
            string msg = enable.Value
                ? "[WeaponMod] Vanilla weapon/ammo/mag/helmet spawn, crafting and trading ENABLED."
                : "[WeaponMod] Vanilla weapon/ammo/mag/helmet spawn, crafting and trading DISABLED.";
            Plugin.Log.LogInfo(msg);
            logMethod?.Invoke(__instance, new object[] { msg });

            return false;
        }
    }

    // === 5. Item.Start 拦截（终极防线） ===
    // GenerateCollapsedPods/GenerateLifePods 实例化的预制体（如 LifepodCollapsed）包含子物体
    // 这些子物体带有 Item 组件，id 字段在预制体中已序列化
    // Item.Start 在物品完全初始化后被调用，此时可以安全销毁被封禁的物品
    // Item.Start 会将物品加入 Item.allItems 静态列表，销毁前需先移除

    [HarmonyPatch(typeof(Item), "Start")]
    public static class ItemStartBlockPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Item __instance)
        {
            if (!BlockEnabled) return;
            try
            {
                if (string.IsNullOrEmpty(__instance.id)) return;

                // BlockedVanillaIds：始终销毁（原版武器/弹药/弹匣不应存在）
                if (IsBlocked(__instance.id))
                {
                    RemoveFromAllItems(__instance);
                    Plugin.Log.LogInfo($"[VanillaBlock] Destroyed blocked item '{__instance.id}' spawned in world (likely prefab child).");
                    UnityEngine.Object.Destroy(__instance.gameObject);
                    return;
                }

                // HiddenFromLootPoolIds：始终销毁（合成获取的物品如 cookednoodles 应仅通过配方获取）。
                // 但配方系统在同一帧设置 IsCraftingHiddenItem=true，防止误杀配方产出。
                if (HiddenFromLootPoolIds.Contains(__instance.id) && !IsCraftingHiddenItem)
                {
                    RemoveFromAllItems(__instance);
                    Plugin.Log.LogInfo($"[VanillaBlock] Destroyed hidden item '{__instance.id}' (non-recipe spawn).");
                    UnityEngine.Object.Destroy(__instance.gameObject);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[VanillaBlock] Item.Start block patch failed: {ex}");
            }
        }

        private static void RemoveFromAllItems(Item item)
        {
            var allItemsField = AccessTools.Field(typeof(Item), "allItems");
            if (allItemsField != null)
            {
                var allItems = allItemsField.GetValue(null) as List<Item>;
                allItems?.Remove(item);
            }
        }
    }
}
