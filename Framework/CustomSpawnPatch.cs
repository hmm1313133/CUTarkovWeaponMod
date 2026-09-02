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
/// - 物资箱（Container）: 5% 枪械 + 10% 弹匣 + 10% 近战 + 5% 护甲/胸挂 + 5% 头盔 + 3% 背包(1~2个) + 2% 夜视仪 + 4.5% Blue Area钥匙卡 + 7% 武器室钥匙卡
/// - 空投舱（LifePod）: 20% 枪械 + 25% 弹挂 + 20% 头盔 + 6% 背包 + 8% 夜视仪（通过 GenerateLifePods 期间的 Container.Awake）
/// - 空投胶囊（DropCapsule）: 29% 枪械 + 32% 弹挂类 + 17% 头盔(1~2个) + 16% 背包 + 10% 夜视仪（通过 GenerateDropCapsules 期间的 Container.Awake）
/// - 医疗箱（medcrate）: 20% 护甲（BuildingEntity 被破坏时触发）
/// - 尸体（CorpseScript）: 1.5% 枪械 + 5% 弹匣 + 3% 护甲/弹挂 + 2% 头盔 + 3% 背包 + 2.8% 武器室钥匙卡（每次 roll，共 2 次）
/// - 崩溃舱（CollapsedPod）: 62% 弹匣（通过 Item.Start 替换被 VanillaBlockPatch 销毁的原版弹匣）
/// 世界生成的弹匣内子弹在 0~满 之间随机；合成出来的弹匣内没有子弹（见 RecipeSpawnPatch）。
///
/// 护甲/胸挂刷新规则（触发后按分类+价值加权随机选一件）：
/// - 40% 弹挂类（bandolier 槽位，无防护）
/// - 35% 防弹衣类（outertorso 槽位，有防护无容器）
/// - 25% 弹挂甲类（outertorso+bandolier 双槽位，有防护有容器）
/// 每类内按价值反比加权（低价值=高权重，权重 = max+1-value）。
///
/// 头盔刷新规则（触发后按价值反比加权随机选一件）：
/// 每件内按价值反比加权（低价值=高权重，权重 = sqrt(max+1-value) 取整）。
///
/// KrokMP 多人联机兼容：
/// 使用 KrokMpHelper.ShouldSpawnLoot 门控，多人模式仅主机执行世界生成，
/// 客户端通过 KrokMP 同步接收物品（与医疗模组 MedicalWorldLootHooks 对齐）。
/// </summary>
public static class CustomSpawnPatch
{
    // === 枪械分类列表（用于加权随机） ===
    private static readonly string[] PistolIds  = { DeagleItemSystem.ItemKey, Glock17ItemSystem.ItemKey, USPItemSystem.ItemKey };
    private static readonly string[] SksShotgunIds = { SKSItemSystem.ItemKey, MP133ItemSystem.ItemKey, MP153ItemSystem.ItemKey, AA12ItemSystem.ItemKey };
    private static readonly string[] SmgIds     = { P90ItemSystem.ItemKey, UMP45ItemSystem.ItemKey };
    private static readonly string[] RifleIds   = { AKMItemSystem.ItemKey, M4A1ItemSystem.ItemKey };
    private static readonly string[] SniperIds  = { AXMCItemSystem.ItemKey, DVL10ItemSystem.ItemKey };
    private static readonly string[] LmgIds     = { RPDItemSystem.ItemKey };

    // === 使用可拆卸弹匣的枪械（世界生成时自动插入弹匣） ===
    private static readonly HashSet<string> MagazineFedGuns = new(StringComparer.OrdinalIgnoreCase)
    {
        AXMCItemSystem.ItemKey, DVL10ItemSystem.ItemKey, AKMItemSystem.ItemKey,
        DeagleItemSystem.ItemKey, Glock17ItemSystem.ItemKey, M4A1ItemSystem.ItemKey,
        P90ItemSystem.ItemKey, UMP45ItemSystem.ItemKey, RPDItemSystem.ItemKey,
        USPItemSystem.ItemKey, VSSItemSystem.ItemKey,
        AA12ItemSystem.ItemKey,
    };

