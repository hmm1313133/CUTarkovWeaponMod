using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUCoreLib.Helpers;
using CUCoreLib.Registries;
using CUTarkovMedicalMod.Framework;
using Newtonsoft.Json;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 武器配件地堡 + 武器物资箱世界生成。
///
/// 地堡：35x12 实验室风格防弹加强钢建筑（bulletproof_steel），左侧刷卡进入，内部有充电站（rechargingstation）和武器物资箱。
/// 武器物资箱：可开锁（原版 LockpingMinigame 开锁小游戏），开锁成功后随机掉落 1~2 个武器改装配件。
///
/// 开锁链路（原版原生）：
/// 1. 玩家点击物资箱 → PlayerCamera 识别 UsableObject → SendMessage("OnUse")
/// 2. Openable.OnUse() → 检查背包 lockpickingkit + INT ≥ 10 → 启动 LockpingMinigame
/// 3. 开锁成功 → BuildingEntity.health = 0 → 建筑销毁 → WeaponCacheBoxDrop.OnDestroy 掉落配件
/// 4. 开锁失败 → 消耗开锁工具耐久或徒手受伤
/// </summary>
public static class WeaponCacheBunker
{
    /// <summary>武器物资箱建筑 ID</summary>
    public const string WeaponCacheBoxId = "weapon_cache_box";

    /// <summary>蓝白加固门建筑 ID</summary>
    public const string WeaponCacheDoorId = "weapon_cache_door";

    /// <summary>刷卡装置建筑 ID</summary>
    public const string CardReaderId = "weapon_cache_card_reader";

    /// <summary>大型武器箱建筑 ID</summary>
    public const string WeaponCacheBoxLargeId = "weapon_cache_box_large";

    /// <summary>可选小武器箱建筑 ID（50% 概率生成，用于每房随机 2~3 个小箱）</summary>
    public const string WeaponCacheBoxOptionalId = "weapon_cache_box_optional";

    /// <summary>大型武器箱贴地偏移（根据贴图实际宽高比计算，注册大箱时写入）。</summary>
    public static float LargeCrateFloorOffset = 1f;

    /// <summary>地堡结构 ID</summary>
    public const string BunkerId = "weapon_cache_bunker";

    /// <summary>开锁精度（与原版物资箱持平，越小越难）</summary>
    private const float LockpickAnglePrecision = 0.5f;

    /// <summary>
    /// 所有武器改装配件 ID（等权重掉落池）。
    /// 后续如需调整稀有度，可改为加权列表。
    /// </summary>
    public static readonly string[] WeaponPartIds =
    {
        // === 护木/导轨 ===
        MoeAkmItemSystem.ItemKey,
        HexagonAkHandguardItemSystem.ItemKey,
        B10mB19ItemSystem.ItemKey,
        WasrItemSystem.ItemKey,
        AkmLItemSystem.ItemKey,
        MoeSlItemSystem.ItemKey,
        ViperItemSystem.ItemKey,
        KacRisItemSystem.ItemKey,
        SmrMk16ItemSystem.ItemKey,
        AdarWoodItemSystem.ItemKey,
        LvoaItemSystem.ItemKey,
        HexagonSksItemSystem.ItemKey,
        TapcoIntrafuseItemSystem.ItemKey,
        UasSksItemSystem.ItemKey,
        SksMcItemSystem.ItemKey,
        Mtu017ItemSystem.ItemKey,
        // === 握把 ===
        Rk3ItemSystem.ItemKey,
        Mg47ItemSystem.ItemKey,
        Ags74ItemSystem.ItemKey,
        Td120001ItemSystem.ItemKey,
        StarkArrgItemSystem.ItemKey,
        MiadItemSystem.ItemKey,
        F1st2pcItemSystem.ItemKey,
        ErgoItemSystem.ItemKey,
        ShiftForegripItemSystem.ItemKey,
        Se5ForegripItemSystem.ItemKey,
        Rk0ForegripItemSystem.ItemKey,
        Rk2ForegripItemSystem.ItemKey,
        B25ur1ForegripItemSystem.ItemKey,
        CobraForegripItemSystem.ItemKey,
        P2ForegripItemSystem.ItemKey,
        AfgForegripItemSystem.ItemKey,
        AxmcGripItemSystem.ItemKey,
        // === 枪托 ===
        OpforAak7ItemSystem.ItemKey,
        KochergaItemSystem.ItemKey,
        ZhukovSItemSystem.ItemKey,
        Cqr47ItemSystem.ItemKey,
        Vipermod1ItemSystem.ItemKey,
        CtrItemSystem.ItemKey,
        Ds150fdeItemSystem.ItemKey,
        AcsItemSystem.ItemKey,
        MoefgItemSystem.ItemKey,
        MoefdeItemSystem.ItemKey,
        MoesgItemSystem.ItemKey,
        // === 瞄具 ===
        MrsItemSystem.ItemKey,
        Eotech553ItemSystem.ItemKey,
        Hhs1ItemSystem.ItemKey,
        SpecterDrItemSystem.ItemKey,
        Monstr2x32ItemSystem.ItemKey,
        Ta01nsnItemSystem.ItemKey,
        RazorHdItemSystem.ItemKey,
        Pm2ItemSystem.ItemKey,
        DeltaPointItemSystem.ItemKey,
        AcroP1ItemSystem.ItemKey,
        // === 消音器/枪口 ===
        HexagonAKMSuppressorItemSystem.ItemKey,
        DynacompItemSystem.ItemKey,
        Dtk1ItemSystem.ItemKey,
        Dtk4mItemSystem.ItemKey,
        DtkpItemSystem.ItemKey,
        Rotor43ItemSystem.ItemKey,
        Nt4ItemSystem.ItemKey,
        SakerItemSystem.ItemKey,
        Kx3ItemSystem.ItemKey,
        Vp09ItemSystem.ItemKey,
        Rotor43762ItemSystem.ItemKey,
        P90AttenuatorItemSystem.ItemKey,
        UmpOemItemSystem.ItemKey,
        Dvl10SilencedItemSystem.ItemKey,
        Ac858ItemSystem.ItemKey,
        HekateDt338ItemSystem.ItemKey,
        Tmb338lmItemSystem.ItemKey,
        Tsm338lmItemSystem.ItemKey,
        SrvvAkmItemSystem.ItemKey,
        Wt0032_1ItemSystem.ItemKey,
        // === 战术设备 ===
        LasTac2ItemSystem.ItemKey,
        Klesch2UItemSystem.ItemKey,
        BaldrProItemSystem.ItemKey,
        TblItemSystem.ItemKey,
        // === 格洛克配件 ===
        GlockViperCutItemSystem.ItemKey,
        GlockPs9ItemSystem.ItemKey,
        GlockUm3ItemSystem.ItemKey,
        GlockAwlwItemSystem.ItemKey,
        GlockG3PortItemSystem.ItemKey,
        GlockLw9ItemSystem.ItemKey,
        GlockOsprey9ItemSystem.ItemKey,
        GlockSrd9ItemSystem.ItemKey,
        // === 加长弹匣 ===
        X47MagItemSystem.ItemKey,
        M4A1Mag560ItemSystem.ItemKey,
        GlockBigStickMagItemSystem.ItemKey,
        GlockG50MagItemSystem.ItemKey,
        // === 工具/维修 ===
        LeathermanItemSystem.ItemKey,
        WeaponRepairKitItemSystem.ItemKey,
    };

