using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using CUTarkovMedicalMod.Framework;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 自定义枪械和弹匣的世界生成补丁。
///
/// 生成规则（触发后从模组列表随机选一个）：
/// - 物资箱（Container）: 6.6% 枪械 + 10% 弹匣
/// - 空投舱（LifePod）: 3% 枪械（通过 GenerateLifePods 期间的 Container.Awake）
/// - 尸体（CorpseScript）: 3% 枪械 + 3% 弹匣
/// - 崩溃舱（CollapsedPod）: 50% 弹匣（通过 Item.Start 替换被 VanillaBlockPatch 销毁的原版弹匣）
/// 世界生成的弹匣内子弹在 0~满 之间随机；合成出来的弹匣内没有子弹（见 RecipeSpawnPatch）。
/// </summary>
public static class CustomSpawnPatch
{
    // === 枪械分类列表（用于加权随机） ===
    private static readonly string[] PistolIds  = { DeagleItemSystem.ItemKey, Glock17ItemSystem.ItemKey, USPItemSystem.ItemKey };
    private static readonly string[] SksShotgunIds = { SKSItemSystem.ItemKey, MP133ItemSystem.ItemKey, MP153ItemSystem.ItemKey };
    private static readonly string[] SmgIds     = { P90ItemSystem.ItemKey, UMP45ItemSystem.ItemKey };
    private static readonly string[] RifleIds   = { AKMItemSystem.ItemKey, M4A1ItemSystem.ItemKey };
    private static readonly string[] SniperIds  = { AXMCItemSystem.ItemKey, DVL10ItemSystem.ItemKey };
    private static readonly string[] LmgIds     = { RPDItemSystem.ItemKey };

    // 保留旧数组兼容性（供外部诊断统计用）
    internal static readonly string[] CustomGunIds = ConcatGunIds();

    private static string[] ConcatGunIds()
    {
        var list = new List<string>();
        list.AddRange(PistolIds);
        list.AddRange(SksShotgunIds);
        list.AddRange(SmgIds);
        list.AddRange(RifleIds);
        list.AddRange(SniperIds);
        list.AddRange(LmgIds);
        return list.ToArray();
    }

    // === 近战武器列表（物资箱专属） ===
    private static readonly string[] MeleeIds = { RedRebelItemSystem.ItemKey, M2SwordItemSystem.ItemKey };

    /// <summary>加权随机选一把枪械 ID</summary>
    private static string GetRandomGunId()
    {
        float roll = UnityEngine.Random.Range(0f, 100f);
        if (roll < 35f) // 35% 手枪
            return PistolIds[UnityEngine.Random.Range(0, PistolIds.Length)];
        roll -= 35f;
        if (roll < 20f) // 20% SKS + 霰弹枪
            return SksShotgunIds[UnityEngine.Random.Range(0, SksShotgunIds.Length)];
        roll -= 20f;
        if (roll < 17f) // 17% 冲锋枪
            return SmgIds[UnityEngine.Random.Range(0, SmgIds.Length)];
        roll -= 17f;
        if (roll < 13f) // 13% 步枪
            return RifleIds[UnityEngine.Random.Range(0, RifleIds.Length)];
        roll -= 13f;
        if (roll < 10f) // 10% 狙击枪
            return SniperIds[UnityEngine.Random.Range(0, SniperIds.Length)];
        // 5% 轻机枪
        return LmgIds[UnityEngine.Random.Range(0, LmgIds.Length)];
    }

    /// <summary>加权随机选一把近战武器 ID（40%冰镐 60%M2）</summary>
    private static string GetRandomMeleeId()
    {
        return UnityEngine.Random.Range(0f, 1f) < 0.4f ? MeleeIds[0] : MeleeIds[1];
    }

    // === 自定义弹匣 ID 列表 ===
    internal static readonly string[] CustomMagIds =
    {
        AXMCMagItemSystem.ItemKey,    // axmc_mag
        DVL10MagItemSystem.ItemKey,   // dvl10_mag
        AKMMagItemSystem.ItemKey,     // akm_mag
        DeagleMagItemSystem.ItemKey,  // deagle_mag
        Glock17MagItemSystem.ItemKey, // glock17_mag
        M4A1MagItemSystem.ItemKey,    // m4a1_mag
        P90MagItemSystem.ItemKey,     // p90_mag
        UMP45MagItemSystem.ItemKey,   // ump45_mag
        RPDMagItemSystem.ItemKey,     // rpd_mag
    };

    // === 生成状态标志 ===
    /// <summary>正在生成空投舱（LifePod）期间为 true</summary>
    internal static bool InLifePodGen = false;

