using System;
using System.Collections.Generic;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 瞄准系统（长按右键瞄准）。
///
/// 机制：
/// - 长按右键（iteminteract）→ aimProgress 从 0 累积到 1（每把枪不同恢复时间）
/// - 松开右键 → aimProgress 逐渐下降
/// - 开火时 FireEffectsPatch 根据 aimProgress 计算有效散布：
///     未瞄准(0)：spread = baseSpread × 2.5
///     瞄准满(1)：spread = baseSpread × 瞄准加成（各瞄具额外降低）
/// - 各配件加快/减慢瞄准速度
/// - 瞄准时玩家眼睛半闭（eyeCloseTime）+ 准星十字从远到近汇拢
///
/// 每把枪的瞄准恢复时间通过 AimTimeMap 配置（AKM 默认 1.5 秒）。
/// </summary>
public static class AimSystem
{
    // ===== 参数 =====
    public const float AimedSpreadMult = 1.0f;     // 瞄准满：回到武器原版精准度
    public const float AimDecayPerSecond = 3.33f;  // 松开右键后 aimProgress 下降速度（0.3 秒从 1 降到 0）

    // ===== 腰射散布分级（未瞄准惩罚）=====
    // 按枪械类型分级，让不同枪械的腰射精度差异明显：
    // - 机枪/狙击枪：腰射散布最大（×5.5）
    // - 步枪/半自动卡宾枪：其次（×4.0）
    // - 冲锋枪：再次（×3.0）
    // - 手枪/霰弹枪：最小（×2.0）
    private const float UnaimedMultMachineGun = 5.5f;
    private const float UnaimedMultRifle = 4.0f;
    private const float UnaimedMultSMG = 3.0f;
    private const float UnaimedMultPistolShotgun = 2.0f;