    /// <summary>
    /// 大型武器箱可掉落的枪械 ID（30% 概率抽一把）。
    /// </summary>
    public static readonly string[] WeaponGunIds =
    {
        MP133ItemSystem.ItemKey,
        MP153ItemSystem.ItemKey,
        SKSItemSystem.ItemKey,
        AXMCItemSystem.ItemKey,
        DVL10ItemSystem.ItemKey,
        AKMItemSystem.ItemKey,
        DeagleItemSystem.ItemKey,
        Glock17ItemSystem.ItemKey,
        M4A1ItemSystem.ItemKey,
        P90ItemSystem.ItemKey,
        UMP45ItemSystem.ItemKey,
        RPDItemSystem.ItemKey,
        USPItemSystem.ItemKey,
        VSSItemSystem.ItemKey,
        AA12ItemSystem.ItemKey,
    };

    /// <summary>注册武器物资箱 + 地堡结构。</summary>
    public static void Register()
    {
        RegisterWeaponCacheBox();
        RegisterOptionalWeaponCacheBox();
        RegisterLargeWeaponCacheBox();
        RegisterDoorAndReader();
        RegisterBunker();
    }

    /// <summary>注册武器物资箱建筑（可开锁，掉落配件）。</summary>
    private static void RegisterWeaponCacheBox()
    {
        var sprite = LoadSprite("smallweaponcrate");
        if (sprite == null)
            Plugin.Log.LogWarning("[WeaponCacheBunker] smallweaponcrate.png not found, box will use default sprite.");

        var def = new CustomBuildingEntityDefinition
        {
            Name = WModLoc.Tr("wm.bunker.weapon_crate.name", "武器物资箱"),
            Description = WModLoc.Tr("wm.bunker.weapon_crate.desc", "存放武器改装配件的上锁物资箱。"),
            GenerationStyle = BuildingGenerationStyle.None, // 不自动散布，由地堡结构放置
            Sprite = sprite,
            Scale = new Vector3(2f, 2f, 1f), // 2x2 大号物资箱，不再是一格大小
            Health = 4200f, // 高血量，避免被轻易打爆
            AddRigidbody2D = true, // 参与 2D 物理碰撞
            RigidbodyBodyType = RigidbodyType2D.Static,
            IgnoreBodyOptimize = true, // 防止 BuildingEntity.Update 把刚体切成 Dynamic
            Layer = LayerMask.NameToLayer("Ground"), // 确保与玩家碰撞层一致
            HitSoundReferenceId = "metal",
            Metallic = false, // 关闭 10 倍金属切割加成，配合减伤 patch 提高强攻难度
            // UsableObject：玩家可点击交互；Openable：触发原版开锁小游戏
            Components = new[] { typeof(UsableObject), typeof(Openable) },
            ConfigureInstance = (go) =>
            {
                var openable = go.GetComponent<Openable>();
                if (openable != null)
                    openable.lockpickAnglePrecision = LockpickAnglePrecision;

                var usable = go.GetComponent<UsableObject>();
                if (usable != null)
                    usable.langToggleString = "open";

                // 掉落组件：开锁成功（建筑销毁）时随机掉落 1~2 个配件
                go.AddComponent<WeaponCacheBoxDrop>();
            },
        };
        BuildingEntityRegistry.Register(WeaponCacheBoxId, def);
        Plugin.Log.LogInfo($"[WeaponCacheBunker] Registered weapon cache box '{WeaponCacheBoxId}'.");
    }

    /// <summary>注册可选小武器箱（50% 概率生成，用于每房随机 2~3 个小箱）。</summary>
    private static void RegisterOptionalWeaponCacheBox()
    {
        var sprite = LoadSprite("smallweaponcrate");
        if (sprite == null)
            Plugin.Log.LogWarning("[WeaponCacheBunker] smallweaponcrate.png not found, optional box will use default sprite.");

        var def = new CustomBuildingEntityDefinition
        {
            Name = WModLoc.Tr("wm.bunker.weapon_crate.name", "武器物资箱"),
            Description = WModLoc.Tr("wm.bunker.weapon_crate.desc", "存放武器改装配件的上锁物资箱。"),
            GenerationStyle = BuildingGenerationStyle.None,
            Sprite = sprite,
            Scale = new Vector3(2f, 2f, 1f),
            Health = 4200f,
            AddRigidbody2D = true,
            RigidbodyBodyType = RigidbodyType2D.Static,
            IgnoreBodyOptimize = true,
            Layer = LayerMask.NameToLayer("Ground"),
            HitSoundReferenceId = "metal",
            Metallic = false, // 关闭 10 倍金属切割加成，配合减伤 patch 提高强攻难度
            Components = new[] { typeof(UsableObject), typeof(Openable) },
            ConfigureInstance = (go) =>
            {
                var openable = go.GetComponent<Openable>();
                if (openable != null)
                    openable.lockpickAnglePrecision = LockpickAnglePrecision;

                var usable = go.GetComponent<UsableObject>();
                if (usable != null)
                    usable.langToggleString = "open";

                go.AddComponent<WeaponCacheBoxDrop>();
                go.AddComponent<OptionalSmallCrateCuller>(); // 50% 概率自毁，让房间出现 2~3 个小箱
            },
        };
        BuildingEntityRegistry.Register(WeaponCacheBoxOptionalId, def);
        Plugin.Log.LogInfo($"[WeaponCacheBunker] Registered optional weapon cache box '{WeaponCacheBoxOptionalId}'.");
    }