    // === 近战武器列表（物资箱专属） ===
    private static readonly string[] MeleeIds = { RedRebelItemSystem.ItemKey, M2SwordItemSystem.ItemKey };

    /// <summary>加权随机选一把枪械 ID（用户指定权重）</summary>
    private static readonly (string id, int weight)[] GunSpawnTable =
    {
        (Glock17ItemSystem.ItemKey, 10),
        (USPItemSystem.ItemKey, 10),
        (MP133ItemSystem.ItemKey, 8),
        (SKSItemSystem.ItemKey, 7),
        (UMP45ItemSystem.ItemKey, 7),
        (DeagleItemSystem.ItemKey, 7),
        (MP153ItemSystem.ItemKey, 6),
        (DVL10ItemSystem.ItemKey, 5),
        (P90ItemSystem.ItemKey, 4),
        (AKMItemSystem.ItemKey, 3),
        (M4A1ItemSystem.ItemKey, 3),
        (RPDItemSystem.ItemKey, 2),
        (AXMCItemSystem.ItemKey, 1),
        (AA12ItemSystem.ItemKey, 1),
    };
    private static readonly int GunTotalWeight = 74;

    private static string GetRandomGunId()
    {
        int w = UnityEngine.Random.Range(1, GunTotalWeight + 1);
        int accum = 0;
        foreach (var (id, weight) in GunSpawnTable)
        {
            accum += weight;
            if (w <= accum) return id;
        }
        return GunSpawnTable[GunSpawnTable.Length - 1].id;
    }

    /// <summary>加权随机选一把近战武器 ID（40%冰镐 60%M2）</summary>
    private static string GetRandomMeleeId()
    {
        return UnityEngine.Random.Range(0f, 1f) < 0.4f ? MeleeIds[0] : MeleeIds[1];
    }

    // === 背包分类列表（价值反比加权，低价值=高权重） ===
    private static readonly (string id, int weight)[] BackpackIds =
    {
        (LK3FItemSystem.ItemKey,                5), // val=15, cap=4.4
        (ReadyPackItemSystem.ItemKey,           4), // val=20, cap=4.8
        (ScavPackItemSystem.ItemKey,            4), // val=25, cap=4.8
        (DayPackItemSystem.ItemKey,             3), // val=30, cap=5.5
        (BerkutItemSystem.ItemKey,              3), // val=30, cap=5.0
        (MysteryRanch2DayItemSystem.ItemKey,    3), // val=35, cap=5.0
        (PartizanItemSystem.ItemKey,            2), // val=35, cap=5.5
        (PilgrimItemSystem.ItemKey,             2), // val=40, cap=7.0
        (SsoAttack2ItemSystem.ItemKey,          2), // val=42, cap=7.2
        (LBT2670ItemSystem.ItemKey,             1), // val=45, cap=14.0 (医疗专用)
        (SH118ItemSystem.ItemKey,               1), // val=50, cap=14.2
    };
    private static readonly int BackpackTotalWeight = SumWeights(BackpackIds);

    /// <summary>从背包列表中按价值反比加权随机选一件</summary>
    private static string GetRandomBackpackId()
    {
        int w = UnityEngine.Random.Range(1, BackpackTotalWeight + 1);
        int accum = 0;
        foreach (var (id, weight) in BackpackIds)
        {
            accum += weight;
            if (w <= accum) return id;
        }
        return BackpackIds[BackpackIds.Length - 1].id;
    }

    /// <summary>以指定概率在指定位置生成一件加权随机背包</summary>
    internal static void TrySpawnRandomBackpack(Vector2 pos, float chance)
    {
        BackpackAttempts++;
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        BackpackSpawned++;
        var backpackId = GetRandomBackpackId();
        SpawnCustomItemAt(backpackId, pos);
    }

    /// <summary>以指定概率在指定位置生成 minCount~maxCount 件加权随机背包</summary>
    internal static void TrySpawnRandomBackpackCount(Vector2 pos, float chance, int minCount, int maxCount)
    {
        BackpackAttempts++;
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        int count = UnityEngine.Random.Range(minCount, maxCount + 1);
        for (int i = 0; i < count; i++)
        {
            BackpackSpawned++;
            var backpackId = GetRandomBackpackId();
            SpawnCustomItemAt(backpackId, pos);
        }
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
        USPMagItemSystem.ItemKey,     // usp_mag
    };