    // 已处理的 Container 实例 ID（避免重复生成）
    private static readonly HashSet<int> _processedContainers = new();

    // === 诊断计数器 ===
    internal static int ContainerCalls = 0;
    internal static int ContainerSkippedDup = 0;
    internal static int GunAttempts = 0;
    internal static int GunSpawned = 0;
    internal static int MagAttempts = 0;
    internal static int MagSpawned = 0;
    internal static int CorpseCalls = 0;
    internal static int CorpseSkippedAnimal = 0;
    internal static int ItemStartCalls = 0;
    internal static int ItemStartMagReplaced = 0;

    // === 生成辅助方法 ===

    /// <summary>
    /// 在指定位置生成一个自定义物品。
    /// 流程：Resources.Load(基础预制体) -> Instantiate -> ConfigureCustomItem
    /// </summary>
    internal static void SpawnCustomItemAt(string itemKey, Vector2 pos)
    {
        try
        {
            if (!ConsoleSpawnPatch.CustomItemPrefabs.TryGetValue(itemKey, out var prefabName))
            {
                Plugin.Log.LogWarning($"[CustomSpawn] No prefab mapping for '{itemKey}'.");
                return;
            }

            var prefab = Resources.Load<GameObject>(prefabName);
            if (prefab == null)
            {
                Plugin.Log.LogWarning($"[CustomSpawn] Prefab '{prefabName}' not found for '{itemKey}'.");
                return;
            }

            // 随机偏移：X ±1.5，Y +1~+3（从上方掉落，避免卡入地面/箱子内部）
            var offset = new Vector2(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(1f, 3f));

            var go = UnityEngine.Object.Instantiate(prefab, pos + offset, Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));
            var item = go.GetComponent<Item>();
            if (item == null)
            {
                UnityEngine.Object.Destroy(go);
                return;
            }

            // 配置自定义物品（设置 id、GunScript/AmmoScript 字段、图标等）
            var request = new MedicalGrantRequest(itemKey, itemKey, 1, "WorldSpawn", prefabName);
            ConsoleSpawnPatch.ConfigureCustomItem(item, request);

            // 世界生成的弹匣：子弹在 0~满 之间随机
            var ammo = item.GetComponent<AmmoScript>();
            if (ammo != null && ammo.itemType == AmmoScript.AmmoItemType.Magazine)
            {
                ammo.rounds = UnityEngine.Random.Range(0, ammo.maxRounds + 1);
                Plugin.Log.LogInfo($"[CustomSpawn] Magazine '{itemKey}' ammo randomized to {ammo.rounds}/{ammo.maxRounds}.");
            }

            // FreshItemDrop：让物品正确落入世界（物理、可见性、拾取注册）
            go.AddComponent<FreshItemDrop>();

            Plugin.Log.LogInfo($"[CustomSpawn] Spawned '{itemKey}' at {pos + offset}.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[CustomSpawn] Failed to spawn '{itemKey}': {ex}");
        }
    }

    /// <summary>以指定概率在指定位置生成一把加权随机枪械</summary>
    internal static void TrySpawnRandomGun(Vector2 pos, float chance)
    {
        GunAttempts++;
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        GunSpawned++;
        var gunId = GetRandomGunId();
        SpawnCustomItemAt(gunId, pos);
    }

    /// <summary>以指定概率在指定位置生成一把随机近战武器（40%冰镐 60%M2）</summary>
    internal static void TrySpawnRandomMelee(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        var meleeId = GetRandomMeleeId();
        SpawnCustomItemAt(meleeId, pos);
    }

    /// <summary>以指定概率在指定位置生成一个随机自定义弹匣（子弹0~满随机）</summary>
    internal static void TrySpawnRandomMag(Vector2 pos, float chance)
    {
        MagAttempts++;
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        MagSpawned++;
        var magId = CustomMagIds[UnityEngine.Random.Range(0, CustomMagIds.Length)];
        SpawnCustomItemAt(magId, pos);
    }

    // === 补丁 ===

    // 1. GenerateLifePods 标志（空投舱期间）
    // GenerateLifePods 是 Void(Single amt)，被 GenerateWorld 协程同步调用
    // 期间实例化的 lifepodchest（Container）的 Awake 会在此时触发
    [HarmonyPatch(typeof(WorldGeneration), nameof(WorldGeneration.GenerateLifePods))]
    public static class GenerateLifePodsSpawnPatch
    {
        [HarmonyPrefix]
        public static void Prefix() => InLifePodGen = true;

        [HarmonyPostfix]
        public static void Postfix() => InLifePodGen = false;
    }

    // 2. Container.Awake - 物资箱 / 空投舱箱子
    // Container.Awake 在 Instantiate 时同步调用
    // 使用 HashSet 去重（didAwake 标志在 Postfix 时已为 true，无法区分首次）
    [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
    public static class ContainerAwakeSpawnPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Container __instance)
        {
            try
            {
                int instanceId = __instance.GetInstanceID();
                if (_processedContainers.Contains(instanceId))
                {
                    ContainerSkippedDup++;
                    return;
                }
                _processedContainers.Add(instanceId);
                ContainerCalls++;

                Plugin.Log.LogInfo($"[CustomSpawn] Container.Awake #{ContainerCalls} id={instanceId} pos={__instance.transform.position} lifePod={InLifePodGen}");

                var pos = (Vector2)__instance.transform.position;

                if (InLifePodGen)
                {
                    // 空投舱: 3% 枪械
                    TrySpawnRandomGun(pos, 0.03f);
                }
                else
                {
                    // 物资箱: 6.6% 枪械 + 10% 弹匣 + 13% 近战武器（40%冰镐 60%M2）
                    TrySpawnRandomGun(pos, 0.066f);
                    TrySpawnRandomMag(pos, 0.10f);
                    TrySpawnRandomMelee(pos, 0.13f);
                }

                Plugin.Log.LogInfo($"[CustomSpawn] Stats so far: Container={ContainerCalls}(dup={ContainerSkippedDup}) Gun={GunSpawned}/{GunAttempts} Mag={MagSpawned}/{MagAttempts} Corpse={CorpseCalls}(animal={CorpseSkippedAnimal}) ItemStart={ItemStartCalls}(mag={ItemStartMagReplaced})");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[CustomSpawn] Container.Awake patch failed: {ex}");
            }
        }
    }

    // 3. CorpseScript.Start - 尸体旁
    // 原版 CorpseScript.Start 会从 ItemLootPool 随机生成 1-4 个物品
    // animalCorpse=true 时原版跳过战利品生成，我们也跳过
    [HarmonyPatch(typeof(CorpseScript), nameof(CorpseScript.Start))]
    public static class CorpseScriptStartSpawnPatch
    {
        [HarmonyPostfix]
        public static void Postfix(CorpseScript __instance)
        {
            try
            {
                CorpseCalls++;
                if (__instance.animalCorpse)
                {
                    CorpseSkippedAnimal++;
                    return;
                }

                Plugin.Log.LogInfo($"[CustomSpawn] CorpseScript.Start #{CorpseCalls} pos={__instance.transform.position} animal={__instance.animalCorpse}");

                var pos = (Vector2)__instance.transform.position;
                // 尸体旁: 3% 枪械 + 3% 弹匣
                TrySpawnRandomGun(pos, 0.03f);
                TrySpawnRandomMag(pos, 0.03f);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[CustomSpawn] CorpseScript.Start patch failed: {ex}");
            }
        }
    }

    // 4. Item.Start - 替换被销毁的原版弹匣（崩溃舱）
    // VanillaBlockPatch.ItemStartBlockPatch.Postfix 会销毁被封禁的原版弹匣
    // 此 Postfix 注册在同一方法上；由于 Object.Destroy 是延迟执行，物品仍可访问
    // 崩溃舱（LifepodCollapsed）预制体的弹匣子物体不经过 ItemLootPool/Utils.Create，
    // 只能通过 Item.Start 拦截
    [HarmonyPatch(typeof(Item), nameof(Item.Start))]
    public static class ItemStartMagReplacePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Item __instance)
        {
            try
            {
                ItemStartCalls++;

                if (string.IsNullOrEmpty(__instance.id)) return;

                // 只处理被封禁的弹匣
                if (!VanillaBlockPatch.IsBlocked(__instance.id)) return;

                bool isMag = __instance.id.Equals("riflemagazine", StringComparison.OrdinalIgnoreCase) ||
                             __instance.id.Equals("smallmagazine", StringComparison.OrdinalIgnoreCase);
                if (!isMag) return;

                Plugin.Log.LogInfo($"[CustomSpawn] Item.Start mag check id='{__instance.id}' pos={__instance.transform.position}");

                // 跳过容器内的弹匣（容器的 Awake 补丁会处理生成）
                var parent = __instance.transform.parent;
                while (parent != null)
                {
                    if (parent.GetComponent<Container>() != null)
                        return;
                    parent = parent.parent;
                }

                ItemStartMagReplaced++;
                // 世界中的弹匣（通常是崩溃舱 LifepodCollapsed 的子物体）
                // 50% 生成自定义弹匣替代
                var pos = (Vector2)__instance.transform.position;
                TrySpawnRandomMag(pos, 0.50f);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[CustomSpawn] Item.Start mag replace patch failed: {ex}");
            }
        }
    }
}