    /// <summary>注册大型武器箱（30% 掉落一把枪 + 2~4 个配件）。</summary>
    private static void RegisterLargeWeaponCacheBox()
    {
        var sprite = LoadSprite("weapon_crate_large");
        if (sprite == null)
            sprite = LoadSprite("smallweaponcrate");

        if (sprite == null)
            Plugin.Log.LogWarning("[WeaponCacheBunker] weapon_crate_large.png not found, large box will use default sprite.");

        // 用户重制的大箱贴图宽高比可能不是 1:1；根据实际贴图高度计算贴地偏移。
        LargeCrateFloorOffset = 0.5f * sprite.bounds.size.y * 3f - 0.5f;

        var def = new CustomBuildingEntityDefinition
        {
            Name = WModLoc.Tr("wm.bunker.weapon_crate_large.name", "大型武器箱"),
            Description = WModLoc.Tr("wm.bunker.weapon_crate_large.desc", "存放大量武器与配件的上锁大型物资箱。"),
            GenerationStyle = BuildingGenerationStyle.None,
            Sprite = sprite,
            Scale = new Vector3(3f, 3f, 1f), // 3x3 大型武器箱
            Health = 8000f,
            AddRigidbody2D = true,
            RigidbodyBodyType = RigidbodyType2D.Static,
            IgnoreBodyOptimize = true,
            Layer = LayerMask.NameToLayer("Ground"),
            HitSoundReferenceId = "metal",
            Metallic = false, // 关闭 10 倍金属切割加成，配合减伤 patch 提高强攻难度
            Components = new[] { typeof(UsableObject), typeof(Openable) },
            ConfigureInstance = (go) =>
            {
                var openable = go.GetComponent<Openable>();
                if (openable != null)
                    openable.lockpickAnglePrecision = LockpickAnglePrecision;

                var usable = go.GetComponent<UsableObject>();
                if (usable != null)
                    usable.langToggleString = "open";

                go.AddComponent<LargeWeaponCacheBoxDrop>();
            },
        };
        BuildingEntityRegistry.Register(WeaponCacheBoxLargeId, def);
        Plugin.Log.LogInfo($"[WeaponCacheBunker] Registered large weapon cache box '{WeaponCacheBoxLargeId}'.");
    }

    /// <summary>注册蓝白加固门 + 刷卡装置（刷 Terragroup 武器室房卡开门）。</summary>
    private static void RegisterDoorAndReader()
    {
        // === 蓝白加固门（血量 = 原版 reinforceddoor 的 3 倍） ===
        var doorSprite = LoadSprite("door_bluewhite");
        if (doorSprite == null)
            Plugin.Log.LogWarning("[WeaponCacheBunker] door_bluewhite.png not found, door will use default sprite.");

        float vanillaDoorHealth = 250f;
        try
        {
            var vanillaDoor = Resources.Load<GameObject>("reinforceddoor");
            if (vanillaDoor != null && vanillaDoor.TryGetComponent<BuildingEntity>(out var vanillaBuilding))
                vanillaDoorHealth = vanillaBuilding.health;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponCacheBunker] Failed to read vanilla reinforceddoor health: {ex.Message}");
        }

        var doorDef = new CustomBuildingEntityDefinition
        {
            Name = WModLoc.Tr("wm.bunker.door.name", "蓝白加固门"),
            Description = WModLoc.Tr("wm.bunker.door.desc", "刷 Terragroup 武器室房卡开启的蓝白相间加固门。"),
            GenerationStyle = BuildingGenerationStyle.None,
            Sprite = doorSprite,
            Scale = new Vector3(2f, 1f, 1f), // 门洞 2 格宽，贴图 18x144 PPU=18 基础 1x8，放大后 2x8
            Health = 1_000_000_000f, // 不可破坏：仅能通过刷卡开启
            Metallic = false, // 关闭 10 倍金属切割加成，配合减伤 patch 提高强攻难度
            AddRigidbody2D = true,
            RigidbodyBodyType = RigidbodyType2D.Static,
            IgnoreBodyOptimize = true, // 防止 BuildingEntity.Update 把刚体切成 Dynamic
            Layer = LayerMask.NameToLayer("Ground"), // 确保与玩家碰撞层一致
            HitSoundReferenceId = "metal",
        };
        BuildingEntityRegistry.Register(WeaponCacheDoorId, doorDef);
        Plugin.Log.LogInfo($"[WeaponCacheBunker] Registered blue-white door '{WeaponCacheDoorId}' health={doorDef.Health:0}.");

        // === 刷卡装置 ===
        var readerSprite = LoadSprite("card_reader");
        if (readerSprite == null)
            Plugin.Log.LogWarning("[WeaponCacheBunker] card_reader.png not found, card reader will use default sprite.");