    // === 护甲/胸挂分类列表（价值加权随机） ===
    // 弹挂类（bandolier 槽位，无防护）: 40%
    // 权重 = sqrt(max+1-value) 取整，低价值略多但不极端
    private static readonly (string id, int weight)[] RigIds =
    {
        (IDEAItemSystem.ItemKey,       5), // val=16
        (BankRobberItemSystem.ItemKey, 4), // val=24
        (Type56ItemSystem.ItemKey,     4), // val=27
        (WTChestRigItemSystem.ItemKey, 4), // val=27
        (UmkaItemSystem.ItemKey,       3), // val=31
        (CommandoItemSystem.ItemKey,   3), // val=34
        (LBCRItemSystem.ItemKey,       2), // val=36
        (BlackRockItemSystem.ItemKey,  1), // val=41
    };

    // 防弹衣类（outertorso 槽位，有防护无容器）: 35%
    // 权重 = sqrt(max+1-value) 取整，低价值略多但不极端
    private static readonly (string id, int weight)[] ArmorIds =
    {
        (PACAItemSystem.ItemKey,       8), // val=17
        (MFUNItemSystem.ItemKey,       7), // val=24
        (DRDItemSystem.ItemKey,        7), // val=24
        (ThorItemSystem.ItemKey,       6), // val=36
        (TrooperItemSystem.ItemKey,    6), // val=37
        (SixB13ItemSystem.ItemKey,     6), // val=44
        (HPCItemSystem.ItemKey,        5), // val=50
        (GzhelKItemSystem.ItemKey,     5), // val=53
        (HGridItemSystem.ItemKey,      4), // val=64
        (SlickItemSystem.ItemKey,      3), // val=66
        (RedutT5ItemSystem.ItemKey,    3), // val=67
        (SixB43ItemSystem.ItemKey,     2), // val=75
    };

    // 弹挂甲类（outertorso+bandolier 双槽位，有防护有容器）: 25%
    // 权重 = sqrt(max+1-value) 取整，低价值略多但不极端
    private static readonly (string id, int weight)[] ArmoredRigIds =
    {
        (SixB516ItemSystem.ItemKey,    7), // val=26
        (MBSSItemSystem.ItemKey,       6), // val=35
        (TV115ItemSystem.ItemKey,      6), // val=37
        (SPPCV2ItemSystem.ItemKey,     6), // val=44
        (TV110ItemSystem.ItemKey,      5), // val=45
        (MK4AItemSystem.ItemKey,       5), // val=55
        (SixB45ItemSystem.ItemKey,     3), // val=64
        (SiegeRItemSystem.ItemKey,     3), // val=66
        (AVSTEItemSystem.ItemKey,      2), // val=69
        (TTSKItemSystem.ItemKey,       2), // val=70
        (LV119ItemSystem.ItemKey,      2), // val=74
    };

    // === 头盔分类列表（价值加权随机） ===
    // 权重 = sqrt(max+1-value) 取整，max=55(Rys-T)，低价值略多但不极端
    private static readonly (string id, int weight)[] HelmetIds =
    {
        (Ssh68ItemSystem.ItemKey,    8), // val=36 sqrt(20)≈4.47→4
        (B47ItemSystem.ItemKey,      6), // val=38 sqrt(18)≈4.24→4
        (CalmanItemSystem.ItemKey,   5), // val=40 sqrt(16)=4→4
        (ExfilItemSystem.ItemKey,    3), // val=46 sqrt(10)≈3.16→3
        (UlachItemSystem.ItemKey,    3), // val=48 sqrt(8)≈2.83→3
        (RysTItemSystem.ItemKey,     2), // val=55
        (FastMtItemSystem.ItemKey,   3), // val=44 sqrt(12)≈3.46->3
    };

    private static readonly int RigTotalWeight = SumWeights(RigIds);
    private static readonly int ArmorTotalWeight = SumWeights(ArmorIds);
    private static readonly int ArmoredRigTotalWeight = SumWeights(ArmoredRigIds);
    private static readonly int HelmetTotalWeight = SumWeights(HelmetIds);