    // ===== 每把枪的瞄准恢复时间（秒）=====
    private static readonly Dictionary<string, float> AimTimeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "axmc", 2.0f },
        { "dvl10", 1.3f },
        { "vss", 1.2f },
        { "sks", 1.6f },
        { "m4a1", 1.6f },
        { "akm", 1.6f },
        { "usp", 0.5f },
        { "glock17", 0.5f },
        { "rpd", 2.5f },
        { "ump45", 1.1f },
        { "p90", 1.0f },
        { "mp133", 1.6f },
        { "mp153", 1.6f },
        { "aa12", 2.0f },
        { "deagle", 0.8f },
    };

    // ===== 每把枪的腰射散布（用户指定：0=最准，0.8=最宽） =====
    private static readonly Dictionary<string, float> HipFireSpreadMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { AA12ItemSystem.ItemKey, 0.40f },
        { AKMItemSystem.ItemKey, 0.50f },
        { AXMCItemSystem.ItemKey, 0.75f },
        { DeagleItemSystem.ItemKey, 0.25f },
        { DVL10ItemSystem.ItemKey, 0.65f },
        { Glock17ItemSystem.ItemKey, 0.35f },
        { M4A1ItemSystem.ItemKey, 0.45f },
        { MP133ItemSystem.ItemKey, 0.37f },
        { MP153ItemSystem.ItemKey, 0.38f },
        { P90ItemSystem.ItemKey, 0.36f },
        { RPDItemSystem.ItemKey, 0.66f },
        { SKSItemSystem.ItemKey, 0.42f },
        { UMP45ItemSystem.ItemKey, 0.40f },
        { USPItemSystem.ItemKey, 0.20f },
        { VSSItemSystem.ItemKey, 0.42f },
    };

    // ===== 各配件对瞄准速度的影响（秒，正=减慢，负=加快）=====
    private static readonly Dictionary<string, float> AttachmentAimTimeDelta = new(StringComparer.OrdinalIgnoreCase)
    {
        { HexagonAKMSuppressorItemSystem.ItemKey, 0.5f },    // hexagonakm 减慢0.5
        { Rotor43ItemSystem.ItemKey, 0.6f },                 // rotor43 减慢0.6（开镜速度+0.6s）
        { Nt4ItemSystem.ItemKey, 1.0f },                     // nt4 减慢1.0
        { SakerItemSystem.ItemKey, 0.65f },                  // saker 减慢0.65
        { Kx3ItemSystem.ItemKey, 0.15f },                    // kx3 减慢0.15
        { Vp09ItemSystem.ItemKey, 0.12f },                   // vp09 减慢0.12
        { Rotor43762ItemSystem.ItemKey, 0.9f },              // rotor43762 减慢0.9
        { HexagonSksItemSystem.ItemKey, 0.62f },             // hexagon_sks 减慢0.62
        { SksMcItemSystem.ItemKey, -0.45f },                 // sks_mc 加快0.45（开镜速度-0.45s）
        { SrvvAkmItemSystem.ItemKey, 0.12f },                // srvvakm 减慢0.12
        { Dtk4mItemSystem.ItemKey, 0.62f },                  // dtk4m 减慢0.62
        { DtkpItemSystem.ItemKey, 0.32f },                   // dtkp 减慢0.32
        { Ac858ItemSystem.ItemKey, 0.05f },                  // ac858 减慢0.05
        { HekateDt338ItemSystem.ItemKey, 0.35f },            // hekate_dt338 减慢0.35
        { Tmb338lmItemSystem.ItemKey, 0.06f },               // tmb338lm 减慢0.06
        { Tsm338lmItemSystem.ItemKey, 0.32f },               // tsm338lm 减慢0.32
        { AxmcGripItemSystem.ItemKey, -0.65f },              // axmc_grip 加快0.65（瞄准时间-0.65s）
        { Dvl10SilencedItemSystem.ItemKey, -0.25f },         // dvl10_silenced 加快0.25（瞄准时间-0.25s）
        { MoeAkmItemSystem.ItemKey, -0.2f },                 // moeakm 加快0.2
        { UasSksItemSystem.ItemKey, -1.0f },                 // uas_sks 加快1.0（瞄准速度-1s）
        { HexagonAkHandguardItemSystem.ItemKey, -0.3f },     // hexagonak_hg 加快0.3
        { Rk3ItemSystem.ItemKey, -0.5f },                    // rk3 加快0.5
        { Ags74ItemSystem.ItemKey, -0.3f },                  // ags74 加快0.3
        { Td120001ItemSystem.ItemKey, -0.18f },              // td120001 加快0.18
        { StarkArrgItemSystem.ItemKey, -0.2f },              // starkarrg 加快0.2
        { MiadItemSystem.ItemKey, -0.12f },                  // miad 加快0.12
        { F1st2pcItemSystem.ItemKey, -0.65f },               // f1st2pc 加快0.65
        { ErgoItemSystem.ItemKey, -0.3f },                   // ergo 加快0.3
        { Vipermod1ItemSystem.ItemKey, -0.85f },             // vipermod1 加快0.85
        { CtrItemSystem.ItemKey, -0.5f },                    // ctr 加快0.5
        { Ds150fdeItemSystem.ItemKey, -0.44f },              // ds150fde 加快0.44
        { AcsItemSystem.ItemKey, -0.25f },                   // acs 加快0.25
        { MoefgItemSystem.ItemKey, -0.45f },                 // moefg 加快0.45
        { MoefdeItemSystem.ItemKey, -0.45f },                // moefde 加快0.45
        { MoesgItemSystem.ItemKey, -0.45f },                 // moesg 加快0.45
        { MrsItemSystem.ItemKey, 0.1f },                     // mrs 减慢0.1
        { Eotech553ItemSystem.ItemKey, 0.2f },               // 553 减慢0.2
        { Hhs1ItemSystem.ItemKey, 0.4f },                    // hhs1 减慢0.4
        { SpecterDrItemSystem.ItemKey, 0.5f },               // specterdr 减慢0.5
        { Monstr2x32ItemSystem.ItemKey, 0.2f },              // monstr2x32 减慢0.2
        { Ta01nsnItemSystem.ItemKey, 0.3f },                 // ta01nsn 减慢0.3
        { RazorHdItemSystem.ItemKey, 0.5f },                 // razorhd 减慢0.5
        { Pm2ItemSystem.ItemKey, 0.7f },                     // pm2 减慢0.7
        { OpforAak7ItemSystem.ItemKey, -0.7f },              // opforaa47 加快0.7
        { KochergaItemSystem.ItemKey, -0.5f },               // kocherga 加快0.5
        { ZhukovSItemSystem.ItemKey, -1.0f },                // zhukovs 加快1
        { Cqr47ItemSystem.ItemKey, -1.2f },                  // cqr47 加快1.2
        // 前握把（垂直握把）
        { ShiftForegripItemSystem.ItemKey, -0.12f },         // shift 加快0.12
        { Se5ForegripItemSystem.ItemKey, -0.22f },           // se5 加快0.22
        { Rk0ForegripItemSystem.ItemKey, -0.08f },           // rk0 加快0.08
        { Rk2ForegripItemSystem.ItemKey, 0.1f },             // rk2 减慢0.1
        { B25ur1ForegripItemSystem.ItemKey, -0.15f },        // b25ur1 加快0.15
        { CobraForegripItemSystem.ItemKey, -0.27f },         // cobra 加快0.27
        { P2ForegripItemSystem.ItemKey, -0.08f },            // p2 加快0.08
        { AfgForegripItemSystem.ItemKey, -0.1f },            // afg 加快0.1
        // 护木（AKM 专属）
        { B10mB19ItemSystem.ItemKey, -0.12f },               // b10mb19 加快0.12
        { WasrItemSystem.ItemKey, -0.17f },                  // wasr 加快0.17
        // 护木（M4 专属）
        { MoeSlItemSystem.ItemKey, -0.14f },                 // moesl 加快0.14
        { ViperItemSystem.ItemKey, -0.1f },                  // viper 加快0.1
        { KacRisItemSystem.ItemKey, -0.15f },                // kacris 加快0.15
        { SmrMk16ItemSystem.ItemKey, -0.52f },               // smrmk16 加快0.52
        { AdarWoodItemSystem.ItemKey, -0.25f },              // adarwood 加快0.25
        { LvoaItemSystem.ItemKey, -0.5f },                   // lvoa 加快0.5
        // 枪管（M4 专属）
        { M4LongBarrelItemSystem.ItemKey, 0.6f },            // m4longbarrel 减慢0.6
        // 战术设备（手电/激光）
        { TblItemSystem.ItemKey, 0.1f },                     // tbl 减慢0.1
        { Klesch2UItemSystem.ItemKey, 0.25f },               // klesch2u 减慢0.25
        { BaldrProItemSystem.ItemKey, 0.18f },               // baldrpro 减慢0.18
        { LasTac2ItemSystem.ItemKey, 0.15f },                // lastac2 减慢0.15
        // 枪口装置（膛口制退器）
        { DynacompItemSystem.ItemKey, 0.05f },               // dynacomp 减慢0.05
        { Dtk1ItemSystem.ItemKey, 0.1f },                    // dtk1 减慢0.1
        // 护木
        { AkmLItemSystem.ItemKey, -0.35f },                  // akml 加快0.35
        // 格洛克套筒
        { GlockViperCutItemSystem.ItemKey, -0.05f },         // glock_vipercut 加快0.05（瞄准时间-0.05s）
        { GlockPs9ItemSystem.ItemKey, -0.02f },              // glock_ps9 加快0.02（瞄准时间-0.02s）
        // 格洛克基座
        { GlockUm3ItemSystem.ItemKey, 0.1f },                // glock_um3 减慢0.1（瞄准速度+0.1s）
        // 格洛克枪口配件
        { GlockG3PortItemSystem.ItemKey, 0.05f },            // glock_g3port 减慢0.05（瞄准速度+0.05s）
        { GlockLw9ItemSystem.ItemKey, 0.08f },               // glock_lw9 减慢0.08（瞄准速度+0.08s）
        { GlockOsprey9ItemSystem.ItemKey, 0.22f },           // glock_osprey9 减慢0.22（瞄准速度+0.22s）
        { GlockSrd9ItemSystem.ItemKey, 0.42f },              // glock_srd9 减慢0.42（瞄准速度+0.42s）
        // P90 枪口消音器
        { P90AttenuatorItemSystem.ItemKey, 0.3f },           // p90attenuator 减慢0.3（瞄准速度+0.3s）
        { UmpOemItemSystem.ItemKey, 0.15f },                 // ump_oem 减慢0.15（瞄准速度+0.15s）
    };

    // ===== 弹匣对瞄准速度的影响（秒，正=减慢，负=加快）=====
    // 弹匣通过 GunAttachmentHolder.currentMagId 追踪（不在 attachmentIds 中）
    private static readonly Dictionary<string, float> MagazineAimTimeDelta = new(StringComparer.OrdinalIgnoreCase)
    {
        { X47MagItemSystem.ItemKey, 0.5f },                  // x47mag 50发弹鼓 减慢0.5
        { M4A1Mag560ItemSystem.ItemKey, 0.65f },             // mag560 60发弹鼓 减慢0.65
        { GlockBigStickMagItemSystem.ItemKey, 0.2f },        // glock_bigstick_mag 33发 减慢0.2（瞄准速度+0.2s）
        { GlockG50MagItemSystem.ItemKey, 1.0f },             // glock_g50_mag 50发弹鼓 减慢1.0（瞄准速度+1s）
    };

    // ===== 运行时状态 =====
    private static readonly Dictionary<Item, float> AimProgressMap = new();
    private static readonly Dictionary<Item, float> AimTimeCache = new();

    // 缓存瞄准键位（KeyBinds.GetBind 是字典查找，每帧调用有开销）
    private static KeyCode _cachedAimBind;
    private static bool _cachedAimBindSet;
    private static int _aimMapPruneCounter;
    private static int _cachedAimGunId = int.MinValue;
    private static GunScript? _cachedAimGun;

    /// <summary>清理已销毁枪械的瞄准进度/瞄准时间缓存，避免 Item 引用泄漏。</summary>
    private static void PruneDeadAimEntries()
    {
        if (AimProgressMap.Count > 0)
        {
            var dead = new List<Item>();
            foreach (var kv in AimProgressMap)
                if (kv.Key == null) dead.Add(kv.Key);
            foreach (var key in dead) AimProgressMap.Remove(key);
        }
        if (AimTimeCache.Count > 0)
        {
            var dead = new List<Item>();
            foreach (var kv in AimTimeCache)
                if (kv.Key == null) dead.Add(kv.Key);
            foreach (var key in dead) AimTimeCache.Remove(key);
        }
    }

    /// <summary>获取枪械原厂瞄准恢复时间（不含配件影响）。</summary>
    public static float GetBaseAimTime(string gunId)
    {
        if (string.IsNullOrEmpty(gunId)) return 1.5f;
        return AimTimeMap.TryGetValue(gunId, out var t) ? t : 1.5f;
    }

    /// <summary>获取单个配件对瞄准时间的影响（秒，正=减慢，负=加快）。</summary>
    public static float GetAttachmentAimTimeDelta(string attachmentId, Item? gun = null)
    {
        if (string.IsNullOrEmpty(attachmentId)) return 0f;
        if (string.Equals(attachmentId, TapcoIntrafuseItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
        {
            if (gun != null)
                return SuppressorSystem.HasAnyStock(gun)
                    ? TapcoIntrafuseItemSystem.AimTimeDeltaWithStock
                    : TapcoIntrafuseItemSystem.AimTimeDeltaNoStock;
            return TapcoIntrafuseItemSystem.AimTimeDeltaNoStock;
        }
        return AttachmentAimTimeDelta.TryGetValue(attachmentId, out var delta) ? delta : 0f;
    }

    /// <summary>获取枪械的瞄准恢复时间（基础 + 配件影响，最低 0.3 秒）。</summary>
    public static float GetAimTime(Item gunItem)
    {
        if (gunItem == null) return 1.5f;
        if (AimTimeCache.TryGetValue(gunItem, out var cached)) return cached;

        float time = AimTimeMap.TryGetValue(gunItem.id, out var baseTime) ? baseTime : 1.5f;
        var holder = gunItem.GetComponent<GunAttachmentHolder>();
        if (holder != null && holder.attachmentIds != null)
        {
            foreach (var id in holder.attachmentIds)
            {
                if (AttachmentAimTimeDelta.TryGetValue(id, out var delta))
                    time += delta;
            }
            // Tapco INTRAFUSE 动态瞄准速度：有后托 -0.44s，无后托 +0.6s
            if (holder.attachmentIds.Contains(TapcoIntrafuseItemSystem.ItemKey))
            {
                time += SuppressorSystem.HasAnyStock(gunItem)
                    ? TapcoIntrafuseItemSystem.AimTimeDeltaWithStock
                    : TapcoIntrafuseItemSystem.AimTimeDeltaNoStock;
            }
            // 弹匣影响（通过 currentMagId 追踪）
            if (!string.IsNullOrEmpty(holder.currentMagId)
                && MagazineAimTimeDelta.TryGetValue(holder.currentMagId, out var magDelta))
                time += magDelta;
        }
        time = Mathf.Max(0.3f, time);
        AimTimeCache[gunItem] = time;
        return time;
    }

    /// <summary>获取枪械当前瞄准进度（0~1）。</summary>
    public static float GetAimProgress(Item gunItem)
    {
        if (gunItem == null) return 0f;
        return AimProgressMap.TryGetValue(gunItem, out var p) ? p : 0f;
    }

    /// <summary>清空瞄准进度缓存（安装/卸下配件后调用）。</summary>
    public static void InvalidateAimTimeCache(Item gunItem)
    {
        if (gunItem != null) AimTimeCache.Remove(gunItem);
    }

    /// <summary>每帧更新瞄准进度（由 AimController 调用）。</summary>
    public static void UpdateAim(Item gunItem, bool aiming)
    {
        if (gunItem == null) return;

        if (!AimProgressMap.TryGetValue(gunItem, out var progress))
            progress = 0f;

        if (aiming)
        {
            float time = GetAimTime(gunItem);
            progress = Mathf.MoveTowards(progress, 1f, Time.deltaTime / time);
        }
        else
        {
            progress = Mathf.MoveTowards(progress, 0f, Time.deltaTime * AimDecayPerSecond);
        }

        AimProgressMap[gunItem] = progress;
    }

    /// <summary>计算开火时的有效散布倍率（供 FireEffectsPatch 使用）。</summary>
    public static float GetEffectiveSpreadMult(Item gunItem)
    {
        float progress = GetAimProgress(gunItem);
        // 未瞄准(0)→按枪械类型分级倍率，瞄准满(1)→×1.0，线性过渡
        float unaimed = GetUnaimedSpreadMult(gunItem);
        return Mathf.Lerp(unaimed, AimedSpreadMult, progress);
    }

    /// <summary>
    /// 获取枪械的腰射（未瞄准）散布倍率，按枪械类型分级。
    /// 机枪/狙击最大，步枪/半自动卡宾其次，冲锋枪再次，手枪/霰弹最小。
    /// </summary>
    public static float GetUnaimedSpreadMult(Item gunItem)
        => GetUnaimedSpreadMult(gunItem, gunItem != null ? gunItem.GetComponent<GunScript>() : null);

    public static float GetUnaimedSpreadMult(Item gunItem, GunScript gun)
    {
        if (gunItem == null || string.IsNullOrEmpty(gunItem.id)) return UnaimedMultRifle;

        // 用户指定的每枪腰射散布：乘数 = 目标腰射散布 / 枪械基础垂直散布
        if (HipFireSpreadMap.TryGetValue(gunItem.id, out var desiredHipSpread))
        {
            float baseSpread = gun != null && gun.verticalSpread > 0.001f ? gun.verticalSpread : 1f;
            float hipSpread = desiredHipSpread * SuppressorSystem.GetHipFireSpreadMult(gunItem);
            if (hipSpread < baseSpread) hipSpread = baseSpread; // 腰射精度不能超过枪械精度
            return hipSpread / baseSpread;
        }

        string id = gunItem.id;

        // 机枪/狙击枪：腰射散布最大
        if (id == RPDItemSystem.ItemKey
            || id == AXMCItemSystem.ItemKey
            || id == DVL10ItemSystem.ItemKey
            || id == VSSItemSystem.ItemKey)
            return UnaimedMultMachineGun;

        // 冲锋枪
        if (id == UMP45ItemSystem.ItemKey
            || id == P90ItemSystem.ItemKey)
            return UnaimedMultSMG;

        // 手枪/霰弹枪：腰射散布最小
        if (id == USPItemSystem.ItemKey
            || id == Glock17ItemSystem.ItemKey
            || id == DeagleItemSystem.ItemKey
            || id == MP133ItemSystem.ItemKey
            || id == MP153ItemSystem.ItemKey
            || id == AA12ItemSystem.ItemKey)
            return UnaimedMultPistolShotgun;

        // 默认（步枪/半自动卡宾枪：AKM/M4A1/SKS 等）
        return UnaimedMultRifle;
    }

    /// <summary>
    /// 每帧驱动玩家手持枪的瞄准（由 PlayerInputLockPatch.Postfix 调用）。
    /// 直接更新瞄准进度 + 准星视觉，不依赖 MonoBehaviour Update
    /// （枪在手上时 GameObject 可能不 active，Update 不执行）。
    /// </summary>
    public static void TickPlayerAim(PlayerCamera cam)
    {
        try
        {
            var body = cam != null ? cam.body : null;
            if (body == null) return;

            if (++_aimMapPruneCounter % 300 == 0) PruneDeadAimEntries();

            var handItem = body.GetItem(body.handSlot);
            int handId = handItem != null ? handItem.GetInstanceID() : int.MinValue;
            GunScript? handGun;
            if (handId == _cachedAimGunId)
            {
                handGun = _cachedAimGun;
            }
            else
            {
                handGun = handItem != null ? handItem.GetComponent<GunScript>() : null;
                _cachedAimGunId = handId;
                _cachedAimGun = handGun;
            }
            if (handItem == null || handGun == null)
            {
                // 无手持枪：隐藏准星（丢枪/换背包/空手时）
                AimCrosshair.Hide();
                return;
            }

            // 右键瞄准输入
            if (!_cachedAimBindSet) { _cachedAimBind = KeyBinds.GetBind("iteminteract"); _cachedAimBindSet = true; }
            bool aiming = Input.GetKey(_cachedAimBind);

            // 更新瞄准进度
            UpdateAim(handItem, aiming);

            // 准星视觉（实时跟踪精准度）
            AimCrosshair.SetProgress(GetAimProgress(handItem), handItem);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[Aim] TickPlayerAim failed: {ex.Message}");
        }
    }
}

// 注意：瞄准驱动统一由 AimSystem.TickPlayerAim（PlayerInputLockPatch.Postfix）处理，
// 它只针对手持枪，避免多把枪的 MonoBehaviour 竞争全局准星（AimCrosshair 是全局单例）
// 以及双重累积瞄准进度导致速度翻倍的问题。