        var readerDef = new CustomBuildingEntityDefinition
        {
            Name = WModLoc.Tr("wm.bunker.card_reader.name", "门禁刷卡装置"),
            Description = WModLoc.Tr("wm.bunker.card_reader.desc", "使用 Terragroup 武器室房卡开启蓝白加固门。"),
            GenerationStyle = BuildingGenerationStyle.None,
            Sprite = readerSprite,
            Health = 2000f,
            Metallic = false, // 关闭 10 倍金属切割加成，配合减伤 patch 提高强攻难度
            AddRigidbody2D = true,
            RigidbodyBodyType = RigidbodyType2D.Static,
            IgnoreBodyOptimize = true, // 防止 BuildingEntity.Update 把刚体切成 Dynamic
            Layer = LayerMask.NameToLayer("Ground"), // 确保与玩家碰撞层一致
            HitSoundReferenceId = "rubber",
            Components = new[] { typeof(UsableObject) },
            ConfigureInstance = (go) =>
            {
                var usable = go.GetComponent<UsableObject>();
                if (usable != null)
                    usable.langToggleString = "open";

                go.AddComponent<CardReaderDevice>();
            },
        };
        BuildingEntityRegistry.Register(CardReaderId, readerDef);
        Plugin.Log.LogInfo($"[WeaponCacheBunker] Registered card reader '{CardReaderId}'.");
    }

    /// <summary>注册 35x12 实验室风格地堡结构（双层耐热金属墙 + 自定义背景 + 刷卡进入）。</summary>
    private static void RegisterBunker()
    {
        // 35 格宽 x 12 格高。墙体为 H = heatresistantalloy 占位，
        // 生成后由 BunkerWallTilePatch 替换为自定义防弹加强钢 tile（90000 血量）。
        // 布局：左侧 3 格室外门廊（列 0-2），左墙列 3-4（双层），主室列 5-32，
        // 右墙列 33-34（双层）。天花板行 0-1，地板行 10-11，室内高 8 格（行 2-9）。
        // 蓝白加固门在左墙列 3，门洞行 2-9；刷卡装置在门廊列 1。
        string[] fgRows =
        {
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH", // 行0 外天花板
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH", // 行1 内天花板
            ".................................HH", // 行2 门洞列3-4
            ".................................HH", // 行3
            ".................................HH", // 行4
            ".................................HH", // 行5
            ".................................HH", // 行6
            ".................................HH", // 行7
            ".................................HH", // 行8
            ".0.0..0.0.....0.......0......0...HH", // 行9 刷卡(1) 门(3-4) 充电站(6) 小箱(8,22) 大箱(14) 可选小箱(29)
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH", // 行10 内地板
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH", // 行11 外地板
        };
        string[] bgRows =
        {
            "...................................", // 行0
            "...................................", // 行1
            ".....mmmmmmmmmmmmmmmmmmmmmmmmmmmm..", // 行2
            ".....mmmmmmmmmmmmmmmmmmmmmmmmmmmm..", // 行3
            ".....mmmmmmmmmmmmmmmmmmmmmmmmmmmm..", // 行4
            ".....mmmmmmmmmmmmmmmmmmmmmmmmmmmm..", // 行5
            ".....mmmmmmmmmmmmmmmmmmmmmmmmmmmm..", // 行6
            ".....mmmmmmmmmmmmmmmmmmmmmmmmmmmm..", // 行7
            ".....mmmmmmmmmmmmmmmmmmmmmmmmmmmm..", // 行8
            ".....mmmmmmmmmmmmmmmmmmmmmmmmmmmm..", // 行9
            "...................................", // 行10
            "...................................", // 行11
        };

        // 垂直贴地偏移：所有对象都放在行 9（worldY=2）。
        // 偏移公式 pivot.y * 高度 - 0.5 与行号无关。
        float cardReaderOffset = 0f;    // 1x1 贴图，pivot 0.5
        float doorOffset = 3.5f;        // 蓝白门 2x8，pivot 0.5：0.5*8 - 0.5
        float rechargerOffset = GetFloorOffsetY("rechargingstation", 1.5f);
        float smallBoxOffset = 0.5f;    // 小武器箱 2x2：0.5*2 - 0.5
        float largeBoxOffset = LargeCrateFloorOffset; // 由注册大箱时按实际贴图宽高比计算

        Plugin.Log.LogInfo(
            $"[WeaponCacheBunker] Floor offsets: cardReader={cardReaderOffset:0.###}, door={doorOffset:0.###}, " +
            $"recharger={rechargerOffset:0.###}, smallBox={smallBoxOffset:0.###}, largeBox={largeBoxOffset:0.###}");

        // objectsByCell 的 key = 行索引 * width + 列索引（行索引从上到下，非翻转）
        // 刷卡(1,9)=316，门(3,9)=318，充电站(6,9)=321，小箱(8,9)=323，大箱(14,9)=329，小箱(22,9)=337，可选小箱(29,9)=344
        var objectsByCell = new Dictionary<string, object>
        {
            ["316"] = new { id = CardReaderId },
            ["318"] = new { id = WeaponCacheDoorId },
            ["321"] = new { id = "rechargingstation" },
            ["323"] = new { id = WeaponCacheBoxId },
            ["329"] = new { id = WeaponCacheBoxLargeId },
            ["337"] = new { id = WeaponCacheBoxId },
            ["344"] = new { id = WeaponCacheBoxOptionalId },
        };

        // CUCoreLib v1 precisePlacements 在实体路径中按 worldY 查找，因此 gridY 填 worldY。
        // 行 9 的 worldY = 12-1-9 = 2。
        var precisePlacements = new[]
        {
            new { gridX = 1,  gridY = 2, offsetX = 0f, offsetY = cardReaderOffset, rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 3,  gridY = 2, offsetX = 0.5f, offsetY = doorOffset,       rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 6,  gridY = 2, offsetX = 0f, offsetY = rechargerOffset,   rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 8,  gridY = 2, offsetX = 0f, offsetY = smallBoxOffset,    rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 14, gridY = 2, offsetX = 0f, offsetY = largeBoxOffset,    rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 22, gridY = 2, offsetX = 0f, offsetY = smallBoxOffset,    rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 29, gridY = 2, offsetX = 0f, offsetY = smallBoxOffset,    rotation = 0f, scale = 1f, flipX = false, flipY = false },
        };

        var payload = new
        {
            metadata = new
            {
                schemaVersion = 4,
                // 5 个深度层各生成 3 个（biomeDepth 0~4，索引对应）
                spawnCounts = new[] { 0, 2, 2, 3, 0 }, // 第1层不刷，第2-3层各2，第4层3，第5层不刷
                avoidOverlap = true,
            },
            width = 35,
            height = 12,
            layers = new[]
            {
                new { id = "fg", kind = "fg", visible = true, rows = fgRows },
                new { id = "bg", kind = "bg", visible = true, rows = bgRows },
            },
            objectsByCell = objectsByCell,
            precisePlacements = precisePlacements,
        };

        string json = JsonConvert.SerializeObject(payload);
        bool ok = StructureRegistry.RegisterFromJson(BunkerId, json, "weapon cache bunker");
        Plugin.Log.LogInfo($"[WeaponCacheBunker] Registered bunker '{BunkerId}': {ok}");
    }

    /// <summary>
    /// 计算建筑对象在“行 8（worldY=1）”放置时，为让 sprite 底部贴地所需的垂直偏移。
    /// </summary>
    private static float GetFloorOffsetY(string prefabId, float fallback)
    {
        try
        {
            var prefab = Resources.Load<GameObject>(prefabId);
            if (prefab == null) return fallback;

            // 优先取根节点上的 SpriteRenderer；子节点可能是屏幕、特效等大贴图，
            // 之前用 GetComponentInChildren 取到错误 sprite，导致偏移量飞出去。
            var sr = prefab.GetComponent<SpriteRenderer>();
            if (sr == null) sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null || sr.sprite == null) return fallback;

            float height = sr.sprite.bounds.size.y * sr.transform.lossyScale.y;
            float pivotY = sr.sprite.pivot.y;
            float offset = pivotY * height - 0.5f;

            // 安全钳：地堡总高才 10 格，超过这个范围的偏移肯定是取错了 sprite。
            if (offset < -5f || offset > 8f)
            {
                Plugin.Log.LogWarning(
                    $"[WeaponCacheBunker] GetFloorOffsetY('{prefabId}') suspicious offset {offset:0.###}; using fallback {fallback:0.###}.");
                return fallback;
            }

            return offset;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponCacheBunker] GetFloorOffsetY('{prefabId}') failed: {ex.Message}");
            return fallback;
        }
    }
    /// <summary>从插件资源目录加载贴图。</summary>
    private static Sprite LoadSprite(string name)
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? Paths.PluginPath;
            var path = Path.Combine(assemblyDir, "Framework", "Assets", "guns", name + ".png");
            if (!File.Exists(path)) return null;

            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) return null;
            tex.filterMode = FilterMode.Point;
            // pixelsPerUnit = 贴图像素数，使建筑约 1x1 单位（1 格）
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponCacheBunker] LoadSprite('{name}') failed: {ex.Message}");
            return null;
        }
    }
}