    private static int SumWeights((string id, int weight)[] table)
    {
        int sum = 0;
        foreach (var (_, w) in table) sum += w;
        return sum;
    }

    /// <summary>
    /// 护甲/胸挂加权随机选择：
    /// 40% 弹挂类 / 35% 防弹衣类 / 25% 弹挂甲类，
    /// 每类内按价值反比加权（低价值=高权重）。
    /// </summary>
    private static string GetRandomArmorId()
    {
        float roll = UnityEngine.Random.Range(0f, 100f);
        (string id, int weight)[] table;
        int totalWeight;

        if (roll < 40f) // 40% 弹挂类
        {
            table = RigIds;
            totalWeight = RigTotalWeight;
        }
        else if (roll < 75f) // 35% 防弹衣类 (40~75)
        {
            table = ArmorIds;
            totalWeight = ArmorTotalWeight;
        }
        else // 25% 弹挂甲类 (75~100)
        {
            table = ArmoredRigIds;
            totalWeight = ArmoredRigTotalWeight;
        }

        int w = UnityEngine.Random.Range(1, totalWeight + 1);
        int accum = 0;
        foreach (var (id, weight) in table)
        {
            accum += weight;
            if (w <= accum) return id;
        }
        return table[table.Length - 1].id;
    }

    /// <summary>以指定概率在指定位置生成一件加权随机护甲/胸挂</summary>
    internal static void TrySpawnRandomArmor(Vector2 pos, float chance)
    {
        ArmorAttempts++;
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        ArmorSpawned++;
        var armorId = GetRandomArmorId();
        SpawnCustomItemAt(armorId, pos);
    }

    /// <summary>以指定概率在指定位置生成一件弹挂类（bandolier 槽位，无防护）</summary>
    internal static void TrySpawnRandomRig(Vector2 pos, float chance)
    {
        RigAttempts++;
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        RigSpawned++;
        var rigId = GetRandomRigId();
        SpawnCustomItemAt(rigId, pos);
    }

    /// <summary>从弹挂类（RigIds）中按价值反比加权随机选一件</summary>
    private static string GetRandomRigId()
    {
        int w = UnityEngine.Random.Range(1, RigTotalWeight + 1);
        int accum = 0;
        foreach (var (id, weight) in RigIds)
        {
            accum += weight;
            if (w <= accum) return id;
        }
        return RigIds[RigIds.Length - 1].id;
    }

    /// <summary>从头盔列表（HelmetIds）中按价值反比加权随机选一件</summary>
    private static string GetRandomHelmetId()
    {
        int w = UnityEngine.Random.Range(1, HelmetTotalWeight + 1);
        int accum = 0;
        foreach (var (id, weight) in HelmetIds)
        {
            accum += weight;
            if (w <= accum) return id;
        }
        return HelmetIds[HelmetIds.Length - 1].id;
    }

    /// <summary>以指定概率在指定位置生成一件加权随机头盔</summary>
    internal static void TrySpawnRandomHelmet(Vector2 pos, float chance)
    {
        HelmetAttempts++;
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        HelmetSpawned++;
        var helmetId = GetRandomHelmetId();
        SpawnCustomItemAt(helmetId, pos);
    }

    /// <summary>以指定概率在指定位置生成 minCount~maxCount 件加权随机头盔</summary>
    internal static void TrySpawnRandomHelmetCount(Vector2 pos, float chance, int minCount, int maxCount)
    {
        HelmetAttempts++;
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        int count = UnityEngine.Random.Range(minCount, maxCount + 1);
        for (int i = 0; i < count; i++)
        {
            HelmetSpawned++;
            var helmetId = GetRandomHelmetId();
            SpawnCustomItemAt(helmetId, pos);
        }
    }

    // === 夜视仪+面罩刷新池（PVS-14 41%, FAST护目罩 24%, GPNVG-18 18%, FAST面罩 17%） ===
    private static readonly (string id, int weight)[] NvgIds =
    {
        (Pvs14ItemSystem.ItemKey,     70),
        (FastVisorItemSystem.ItemKey,  40),
        (Gpnvg18ItemSystem.ItemKey,   30),
        (FastVisor2ItemSystem.ItemKey, 30),
    };
    private static readonly int NvgTotalWeight = 170;