/// <summary>
/// 武器物资箱掉落组件：开锁成功（建筑销毁）时随机掉落 1~2 个武器改装配件。
/// 通过 Utils.Create 创建物品，走 CUCoreLib 拦截 + KrokMP 网络同步。
/// </summary>
public class WeaponCacheBoxDrop : MonoBehaviour
{
    private BuildingEntity _building;
    private bool _isQuitting;

    private void Awake()
    {
        _building = GetComponent<BuildingEntity>();
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void OnDestroy()
    {
        if (_isQuitting) return;

        // 只有建筑被“摧毁”时才掉落（开锁成功 health=0、或被打爆 health<0.5）。
        // 世界卸载/对象回收等非摧毁场景 health 仍满，不产生掉落。
        if (_building == null || _building.health >= 0.5f) return;

        // 多人客户端：仅上报服务器（服务器端销毁箱子+掉落+广播其他客户端），
        // 本地跳过掉落，防止与服务器 Utils.Create 同步的物品重复。
        if (KrokMpHelper.IsMultiplayer && !KrokMpHelper.IsHost)
        {
            WeaponMpSync.ReportBunkerBox(transform.position);
            return;
        }

        try
        {
            int count = UnityEngine.Random.Range(1, 3); // 1~2 个
            for (int i = 0; i < count; i++)
            {
                string id = WeaponCacheBunker.WeaponPartIds[
                    UnityEngine.Random.Range(0, WeaponCacheBunker.WeaponPartIds.Length)];

                var pos = (Vector2)transform.position
                          + new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0.5f, 1.5f));
                var go = Utils.Create(id, pos, UnityEngine.Random.Range(0f, 360f));
                if (go == null)
                {
                    Plugin.Log.LogWarning($"[WeaponCacheBox] Failed to spawn drop '{id}'.");
                    continue;
                }

                var item = go.GetComponent<Item>();
                if (item != null)
                    item.SetCondition(1f); // 满耐久掉落

                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.velocity = new Vector2(UnityEngine.Random.Range(-7f, 7f), UnityEngine.Random.Range(-7f, 7f));

                go.AddComponent<FreshItemDrop>();
            }
            Plugin.Log.LogInfo($"[WeaponCacheBox] Dropped {count} weapon part(s).");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[WeaponCacheBox] Drop failed: {ex}");
        }
    }

}
/// <summary>
/// 大型武器箱掉落组件：开锁/破坏后 30% 概率掉落一把随机枪械，并掉落 2~4 个武器改装配件。
/// </summary>
public class LargeWeaponCacheBoxDrop : MonoBehaviour
{
    private BuildingEntity _building;
    private bool _isQuitting;