    /// <summary>从刷新池中按权重随机选一件</summary>
    private static string GetRandomNvgId()
    {
        int w = UnityEngine.Random.Range(1, NvgTotalWeight + 1);
        int accum = 0;
        foreach (var (id, weight) in NvgIds)
        {
            accum += weight;
            if (w <= accum) return id;
        }
        return NvgIds[0].id;
    }

    /// <summary>以指定概率在指定位置生成一件加权随机夜视仪</summary>
    internal static void TrySpawnRandomNvg(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        var nvgId = GetRandomNvgId();
        SpawnCustomItemAt(nvgId, pos);
    }

    // === 生成状态标志 ===
    /// <summary>正在生成空投舱（LifePod）期间为 true</summary>
    internal static bool InLifePodGen = false;

    /// <summary>正在生成空投胶囊（DropCapsule）期间为 true</summary>
    internal static bool InDropCapsuleGen = false;

    /// <summary>CUCoreLib 模板预创建期间为 true，跳过世界生成逻辑</summary>
    internal static bool InTemplateSetup = false;

    // 已处理的 Container 实例 ID（避免重复生成）
    private static readonly HashSet<int> _processedContainers = new();

    // 已处理的 BuildingEntity（医疗箱）实例 ID（避免重复生成）
    private static readonly HashSet<int> _processedMedcrates = new();

    // 安全上限：超过后清空（实例 ID 只在世界生成期间使用，Awake 只会触发一次）
    private const int MaxProcessedInstanceTrack = 5000;

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
    internal static int ArmorAttempts = 0;
    internal static int ArmorSpawned = 0;
    internal static int RigAttempts = 0;
    internal static int RigSpawned = 0;
    internal static int MedcrateCalls = 0;
    internal static int MedcrateSkippedDup = 0;
    internal static int HelmetAttempts = 0;
    internal static int HelmetSpawned = 0;

    /// <summary>生成锁：防止自定义物品的 Container.Awake 触发链式生成</summary>
    internal static bool IsSpawning = false;
    internal static int BackpackAttempts = 0;
    internal static int BackpackSpawned = 0;

    // === 生成辅助方法 ===

    /// <summary>
    /// 在指定位置生成一个自定义物品。
    /// 流程：Resources.Load(基础预制体) -> Instantiate -> ConfigureCustomItem
    /// </summary>
    internal static void SpawnCustomItemAt(string itemKey, Vector2 pos)
    {
        IsSpawning = true;
        try
        {
            SpawnCustomItemAtInternal(itemKey, pos);
        }
        finally
        {
            IsSpawning = false;
        }
    }

    private static void SpawnCustomItemAtInternal(string itemKey, Vector2 pos)
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