    private void Awake()
    {
        _building = GetComponent<BuildingEntity>();
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void OnDestroy()
    {
        if (_isQuitting) return;
        if (_building == null || _building.health >= 0.5f) return;

        // 多人客户端：仅上报服务器，本地跳过掉落（同 WeaponCacheBoxDrop）
        if (KrokMpHelper.IsMultiplayer && !KrokMpHelper.IsHost)
        {
            WeaponMpSync.ReportBunkerBox(transform.position);
            return;
        }

        try
        {
            int attachmentCount = UnityEngine.Random.Range(2, 5); // 2~4 个配件
            for (int i = 0; i < attachmentCount; i++)
                SpawnDrop(WeaponCacheBunker.WeaponPartIds[
                    UnityEngine.Random.Range(0, WeaponCacheBunker.WeaponPartIds.Length)]);

            // 30% 概率额外掉落一把枪
            if (UnityEngine.Random.value < 0.3f)
                SpawnDrop(WeaponCacheBunker.WeaponGunIds[
                    UnityEngine.Random.Range(0, WeaponCacheBunker.WeaponGunIds.Length)]);

            Plugin.Log.LogInfo($"[WeaponCacheBoxLarge] Dropped {attachmentCount} attachment(s) + gun chance.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[WeaponCacheBoxLarge] Drop failed: {ex}");
        }
    }

    private void SpawnDrop(string id)
    {
        var pos = (Vector2)transform.position
                  + new Vector2(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(0.5f, 2f));
        var go = Utils.Create(id, pos, UnityEngine.Random.Range(0f, 360f));
        if (go == null)
        {
            Plugin.Log.LogWarning($"[WeaponCacheBoxLarge] Failed to spawn drop '{id}'.");
            return;
        }

        var item = go.GetComponent<Item>();
        if (item != null)
            item.SetCondition(1f); // 满耐久掉落

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = new Vector2(UnityEngine.Random.Range(-7f, 7f), UnityEngine.Random.Range(-7f, 7f));

        go.AddComponent<FreshItemDrop>();
    }
}

/// <summary>
/// 可选小武器箱：Awake 时 50% 概率自毁，用于让每个武器室随机出现 2~3 个小箱。
/// 自毁时建筑血量仍满，WeaponCacheBoxDrop.OnDestroy 不会掉落物品。
/// </summary>
public class OptionalSmallCrateCuller : MonoBehaviour
{
    private void Awake()
    {
        if (UnityEngine.Random.value >= 0.5f) return;

        var building = GetComponent<BuildingEntity>();
        if (building != null)
        {
            // 直接销毁 GameObject；血量仍满，不会触发任何掉落。
            Destroy(gameObject);
        }
    }
}

/// <summary>
/// 刷卡装置：检测玩家身上是否有 Terragroup 武器室房卡，有则扣减一次并打开附近的蓝白加固门。
/// </summary>
public class CardReaderDevice : MonoBehaviour
{
    /// <summary>固定门禁卡物品 ID。</summary>
    public const string AccessCardId = WeaponRoomKeycardItemSystem.ItemKey;

    /// <summary>开门搜索半径。</summary>
    public const float OpenRadius = 6f;

    public void OnUse()
    {
        try
        {
            var body = PlayerCamera.main != null ? PlayerCamera.main.body : null;
            if (body == null) return;

            if (!body.FindByIdSurface(AccessCardId, out var cardItem))
            {
                Sound.Play("beep", transform.position, false, true, null, 1f, 1f, false, false);
                return;
            }

            if (cardItem == null || cardItem.condition <= 0f)
            {
                Sound.Play("beep", transform.position, false, true, null, 1f, 1f, false, false);
                return;
            }

            var hits = Physics2D.OverlapCircleAll(transform.position, OpenRadius);
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (!hit.TryGetComponent<BuildingEntity>(out var building)) continue;
                if (building.id != WeaponCacheBunker.WeaponCacheDoorId) continue;

                building.Backgroundify();

                // 和原版生物终端机制类似：每使用一次损失 2% 耐久。
                cardItem.SetCondition(Mathf.Clamp(cardItem.condition - 0.02f, 0f, 1f));
                Sound.Play("unlock", transform.position, false, true, null, 1f, 1f, false, false);

                // 多人客户端：上报服务器（服务器端扣钥匙卡耐久 + 开门 + 广播其他客户端）。
                // 主机端 KrokMP 建筑状态同步（backgroundified）与物品状态同步自动传播，无需上报。
                if (KrokMpHelper.IsMultiplayer && !KrokMpHelper.IsHost)
                    WeaponMpSync.ReportKeycardUse(cardItem, cardItem.condition, building.transform.position);

                Plugin.Log.LogInfo($"[WeaponCacheBunker] Weapon room keycard condition: {cardItem.condition:P}.");
                return;
            }

            Sound.Play("beep", transform.position, false, true, null, 1f, 1f, false, false);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponCacheBunker] CardReaderDevice.OnUse failed: {ex.Message}");
        }
    }
}

public static class BunkerWallTilePatch
{
    private const ushort BulletproofSteelTileIndex = 36;
    private const int HeatResistantAlloyBlockId = 10; // H = heatresistantalloy
    private const float BulletproofSteelHealth = 90000f; // 15000 * 6（耐热合金 6 倍，即上次的 2 倍）
    private static bool _tileRegistered;


    public static void Prefix(object[] __args)
    {
        if (__args == null || __args.Length < 2) return;

        var worldPos = (Vector2)__args[0];
        var definition = __args[1];
        if (definition == null) return;

        string id;
        try
        {
            id = Traverse.Create(definition).Field("ID").GetValue() as string;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponCacheBunker] BunkerWallTilePatch failed to read structure ID: {ex.Message}");
            return;
        }

        if (id != WeaponCacheBunker.BunkerId && id != BlueAreaBunker.BunkerId && id != RedAreaBunker.BunkerId) return;

        EnsureBulletproofSteelRegistered();