            // 世界生成的弹匣供弹枪械：插入弹匣，弹匣内随机弹药
            var gun = item.GetComponent<GunScript>();
            if (gun != null && MagazineFedGuns.Contains(item.id))
            {
                gun.hasMag = true;
                gun.roundsInMag = UnityEngine.Random.Range(0, gun.magCapacity + 1);
                Plugin.Log.LogInfo($"[CustomSpawn] Gun '{itemKey}' spawned with magazine, rounds={gun.roundsInMag}/{gun.magCapacity}.");
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

    /// <summary>以指定概率在指定位置生成一个武器维修套件</summary>
    internal static void TrySpawnRepairKit(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        SpawnCustomItemAt(WeaponRepairKitItemSystem.ItemKey, pos);
    }

    /// <summary>以指定概率在指定位置生成 Blue Area 钥匙卡</summary>
    internal static void TrySpawnBlueAreaKeycard(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        SpawnCustomItemAt(BlueAreaKeycardItemSystem.ItemKey, pos);
    }

    /// <summary>以指定概率在指定位置生成武器室钥匙卡</summary>
    internal static void TrySpawnWeaponRoomKeycard(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        SpawnCustomItemAt(WeaponRoomKeycardItemSystem.ItemKey, pos);
    }

    /// <summary>以指定概率在指定位置生成一个 TEP-300 战术耳塞</summary>
    internal static void TrySpawnTep300(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        SpawnCustomItemAt(Tep300ItemSystem.ItemKey, pos);
    }

    /// <summary>以指定概率在指定位置生成一个 ProFlex DX5 战术耳塞</summary>
    internal static void TrySpawnProFlex(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        SpawnCustomItemAt(ProFlexItemSystem.ItemKey, pos);
    }

    /// <summary>以指定概率在指定位置生成一个防弹插板（70%普通/30%高级）</summary>
    internal static void TrySpawnArmorPlate(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        string key = UnityEngine.Random.Range(0f, 1f) < 0.7f
            ? ArmorPlateItemSystem.CheapPlateKey
            : ArmorPlateItemSystem.AdvancedPlateKey;
        SpawnCustomItemAt(key, pos);
    }

    /// <summary>以指定概率在指定位置生成军用饼干</summary>
    internal static void TrySpawnCrackers(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        SpawnCustomItemAt(CrackersItemSystem.ItemKey, pos);
    }

    /// <summary>以指定概率在指定位置生成黑麦面包块</summary>
    internal static void TrySpawnCroutons(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        SpawnCustomItemAt(CroutonsItemSystem.ItemKey, pos);
    }

    /// <summary>以指定概率在指定位置生成士力架能量棒</summary>
    internal static void TrySpawnSlickers(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        SpawnCustomItemAt(SlickersItemSystem.ItemKey, pos);
    }

    /// <summary>以指定概率在指定位置生成塔克肉干</summary>
    internal static void TrySpawnTarker(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        SpawnCustomItemAt(TarkerItemSystem.ItemKey, pos);
    }

    /// <summary>以指定概率在指定位置生成Alyonka巧克力棒</summary>
    internal static void TrySpawnAlyonka(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        SpawnCustomItemAt(AlyonkaItemSystem.ItemKey, pos);
    }

    /// <summary>以指定概率在指定位置生成一包糖</summary>
    internal static void TrySpawnSugar(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        SpawnCustomItemAt(SugarItemSystem.ItemKey, pos);
    }

    /// <summary>以指定概率在指定位置生成Iskra单兵口粮</summary>
    internal static void TrySpawnIskra(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        SpawnCustomItemAt(IskraItemSystem.ItemKey, pos);
    }

    /// <summary>以指定概率在指定位置生成MRE个人即食口粮</summary>
    internal static void TrySpawnMre(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        SpawnCustomItemAt(MreItemSystem.ItemKey, pos);
    }

    /// <summary>以指定概率在指定位置生成豌豆罐头</summary>
    internal static void TrySpawnPeas(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        SpawnCustomItemAt(PeasItemSystem.ItemKey, pos);
    }

    /// <summary>以指定概率在指定位置生成方便面</summary>
    internal static void TrySpawnNoodles(Vector2 pos, float chance)
    {
        if (UnityEngine.Random.Range(0f, 1f) > chance) return;
        SpawnCustomItemAt(NoodlesItemSystem.ItemKey, pos);
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

    // 1b. GenerateDropCapsules 标志（空投胶囊）
    // GenerateDropCapsules 是 Void(Single amt)，被 GenerateWorld 协程同步调用
    // 期间实例化的 dropcapsule 预制体的 Container.Awake 会在此时触发
    [HarmonyPatch(typeof(WorldGeneration), nameof(WorldGeneration.GenerateDropCapsules))]
    public static class GenerateDropCapsulesSpawnPatch
    {
        [HarmonyPrefix]
        public static void Prefix() => InDropCapsuleGen = true;

        [HarmonyPostfix]
        public static void Postfix() => InDropCapsuleGen = false;
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
            // 生成锁：跳过我们正在生成的物品的 Container.Awake（防止链式生成）
            if (IsSpawning) return;

            // 跳过物品容器（背包、注射器包、医疗包等有 Item 组件的容器）
            // 仅对世界容器（物资箱、空投舱等无 Item 组件的静态容器）生成物品
            if (__instance.GetComponent<Item>() != null) return;

            // 诊断：确认 patch 是否被调用
            if (ContainerCalls == 0 && ContainerSkippedDup == 0)
                Plugin.Log.LogInfo($"[CustomSpawn] Container.Awake FIRST CALL: name={__instance.gameObject.name} generatingWorld={WorldGeneration.world?.generatingWorld} inTemplate={InTemplateSetup} lifePod={InLifePodGen} dropCapsule={InDropCapsuleGen}");

            if (InTemplateSetup) return; // 模板预创建期间跳过
            if (!KrokMpHelper.ShouldSpawnLoot) return; // 多人模式仅主机生成

            // 跳过玩家身上的容器（背包等），仅对世界容器生成物品
            var playerBody = PlayerCamera.main?.body;
            if (playerBody != null)
            {
                var t = __instance.transform.parent;
                while (t != null)
                {
                    if (t == playerBody.transform) return;
                    t = t.parent;
                }
            }
            try
            {
                int instanceId = __instance.GetInstanceID();
                if (_processedContainers.Contains(instanceId))
                {
                    ContainerSkippedDup++;
                    return;
                }
                _processedContainers.Add(instanceId);
                if (_processedContainers.Count > MaxProcessedInstanceTrack) _processedContainers.Clear();
                ContainerCalls++;

                var pos = (Vector2)__instance.transform.position;
                Plugin.Log.LogInfo($"[CustomSpawn] Container.Awake #{ContainerCalls} pos={pos} lifePod={InLifePodGen} dropCapsule={InDropCapsuleGen} name={__instance.gameObject.name}");

                if (InLifePodGen)
                {
                    // 空投舱(LifePod): 20% 枪械 + 25% 弹挂 + 20% 头盔 + 6% 背包 + 8% 夜视仪
                    TrySpawnRandomGun(pos, 0.02f);
                    TrySpawnRandomRig(pos, 0.02f);
                    TrySpawnRandomHelmet(pos, 0.02f);
                    TrySpawnRandomBackpack(pos, 0.01f);
                    TrySpawnRandomNvg(pos, 0f);
                }
                else if (InDropCapsuleGen)
                {
                    // 空投胶囊(DropCapsule): 29% 枪械 + 32% 弹挂类 + 17% 头盔(1~2个) + 16% 背包 + 10% 夜视仪
                    TrySpawnRandomGun(pos, 0.03f);
                    TrySpawnRandomRig(pos, 0.07f);
                    TrySpawnRandomHelmetCount(pos, 0.10f, 1, 1);
                    TrySpawnRandomBackpack(pos, 0.06f);
                    TrySpawnRandomNvg(pos, 0.01f);
                }
                else
                {
                    // 物资箱: 5% 枪械 + 10% 弹匣 + 10% 近战武器 + 5% 护甲/胸挂 + 5% 头盔 + 3% 背包(1~2个) + 2% 夜视仪 + 4.5% Blue Area钥匙卡 + 7% 武器室钥匙卡
                    TrySpawnRandomGun(pos, 0.05f);
                    TrySpawnRandomMag(pos, 0.10f);
                    TrySpawnRandomMelee(pos, 0.10f);
                    TrySpawnRandomArmor(pos, 0.05f);
                    TrySpawnRandomHelmet(pos, 0.05f);
                    TrySpawnRandomBackpackCount(pos, 0.03f, 1, 2);
                    TrySpawnRandomNvg(pos, 0.02f);
                    TrySpawnBlueAreaKeycard(pos, 0.045f);
                    TrySpawnWeaponRoomKeycard(pos, 0.07f);
                    // 食物使用原版战利池生成，不在此处生成
                }

                Plugin.Log.LogInfo($"[CustomSpawn] Stats so far: Container={ContainerCalls}(dup={ContainerSkippedDup}) Gun={GunSpawned}/{GunAttempts} Mag={MagSpawned}/{MagAttempts} Armor={ArmorSpawned}/{ArmorAttempts} Rig={RigSpawned}/{RigAttempts} Helmet={HelmetSpawned}/{HelmetAttempts} Backpack={BackpackSpawned}/{BackpackAttempts} Corpse={CorpseCalls}(animal={CorpseSkippedAnimal}) ItemStart={ItemStartCalls}(mag={ItemStartMagReplaced}) Medcrate={MedcrateCalls}(dup={MedcrateSkippedDup})");
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
            if (!KrokMpHelper.ShouldSpawnLoot) return; // 多人模式仅主机生成
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
                // 尸体旁：最多刷 2 种物品，两次独立roll，第二次排除已刷类型
                var spawnedTypes = new System.Collections.Generic.HashSet<string>();
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    float roll = UnityEngine.Random.Range(0f, 1f);
                    string type = null;
                    if (roll < 0.015f) type = "gun";
                    else if (roll < 0.065f) type = "mag";
                    else if (roll < 0.095f) type = "armor";
                    else if (roll < 0.115f) type = "helmet";
                    else if (roll < 0.145f) type = "backpack";
                    else if (roll < 0.173f) type = "weaponroom_keycard"; // 武器室钥匙卡 2.8%

                    if (type == null || !spawnedTypes.Add(type)) continue;

                    switch (type)
                    {
                        case "gun": TrySpawnRandomGun(pos, 1f); break;
                        case "mag": TrySpawnRandomMag(pos, 1f); break;
                        case "armor": TrySpawnRandomArmor(pos, 1f); break;
                        case "helmet": TrySpawnRandomHelmet(pos, 1f); break;
                        case "backpack": TrySpawnRandomBackpack(pos, 1f); break;
                        case "weaponroom_keycard": TrySpawnWeaponRoomKeycard(pos, 1f); break;
                    }
                }
                // 53% 无物品, 37% 1件, 10% 2件
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
            if (InTemplateSetup) return; // 模板预创建期间跳过
            if (!KrokMpHelper.ShouldSpawnLoot) return; // 多人模式仅主机生成
            // 仅在世界生成期间触发，游戏过程中创建的物品不触发
            if (WorldGeneration.world == null || !WorldGeneration.world.generatingWorld) return;
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

                // 跳过离玩家过近的弹匣（防止物品生成在玩家脚边）
                var playerBody = PlayerCamera.main?.body;
                if (playerBody != null)
                {
                    float dist = Vector2.Distance(__instance.transform.position, playerBody.transform.position);
                    if (dist < 5f) return;
                }

                ItemStartMagReplaced++;
                // 世界中的弹匣（通常是崩溃舱 LifepodCollapsed 的子物体）
                // 62% 生成自定义弹匣替代
                var pos = (Vector2)__instance.transform.position;
                TrySpawnRandomMag(pos, 0.62f);
                TrySpawnRepairKit(pos, 0.01f);
                TrySpawnTep300(pos, 0.01f);
                TrySpawnProFlex(pos, 0.005f);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[CustomSpawn] Item.Start mag replace patch failed: {ex}");
            }
        }
    }