        // 等原始 PlaceStructure 同步执行完后再替换方块。
        CUCoreUtils.DelayCall(0.1f, () => ReplaceHeatWallsWithBulletproofSteel(worldPos, definition));
    }

    private static void EnsureBulletproofSteelRegistered()
    {
        if (_tileRegistered) return;
        _tileRegistered = true;

        try
        {
            var sprite = LoadTileSprite("bulletproof_steel");
            if (sprite == null)
            {
                Plugin.Log.LogWarning("[WeaponCacheBunker] bulletproof_steel.png not found; bunker walls will stay as heatresistantalloy.");
                return;
            }

            TileRegistry.Register(BulletproofSteelTileIndex, new CustomTileDefinition
            {
                ID = "bulletproof_steel",
                Name = WModLoc.Tr("wm.bunker.steel.name", "防弹加强钢"),
                Description = WModLoc.Tr("wm.bunker.steel.desc", "由耐热合金复合而成的防弹钢板，强度约为耐热合金的三倍。"),
                Sprite = sprite,
                Health = BulletproofSteelHealth,
                HitSound = "steel",
                StepSound = "Steel",
                Metallic = false, // 关闭 10 倍金属切割加成，配合减伤 patch 提高强攻难度
                NoVariation = true,
                SpawnAmount = 0f,
            });
            Plugin.Log.LogInfo("[WeaponCacheBunker] Registered bulletproof_steel tile (index 36, health 90000).");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponCacheBunker] Bulletproof steel tile registration failed: {ex.Message}");
        }
    }

    private static void ReplaceHeatWallsWithBulletproofSteel(Vector2 worldPos, object definition)
    {
        try
        {
            var world = WorldGeneration.world;
            if (world == null) return;

            var compiled = Traverse.Create(definition).Field("CompiledStructure").GetValue();
            if (compiled == null) return;

            var widthValue = Traverse.Create(compiled).Field("Width").GetValue();
            var heightValue = Traverse.Create(compiled).Field("Height").GetValue();
            var blockIds = Traverse.Create(compiled).Field("BlockIDs").GetValue() as int[,];
            if (widthValue == null || heightValue == null || blockIds == null) return;

            int width = (int)widthValue;
            int height = (int)heightValue;
            var center = world.WorldToBlockPos(worldPos);
            int startX = center.x - width / 2;
            int startY = center.y - height / 2;

            for (int x = 0; x < width; x++)
            {
                int globalX = startX + x;
                for (int y = 0; y < height; y++)
                {
                    if (blockIds[x, y] != HeatResistantAlloyBlockId) continue;

                    world.SetBlock(new Vector2Int(globalX, startY + y), BulletproofSteelTileIndex);
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponCacheBunker] ReplaceHeatWallsWithBulletproofSteel failed: {ex.Message}");
        }
    }

    private static Sprite LoadTileSprite(string name)
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? Paths.PluginPath;
            var path = Path.Combine(assemblyDir, "Framework", "Assets", "structures", name + ".png");
            if (!File.Exists(path)) return null;

            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) return null;
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponCacheBunker] LoadTileSprite('{name}') failed: {ex.Message}");
            return null;
        }
    }
}


/// <summary>
/// 防弹加强钢 tile 减伤补丁：对所有伤害（爆炸、挖掘、近战等）统一降低 75%。
/// 由 Plugin.Awake 手动注册。
/// </summary>
public static class BunkerTileDamagePatch
{
    private const ushort BulletproofSteelTileIndex = 36;
    private const float DamageMultiplier = 0.95f; // 按需求调整：承受 95% 伤害

    public static void Prefix(Vector2Int pos, ref float dmg)
    {
        try
        {
            var world = WorldGeneration.world;
            if (world == null) return;

            if (world.GetBlock(pos) == BulletproofSteelTileIndex)
                dmg *= DamageMultiplier;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponCacheBunker] BunkerTileDamagePatch failed: {ex.Message}");
        }
    }
}

/// <summary>
/// 防弹加强钢 tile 爆炸保护补丁。
/// 原版 CreateExplosion -> GenerateBlockCircle 会直接把范围内方块改成空气，
/// 而不是走 DamageBlock，所以上面的减伤 patch 对爆炸无效。
/// 这里在 GenerateBlockCircle 执行前记录范围内所有防弹加强钢 tile，
/// 执行后把它们恢复为 tile 36。
/// </summary>
public static class BunkerExplosionPatch
{
    private const ushort BulletproofSteelTileIndex = 36;

    public static void Prefix(Vector2 pos, int size, ushort block, out List<Vector2Int> __state)
    {
        __state = null;
        if (block != 0) return;

        var world = WorldGeneration.world;
        if (world == null) return;

        var center = world.WorldToBlockPos(pos);
        var list = new List<Vector2Int>();
        for (int dx = -size; dx <= size; dx++)
        {
            for (int dy = -size; dy <= size; dy++)
            {
                if (dx * dx + dy * dy >= size * size) continue;

                var p = new Vector2Int(center.x + dx, center.y + dy);
                if (p.x < 0 || p.y < 0 || p.x >= (int)world.width || p.y >= (int)world.height) continue;
                if (world.GetBlock(p) == BulletproofSteelTileIndex)
                    list.Add(p);
            }
        }

        if (list.Count > 0) __state = list;
    }

    public static void Postfix(Vector2 pos, int size, ushort block, List<Vector2Int> __state)
    {
        if (__state == null || __state.Count == 0) return;

        var world = WorldGeneration.world;
        if (world == null) return;

        foreach (var p in __state)
        {
            if (world.GetBlock(p) == 0)
                world.SetBlock(p, BulletproofSteelTileIndex);
        }
    }
}
/// <summary>
/// CUCoreLib 结构背景砖补丁。
/// 原版 SpawnBackgroundTile 会把所有背景砖压暗到 0.35（视觉近黑）。
/// 这里拦截大理石（block 18）背景触发标记，改为读取自定义实验室背景贴图：
///     Framework/Assets/structures/lab_background.png
/// 用户可直接替换该 PNG 来自定义背景；若图片缺失，则回退到原版 tile 贴图并渲染为纯白。
/// 同时会随机在背景墙上贴海报：
///     Framework/Assets/structures/labposter.png（或 labsposter.png）
/// 由 Plugin.Awake 手动注册，避免影响其它结构使用的背景砖。
/// </summary>
public static class LabWhiteBackgroundPatch
{
    /// <summary>背景触发标记：大理石 tile 在 CUCoreLib GlobalBlockMap 中的 block ID（m = marble）。</summary>
    public const int MarbleBlockId = 18;