    // 5. BuildingEntity.Update - 医疗箱被破坏时生成护甲
    // 医疗箱（medcrate）是 BuildingEntity，被破坏时 health < 0.5
    // 与医疗模组的 MedcrateStimSpawner/WorldContainerLootSpawner 并行运行（Harmony 支持多 Postfix/Prefix）
    [HarmonyPatch(typeof(BuildingEntity), nameof(BuildingEntity.Update))]
    public static class MedcrateArmorSpawnPatch
    {
        [HarmonyPrefix]
        public static void Prefix(BuildingEntity __instance)
        {
            if (InTemplateSetup) return;
            if (!KrokMpHelper.ShouldSpawnLoot) return; // 多人模式仅主机生成
            try
            {
                if (__instance.health >= 0.5f) return; // 未破坏

                int instanceId = __instance.GetInstanceID();
                if (_processedMedcrates.Contains(instanceId))
                {
                    MedcrateSkippedDup++;
                    return;
                }
                _processedMedcrates.Add(instanceId);
                if (_processedMedcrates.Count > MaxProcessedInstanceTrack) _processedMedcrates.Clear();

                string goName = __instance.gameObject.name.ToLowerInvariant();
                string buildingId = (__instance.id ?? "").ToLowerInvariant();
                if (!goName.Contains("medcrate") && buildingId != "medcrate") return;

                MedcrateCalls++;
                Plugin.Log.LogInfo($"[CustomSpawn] Medcrate broken #{MedcrateCalls} id={instanceId} pos={__instance.transform.position}");

                var pos = (Vector2)__instance.transform.position;
                // 医疗箱: 20% 护甲/胸挂
                TrySpawnRandomArmor(pos, 0.20f);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[CustomSpawn] Medcrate armor spawn patch failed: {ex}");
            }
        }
    }
}