    private static GameObject _template;
    private static Sprite _customBackground;
    private static bool _customBackgroundLoaded;
    private static Sprite _customBackgroundDirty;
    private static bool _customBackgroundDirtyLoaded;
    private static Sprite _posterSprite;
    private static bool _posterSpriteLoaded;
    private static Sprite _stripSprite;
    private static bool _stripSpriteLoaded;
    private static int _backgroundTileCount;

    public static bool Prefix(int blockId, Vector2 position)
    {
        // 只接管大理石背景触发标记，其余保持 CUCoreLib 原逻辑。
        if (blockId != MarbleBlockId) return true;

        var world = WorldGeneration.world;
        if (world?.tiles == null || blockId < 0 || blockId >= world.tiles.Length) return false;
        if (!(world.tiles[blockId] is Tile tile) || tile.sprite == null) return false;

        var sprite = GetBackgroundSprite(tile, position);
        if (sprite == null) return false;

        var template = GetTemplate();
        if (template == null) return false;

        var backgroundObject = UnityEngine.Object.Instantiate(template, position, Quaternion.identity);
        backgroundObject.SetActive(true);
        backgroundObject.name = "CUCoreLib_BGTile_" + blockId + "_lab_custom";
        if (world.worldGrid != null)
            backgroundObject.transform.SetParent(world.worldGrid.transform);

        var renderer = backgroundObject.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = -998;

        _backgroundTileCount++;
        int roomIndex = (_backgroundTileCount - 1) % 224; // 每个房间 28x8 = 224 块背景砖

        // 每个房间的第一块背景砖：在整面背景墙的中央挂一条 Weapon Area 长条。
        if (roomIndex == 0)
            SpawnWeaponAreaStrip(position + new Vector2(13.5f, 3.5f), world.worldGrid != null ? world.worldGrid.transform : null);

        // 大约每个地堡 1 张海报：背景砖按固定间隔抽取，位置随每块背景砖变化。
        if (_backgroundTileCount % 197 == 0)
            SpawnPoster(position, backgroundObject.transform);

        return false;
    }

    private static void SpawnPoster(Vector2 position, Transform parent)
    {
        try
        {
            var poster = GetPosterSprite();
            if (poster == null) return;

            var go = new GameObject("CUTarkovWeaponMod_LabPoster");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = poster;
            sr.color = Color.white;
            sr.sortingOrder = -900; // 在背景墙前方，但在建筑/物品后方
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponCacheBunker] SpawnPoster failed: {ex.Message}");
        }
    }

    private static void SpawnWeaponAreaStrip(Vector2 position, Transform parent)
    {
        try
        {
            var strip = GetWeaponAreaStripSprite();
            if (strip == null) return;

            var go = new GameObject("CUTarkovWeaponMod_WeaponAreaStrip");
            if (parent != null)
                go.transform.SetParent(parent, false);
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = strip;
            sr.color = Color.white;
            sr.sortingOrder = -950; // 在背景墙前，海报后
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponCacheBunker] SpawnWeaponAreaStrip failed: {ex.Message}");
        }
    }

    private static Sprite GetWeaponAreaStripSprite()
    {
        if (!_stripSpriteLoaded)
        {
            _stripSpriteLoaded = true;
            _stripSprite = LoadCustomSprite("structures", "lab_background_strip");
        }

        return _stripSprite;
    }

    private static Sprite GetPosterSprite()
    {
        if (!_posterSpriteLoaded)
        {
            _posterSpriteLoaded = true;
            _posterSprite = LoadCustomSprite("structures", "labposter");
            if (_posterSprite == null)
                _posterSprite = LoadCustomSprite("structures", "labsposter");
        }

        return _posterSprite;
    }

    private static Sprite GetBackgroundSprite(Tile tile, Vector2 position)
    {
        if (!_customBackgroundLoaded)
        {
            _customBackgroundLoaded = true;
            _customBackground = LoadCustomSprite("structures", "lab_background");
        }

        if (!_customBackgroundDirtyLoaded)
        {
            _customBackgroundDirtyLoaded = true;
            _customBackgroundDirty = LoadCustomSprite("structures", "lab_background_dirty");
        }

        var clean = _customBackground != null ? _customBackground : tile.sprite;
        var dirty = _customBackgroundDirty != null ? _customBackgroundDirty : clean;

        // 根据世界位置做稳定哈希，让脏/净瓷砖按约 3:7 混合，避免闪烁。
        int h = position.GetHashCode() & 0x7fffffff;
        return h % 10 < 3 ? dirty : clean;
    }

    private static Sprite LoadCustomSprite(string folder, string name)
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? Paths.PluginPath;
            var path = Path.Combine(assemblyDir, "Framework", "Assets", folder, name + ".png");
            if (!File.Exists(path)) return null;

            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) return null;
            tex.filterMode = FilterMode.Bilinear; // 海报/背景贴图用双线性更柔和

            // 背景：宽度 = 贴图像素数，使每张背景图正好覆盖 1 格。
            // 海报：宽度固定为 5 格。
            // Weapon Area 长条：宽度固定为 28 格（正好铺满主室背景墙）。
            float ppu = tex.width;
            if (name == "labposter" || name == "labsposter")
                ppu = tex.width / 5f;
            else if (name == "lab_background_strip")
                ppu = tex.width / 28f;

            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponCacheBunker] LoadCustomSprite('{folder}/{name}') failed: {ex.Message}");
            return null;
        }
    }

    private static GameObject GetTemplate()
    {
        if (_template != null) return _template;

        _template = new GameObject("CUTarkovWeaponMod_LabBackgroundTileTemplate");
        _template.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(_template);
        _template.AddComponent<SpriteRenderer>();
        return _template;
    }
}
