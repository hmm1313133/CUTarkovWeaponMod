using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using CUTarkovMedicalMod.Framework;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 消音器系统：读取枪械的 GunAttachmentHolder，应用消音器效果。
///
/// Hexagon AKM 效果（安装后）：
/// - 听力损伤 -65%（loudness × 0.35）
/// - 后坐力 -1.5%（knockBack × 0.985）
/// - 每发耐久损耗 +10%（conditionLossPerShot × 1.10）
/// - 开火音效替换为消音版
///
/// 交互（无容器 UI）：
/// - 安装：把消音器拖到 AKM 上（Body.CombineItems）
/// - 卸下：右键 AKM（useAction）
/// - 存档：GunAttachmentHolder + WeaponItemSaveProvider
/// </summary>
public static class SuppressorSystem
{
    // ===== 效果参数（用户指定）=====
    private const float LoudnessMult = 0.35f;          // -65%
    private const float KnockBackMult = 0.985f;        // -1.5%
    private const float ConditionLossMult = 1.10f;     // +10%
    private const float SuppressorSpreadMult = 1f;      // 取消消音器精准度减益
    private const float SuppressorBarrelRetreat = 0.35f; // 装消音器时弹道起点往后挪（单位，≈5px PPI14）
    private const float LongBarrelMuzzleOffset = 1.14f; // 加长枪管时火光/弹道起点往右挪（单位，≈16px PPI14）

    private static AudioClip? _cachedSilencedSound;

    // 倍镜控制器缓存（ScopeZoomPatch 每帧调用 IsMagnifiedSightZoomed，避免每帧 6 次 GetComponent）
    private static int _sightZoomCachedItemId = int.MinValue;
    private static Hhs1Controller? _sightZoomHhs;
    private static SpecterDrController? _sightZoomSpec;
    private static Monstr2x32Controller? _sightZoomMonstr;
    private static Ta01nsnController? _sightZoomTa01;
    private static RazorHdController? _sightZoomRazor;
    private static Pm2Controller? _sightZoomPm2;

    public static void InvalidateSightZoomCache(Item gunItem)
    {
        if (gunItem != null && _sightZoomCachedItemId == gunItem.GetInstanceID())
            _sightZoomCachedItemId = int.MinValue;
    }

    // ===== 配件状态查询 =====

    /// <summary>检查枪械是否装有指定配件。</summary>
    public static bool IsAttachmentInstalled(Item gunItem, string attachmentId)
    {
        if (gunItem == null || string.IsNullOrEmpty(attachmentId)) return false;
        var holder = gunItem.GetComponent<GunAttachmentHolder>();
        return holder != null && holder.attachmentIds.Contains(attachmentId);
    }

    /// <summary>检查枪械是否装有消音器（快捷方式）。</summary>
    public static bool IsSuppressorAttached(Item gunItem)
        => IsAttachmentInstalled(gunItem, HexagonAKMSuppressorItemSystem.ItemKey);

    /// <summary>检查枪械是否装有任一改装护木（MOE SL 等）。</summary>
    public static bool IsHandguardInstalled(Item gunItem)
    {
        if (gunItem == null) return false;
        var holder = gunItem.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
            if (HandguardSlotIds.Contains(id)) return true;
        return false;
    }

    /// <summary>判断是否为霰弹枪（霰弹枪除枪口收束器外，其他精度增益无效）。</summary>
    public static bool IsShotgun(Item gunItem)
    {
        if (gunItem == null) return false;
        return gunItem.id == "mp133" || gunItem.id == "mp153" || gunItem.id == "aa12";
    }

    /// <summary>
    /// 计算枪械配装后的综合属性倍率（供 Shift 展开面板显示）。
    /// 返回 (后坐倍率, 精度倍率, 噪音倍率, 耐久损耗倍率)。
    /// </summary>
    public static (float knockBackMult, float spreadMult, float loudnessMult, float conditionMult) GetEffectiveStats(Item gunItem)
    {
        float kb = 1f, sp = 1f, ld = 1f, cd = 1f;
        if (gunItem == null) return (kb, sp, ld, cd);

        var holder = gunItem.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return (kb, sp, ld, cd);

        foreach (var id in holder.attachmentIds)
        {
            ApplyAttachmentStatEffects(id, gunItem, ref kb, ref sp, ref ld, ref cd);
        }
        return (kb, sp, ld, cd);
    }

    /// <summary>把单个配件的属性倍率累加到当前倍率上（供已装汇总和悬停属性预览共用）。</summary>
    private static void ApplyAttachmentStatEffects(string id, Item? gunItem, ref float kb, ref float sp, ref float ld, ref float cd)
    {
            if (id == MoeAkmItemSystem.ItemKey) { kb *= MoeAkmItemSystem.KnockBackMult; cd *= MoeAkmItemSystem.ConditionLossMult; }
            else if (id == UasSksItemSystem.ItemKey) { kb *= UasSksItemSystem.KnockBackMult; cd *= UasSksItemSystem.ConditionLossMult; }
            else if (id == TapcoIntrafuseItemSystem.ItemKey)
            {
                // Tapco 动态效果：有后托 -5%，无后托 +26%；耐久损耗 -2% 恒定
                kb *= HasAnyStock(gunItem)
                    ? TapcoIntrafuseItemSystem.KnockBackMultWithStock
                    : TapcoIntrafuseItemSystem.KnockBackMultNoStock;
                cd *= TapcoIntrafuseItemSystem.ConditionLossMult;
            }
            else if (id == MoeSlItemSystem.ItemKey) { kb *= MoeSlItemSystem.KnockBackMult; }
            else if (id == ViperItemSystem.ItemKey) { kb *= ViperItemSystem.KnockBackMult; cd *= ViperItemSystem.ConditionLossMult; }
            else if (id == SmrMk16ItemSystem.ItemKey) { cd *= SmrMk16ItemSystem.ConditionLossMult; }
            else if (id == AdarWoodItemSystem.ItemKey) { cd *= AdarWoodItemSystem.ConditionLossMult; }
            else if (id == LvoaItemSystem.ItemKey) { cd *= LvoaItemSystem.ConditionLossMult; }
            else if (id == HexagonAkHandguardItemSystem.ItemKey) { kb *= HexagonAkHandguardItemSystem.KnockBackMult; cd *= HexagonAkHandguardItemSystem.ConditionLossMult; }
            else if (id == B10mB19ItemSystem.ItemKey) { kb *= B10mB19ItemSystem.KnockBackMult; cd *= B10mB19ItemSystem.ConditionLossMult; }
            else if (id == WasrItemSystem.ItemKey) { kb *= WasrItemSystem.KnockBackMult; cd *= WasrItemSystem.ConditionLossMult; }
            else if (id == OpforAak7ItemSystem.ItemKey) { kb *= OpforAak7ItemSystem.KnockBackMult; sp *= OpforAak7ItemSystem.SpreadMult; }
            else if (id == KochergaItemSystem.ItemKey) { kb *= KochergaItemSystem.KnockBackMult; sp *= KochergaItemSystem.SpreadMult; }
            else if (id == ZhukovSItemSystem.ItemKey) { kb *= ZhukovSItemSystem.KnockBackMult; sp *= ZhukovSItemSystem.SpreadMult; }
            else if (id == Cqr47ItemSystem.ItemKey) { kb *= Cqr47ItemSystem.KnockBackMult; sp *= Cqr47ItemSystem.SpreadMult; }
            else if (id == Rk3ItemSystem.ItemKey) { kb *= Rk3ItemSystem.KnockBackMult; sp *= Rk3ItemSystem.SpreadMult; }
            else if (id == Mg47ItemSystem.ItemKey) { kb *= Mg47ItemSystem.KnockBackMult; }
            else if (id == Ags74ItemSystem.ItemKey) { kb *= Ags74ItemSystem.KnockBackMult; sp *= Ags74ItemSystem.SpreadMult; }
            else if (id == Td120001ItemSystem.ItemKey) { kb *= Td120001ItemSystem.KnockBackMult; }
            else if (id == StarkArrgItemSystem.ItemKey) { kb *= StarkArrgItemSystem.KnockBackMult; }
            else if (id == MiadItemSystem.ItemKey) { kb *= MiadItemSystem.KnockBackMult; }
            else if (id == F1st2pcItemSystem.ItemKey) { kb *= F1st2pcItemSystem.KnockBackMult; sp *= F1st2pcItemSystem.SpreadMult; }
            else if (id == ErgoItemSystem.ItemKey) { kb *= ErgoItemSystem.KnockBackMult; sp *= ErgoItemSystem.SpreadMult; }
            else if (id == Vipermod1ItemSystem.ItemKey) { kb *= Vipermod1ItemSystem.KnockBackMult; }
            else if (id == CtrItemSystem.ItemKey) { kb *= CtrItemSystem.KnockBackMult; sp *= CtrItemSystem.SpreadMult; }
            else if (id == Ds150fdeItemSystem.ItemKey) { kb *= Ds150fdeItemSystem.KnockBackMult; sp *= Ds150fdeItemSystem.SpreadMult; }
            else if (id == AcsItemSystem.ItemKey) { kb *= AcsItemSystem.KnockBackMult; }
            else if (id == MoefgItemSystem.ItemKey) { kb *= MoefgItemSystem.KnockBackMult; sp *= MoefgItemSystem.SpreadMult; }
            else if (id == MoefdeItemSystem.ItemKey) { kb *= MoefdeItemSystem.KnockBackMult; sp *= MoefdeItemSystem.SpreadMult; }
            else if (id == MoesgItemSystem.ItemKey) { kb *= MoesgItemSystem.KnockBackMult; sp *= MoesgItemSystem.SpreadMult; }
            else if (id == MrsItemSystem.ItemKey) { sp *= MrsItemSystem.SpreadMult; }
            else if (id == Eotech553ItemSystem.ItemKey) { sp *= Eotech553ItemSystem.SpreadMult; }
            else if (id == Hhs1ItemSystem.ItemKey) { sp *= Hhs1ItemSystem.SpreadMult; }
            else if (id == SpecterDrItemSystem.ItemKey) { sp *= SpecterDrItemSystem.SpreadMult; }
            else if (id == HexagonAKMSuppressorItemSystem.ItemKey) { kb *= KnockBackMult; ld *= LoudnessMult; sp *= SuppressorSpreadMult; cd *= ConditionLossMult; }
            else if (id == DynacompItemSystem.ItemKey) { kb *= DynacompItemSystem.KnockBackMult; }
            else if (id == Dtk1ItemSystem.ItemKey) { kb *= Dtk1ItemSystem.KnockBackMult; }
            else if (id == Rotor43ItemSystem.ItemKey) { kb *= Rotor43ItemSystem.KnockBackMult; ld *= Rotor43ItemSystem.LoudnessMult; cd *= Rotor43ItemSystem.ConditionLossMult; }
            else if (id == Nt4ItemSystem.ItemKey) { kb *= Nt4ItemSystem.KnockBackMult; ld *= Nt4ItemSystem.LoudnessMult; sp *= Nt4ItemSystem.SpreadMult; cd *= Nt4ItemSystem.ConditionLossMult; }
            else if (id == SakerItemSystem.ItemKey) { kb *= SakerItemSystem.KnockBackMult; ld *= SakerItemSystem.LoudnessMult; sp *= SakerItemSystem.SpreadMult; cd *= SakerItemSystem.ConditionLossMult; }
            else if (id == Kx3ItemSystem.ItemKey) { kb *= Kx3ItemSystem.KnockBackMult; cd *= Kx3ItemSystem.ConditionLossMult; }
            else if (id == Vp09ItemSystem.ItemKey) { kb *= Vp09ItemSystem.KnockBackMult; }
            else if (id == Rotor43762ItemSystem.ItemKey) { kb *= Rotor43762ItemSystem.KnockBackMult; ld *= Rotor43762ItemSystem.LoudnessMult; cd *= Rotor43762ItemSystem.ConditionLossMult; }
            else if (id == HexagonSksItemSystem.ItemKey) { kb *= HexagonSksItemSystem.KnockBackMult; ld *= HexagonSksItemSystem.LoudnessMult; sp *= HexagonSksItemSystem.SpreadMult; }
            else if (id == SksMcItemSystem.ItemKey) { kb *= SksMcItemSystem.KnockBackMult; }
            else if (id == SrvvAkmItemSystem.ItemKey) { kb *= SrvvAkmItemSystem.KnockBackMult; }
            else if (id == Dtk4mItemSystem.ItemKey) { kb *= Dtk4mItemSystem.KnockBackMult; ld *= Dtk4mItemSystem.LoudnessMult; sp *= Dtk4mItemSystem.SpreadMult; }
            else if (id == DtkpItemSystem.ItemKey) { kb *= DtkpItemSystem.KnockBackMult; ld *= DtkpItemSystem.LoudnessMult; sp *= DtkpItemSystem.SpreadMult; }
            else if (id == Ac858ItemSystem.ItemKey) { kb *= Ac858ItemSystem.KnockBackMult; }
            else if (id == HekateDt338ItemSystem.ItemKey) { kb *= HekateDt338ItemSystem.KnockBackMult; ld *= HekateDt338ItemSystem.LoudnessMult; }
            else if (id == Tmb338lmItemSystem.ItemKey) { kb *= Tmb338lmItemSystem.KnockBackMult; }
            else if (id == Tsm338lmItemSystem.ItemKey) { kb *= Tsm338lmItemSystem.KnockBackMult; ld *= Tsm338lmItemSystem.LoudnessMult; }
            else if (id == Dvl10SilencedItemSystem.ItemKey) { kb *= Dvl10SilencedItemSystem.KnockBackMult; sp *= Dvl10SilencedItemSystem.SpreadMult; }
            else if (id == UmpOemItemSystem.ItemKey) { kb *= UmpOemItemSystem.KnockBackMult; ld *= UmpOemItemSystem.LoudnessMult; }
            else if (id == AkmLItemSystem.ItemKey) { cd *= AkmLItemSystem.ConditionLossMult; }
            else if (id == PdcItemSystem.ItemKey) { cd *= PdcItemSystem.ConditionLossMult; }
            else if (id == ShiftForegripItemSystem.ItemKey) { kb *= ShiftForegripItemSystem.KnockBackMult; }
            else if (id == Se5ForegripItemSystem.ItemKey) { kb *= Se5ForegripItemSystem.KnockBackMult; }
            else if (id == Rk0ForegripItemSystem.ItemKey) { kb *= Rk0ForegripItemSystem.KnockBackMult; }
            else if (id == Rk2ForegripItemSystem.ItemKey) { kb *= Rk2ForegripItemSystem.KnockBackMult; }
            else if (id == B25ur1ForegripItemSystem.ItemKey) { kb *= B25ur1ForegripItemSystem.KnockBackMult; }
            else if (id == CobraForegripItemSystem.ItemKey) { kb *= CobraForegripItemSystem.KnockBackMult; }
            else if (id == P2ForegripItemSystem.ItemKey) { kb *= P2ForegripItemSystem.KnockBackMult; }
            else if (id == AfgForegripItemSystem.ItemKey) { kb *= AfgForegripItemSystem.KnockBackMult; }
    }

    /// <summary>获取单个配件的属性倍率（用于改枪面板悬停预览）。</summary>
    public static (float knockBackMult, float spreadMult, float loudnessMult, float conditionMult) GetAttachmentStatMults(string attachmentId, Item? gun = null)
    {
        float kb = 1f, sp = 1f, ld = 1f, cd = 1f;
        ApplyAttachmentStatEffects(attachmentId, gun, ref kb, ref sp, ref ld, ref cd);
        return (kb, sp, ld, cd);
    }

    /// <summary>
    /// 各配件对腰射散布的乘算系数（用户指定）。
    /// 正收益（更准）为 &lt;1，负收益（更散）为 &gt;1。
    /// 约束：最终腰射散布不会低于枪械基础垂直散布（腰射精度不能超过枪械精度）。
    /// </summary>
    public static float GetHipFireSpreadMult(Item gunItem)
    {
        if (gunItem == null) return 1f;
        var holder = gunItem.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return 1f;

        float mult = 1f;
        foreach (var id in holder.attachmentIds)
        {
            if (string.Equals(id, TapcoIntrafuseItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                mult *= 0.97f; // Tapco intrafuse 腰射精准度 +3%
            else if (string.Equals(id, UasSksItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                mult *= 0.94f; // UAS SKS 腰射精准度 +6%
            else if (string.Equals(id, Ags74ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                mult *= 0.95f; // AGS-74 腰射精准度 +5%
            else if (string.Equals(id, ErgoItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                mult *= 0.95f; // Ergo 腰射精准度 +5%
            else if (string.Equals(id, B25ur1ForegripItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                mult *= 0.88f; // B-25U RK-1 腰射精准度 +12%
            else if (string.Equals(id, Rk2ForegripItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                mult *= 1.022f; // RK-2 腰射精准度 -2.2%
            else if (string.Equals(id, AfgForegripItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, Se5ForegripItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                mult *= 0.965f; // AFG / SE-5 腰射精准度 +3.5%
            else if (string.Equals(id, BaldrProItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                var ctrl = gunItem.GetComponent<BaldrProController>();
                if (ctrl != null
                    && (ctrl.CurrentMode == BaldrProController.Mode.Laser || ctrl.CurrentMode == BaldrProController.Mode.Both))
                    mult *= 0.75f; // BaldrPro 激光/手电+激光模式 腰射精准度 +25%
            }
            else if (string.Equals(id, TblItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                var ctrl = gunItem.GetComponent<TblController>();
                if (ctrl != null && ctrl.IsOn)
                    mult *= 0.75f; // TBL 激光开启 腰射精准度 +25%
            }
        }
        return mult;
    }

    /// <summary>判断物品是否为可安装的枪械配件。</summary>
    public static bool IsAttachmentItem(Item item)
    {
        if (item == null) return false;
        var tags = item.Stats.GetTags();
        return tags != null && Array.IndexOf(tags, "attachment") >= 0;
    }

    // ===== 战术设备（每把枪同时只能安装一个）=====
    // 战术设备槽位互斥：LAS/TAC 2 与 Klesch-2U 都是装在护木上的手电，不能同时装两把。

    private static readonly HashSet<string> TacticalDeviceIds = new(StringComparer.OrdinalIgnoreCase)
    {
        LasTac2ItemSystem.ItemKey,
        Klesch2UItemSystem.ItemKey,
        BaldrProItemSystem.ItemKey,
        TblItemSystem.ItemKey,
    };

    // ===== 护木槽（互斥）=====
    // 占用护木槽的配件：同一把枪只能装一个护木。
    // MOE AKM 与 Hexagon AK 管状护木互斥。

    private static readonly HashSet<string> HandguardSlotIds = new(StringComparer.OrdinalIgnoreCase)
    {
        MoeAkmItemSystem.ItemKey,
        HexagonAkHandguardItemSystem.ItemKey,
        B10mB19ItemSystem.ItemKey,
        WasrItemSystem.ItemKey,
        AkmLItemSystem.ItemKey,
        MoeSlItemSystem.ItemKey,   // M4 专属护木
        ViperItemSystem.ItemKey,   // M4 专属护木
        KacRisItemSystem.ItemKey,  // M4 专属护木
        SmrMk16ItemSystem.ItemKey,   // M4 长枪管专属护木
        AdarWoodItemSystem.ItemKey,  // M4 长枪管专属护木
        LvoaItemSystem.ItemKey,      // M4 长枪管专属护木
        UasSksItemSystem.ItemKey,    // SKS 专属 UAS 套件
        TapcoIntrafuseItemSystem.ItemKey, // SKS 专属 Tapco INTRAFUSE 套件
    };

    /// <summary>判断是否为占用护木槽的配件。</summary>
    public static bool IsHandguardItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId) && HandguardSlotIds.Contains(attachmentId);

    /// <summary>判断是否为长枪管专属护木（SMR Mk.16 / 2-15木制 / LVOA-S）。</summary>
    public static bool IsLongBarrelHandguardItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId)
           && (string.Equals(attachmentId, SmrMk16ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, AdarWoodItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, LvoaItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase));

    /// <summary>枪上是否已装有占用护木槽的配件（含同型号，防止重复安装）。</summary>
    public static bool HasOtherHandguard(Item gunItem, string attachmentId)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
        {
            if (HandguardSlotIds.Contains(id))
                return true;
        }
        return false;
    }

    // ===== 后托槽（互斥）=====
    // 占用后托槽的配件：普通枪托 + 一体式枪托（CQR47 含后托）。同一把枪只能装一个后托。

    private static readonly HashSet<string> StockSlotIds = new(StringComparer.OrdinalIgnoreCase)
    {
        OpforAak7ItemSystem.ItemKey,
        KochergaItemSystem.ItemKey,
        ZhukovSItemSystem.ItemKey,
        Cqr47ItemSystem.ItemKey, // 一体式（握把+后托）
        Vipermod1ItemSystem.ItemKey,   // M4 专属后托
        CtrItemSystem.ItemKey,         // M4 专属后托
        Ds150fdeItemSystem.ItemKey,    // M4 专属后托
        AcsItemSystem.ItemKey,         // M4 专属后托
        MoefgItemSystem.ItemKey,       // M4 专属后托
        MoefdeItemSystem.ItemKey,      // M4 专属后托
        MoesgItemSystem.ItemKey,       // M4 专属后托
        SksMcItemSystem.ItemKey,       // SKS 专属 ATI Monte Carlo 枪托
    };

    /// <summary>判断是否为占用后托槽的配件。</summary>
    public static bool IsStockItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId) && StockSlotIds.Contains(attachmentId);

    /// <summary>枪上是否已装有占用后托槽的配件（含同型号，防止重复安装）。</summary>
    public static bool HasOtherStock(Item gunItem, string attachmentId)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
        {
            if (StockSlotIds.Contains(id))
                return true;
        }
        return false;
    }

    // ===== 后握把槽（互斥）=====
    // 占用后握把槽的配件：一体式枪托（如 CQR47 握把+后托一体）与独立后握把互斥，
    // 同一把枪只能装一个。

    private static readonly HashSet<string> GripSlotIds = new(StringComparer.OrdinalIgnoreCase)
    {
        Cqr47ItemSystem.ItemKey, // 一体式（握把+后托）
        Rk3ItemSystem.ItemKey,   // RK-3 独立手枪式握把
        Mg47ItemSystem.ItemKey,  // MG-47 独立手枪式握把
        Ags74ItemSystem.ItemKey, // AGS-74 独立手枪式握把
        Td120001ItemSystem.ItemKey,   // M4 专属后握把
        StarkArrgItemSystem.ItemKey,  // M4 专属后握把
        MiadItemSystem.ItemKey,       // M4 专属后握把
        AxmcGripItemSystem.ItemKey,   // AXMC 专属橡胶握把垫
        F1st2pcItemSystem.ItemKey,    // M4 专属后握把
        ErgoItemSystem.ItemKey,       // M4 专属后握把
    };

    /// <summary>判断是否为占用后握把槽的配件（一体式枪托 / 独立后握把）。</summary>
    public static bool IsGripSlotItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId) && GripSlotIds.Contains(attachmentId);

    /// <summary>枪上是否已装有占用后握把槽的配件（含同型号，防止重复安装）。</summary>
    public static bool HasOtherGripSlotItem(Item gunItem, string attachmentId)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
        {
            if (GripSlotIds.Contains(id))
                return true;
        }
        return false;
    }

    // ===== 枪口槽（互斥）=====
    // 占用枪口槽的配件：消音器等。同一把枪只能装一个枪口装置。
    // 未来制作制退器/消焰器等枪口配件时加入此集合。

    private static readonly HashSet<string> MuzzleSlotIds = new(StringComparer.OrdinalIgnoreCase)
    {
        HexagonAKMSuppressorItemSystem.ItemKey,
        DynacompItemSystem.ItemKey,
        Dtk1ItemSystem.ItemKey,
        Rotor43ItemSystem.ItemKey,   // M4 专属枪口消音器
        Nt4ItemSystem.ItemKey,       // M4 专属枪口消音器
        SakerItemSystem.ItemKey,     // M4 专属枪口消音器
        Kx3ItemSystem.ItemKey,       // M4 专属枪口消焰器
        Vp09ItemSystem.ItemKey,      // M4 专属枪口制退器
        Rotor43762ItemSystem.ItemKey, // AKM 专属枪口消音器
        HexagonSksItemSystem.ItemKey, // SKS 专属声音抑制器
        Wt0032_1ItemSystem.ItemKey,   // SKS 专属螺纹转换器
        SrvvAkmItemSystem.ItemKey,    // AKM 专属膛口制退器
        Dtk4mItemSystem.ItemKey,      // AKM 专属膛口制退器
        DtkpItemSystem.ItemKey,       // AKM 专属消音器
        Ac858ItemSystem.ItemKey,      // AXMC 专属膛口制退器
        HekateDt338ItemSystem.ItemKey, // AXMC 专属消音器
        Tmb338lmItemSystem.ItemKey,   // AXMC 专属膛口制退器
        Tsm338lmItemSystem.ItemKey,   // AXMC 专属声音抑制器
        Dvl10SilencedItemSystem.ItemKey, // DVL 专属消音枪管枪口组合
        // 格洛克枪口配件（需先装 AW螺纹枪管）
        GlockG3PortItemSystem.ItemKey, // G 3 Port 补偿器
        GlockLw9ItemSystem.ItemKey,    // LW 9 补偿器
        GlockOsprey9ItemSystem.ItemKey, // Osprey 9 抑制器
        GlockSrd9ItemSystem.ItemKey,   // SRD 9 抑制器
        // P90 专属枪口消音器
        P90AttenuatorItemSystem.ItemKey, // FN P90 Attenuator 消音器
        // UMP 专属枪口消音器
        UmpOemItemSystem.ItemKey, // B&T OEM .45 ACP UMP 消音器
    };

    // ===== 弹匣槽（供弹方式切换）=====
    // SKS 默认装 10 发弹仓改件（Direct 弹仓模式）。
    // 卸下弹仓改件后可安装 SKS-A5 弹匣（切换为 Mag 弹匣模式）。
    private static readonly HashSet<string> MagSlotIds = new(StringComparer.OrdinalIgnoreCase)
    {
        SksA5MagItemSystem.ItemKey,        // SKS-A5 20发弹匣（Mag 模式）
        SksIntegralMagItemSystem.ItemKey,  // SKS 10发弹仓改件（Direct 模式）
    };

    /// <summary>判断是否为占用弹匣槽的配件（弹仓改件/可拆卸弹匣）。</summary>
    public static bool IsMagItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId) && MagSlotIds.Contains(attachmentId);

    /// <summary>枪上是否已装有占用弹匣槽的配件（含同型号，防止重复安装）。</summary>
    public static bool HasOtherMag(Item gunItem, string attachmentId)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
        {
            if (MagSlotIds.Contains(id))
                return true;
        }
        return false;
    }

    // ===== 枪管槽（互斥）=====
    // 占用枪管槽的配件：加长枪管等。同一把枪只能装一个枪管。
    // 枪口槽与枪管槽分离：后期制作的枪口装置（消音器/制退器等）可同时
    // 安装到普通枪管和加长枪管上。

    private static readonly HashSet<string> BarrelSlotIds = new(StringComparer.OrdinalIgnoreCase)
    {
        M4LongBarrelItemSystem.ItemKey,   // M4 加长枪管
        GlockAwlwItemSystem.ItemKey,      // 格洛克 AW螺纹枪管（解锁枪口配件）
    };

    // ===== 套筒槽（互斥）=====
    // 占用套筒槽的配件：格洛克改装机匣（套筒）。同一把枪只能装一个套筒。

    private static readonly HashSet<string> SlideSlotIds = new(StringComparer.OrdinalIgnoreCase)
    {
        GlockViperCutItemSystem.ItemKey,  // Glock Viper Cut 套筒
        GlockPs9ItemSystem.ItemKey,       // Polymer80 PS9 套筒
    };

    /// <summary>判断是否为占用套筒槽的配件。</summary>
    public static bool IsSlideItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId) && SlideSlotIds.Contains(attachmentId);

    /// <summary>枪上是否已装有占用套筒槽的配件（含同型号，防止重复安装）。</summary>
    public static bool HasOtherSlide(Item gunItem, string attachmentId)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
        {
            if (SlideSlotIds.Contains(id))
                return true;
        }
        return false;
    }

    // ===== 基座槽（互斥）=====
    // 占用基座槽的配件：格洛克瞄具基座。同一把枪只能装一个基座。

    private static readonly HashSet<string> BaseSlotIds = new(StringComparer.OrdinalIgnoreCase)
    {
        GlockUm3ItemSystem.ItemKey,       // UM Tactical UM3 瞄具基座
    };

    /// <summary>判断是否为占用基座槽的配件。</summary>
    public static bool IsBaseItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId) && BaseSlotIds.Contains(attachmentId);

    /// <summary>枪上是否已装有占用基座槽的配件（含同型号，防止重复安装）。</summary>
    public static bool HasOtherBase(Item gunItem, string attachmentId)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
        {
            if (BaseSlotIds.Contains(id))
                return true;
        }
        return false;
    }

    // ===== 防尘盖槽（互斥）=====
    // 占用防尘盖槽的配件：导轨防尘盖等。同一把枪只能装一个防尘盖。

    private static readonly HashSet<string> DustCoverSlotIds = new(StringComparer.OrdinalIgnoreCase)
    {
        PdcItemSystem.ItemKey,
        Mtu017ItemSystem.ItemKey, // SKS 专属 Leapers UTG PRO MTU017 机匣基座
    };

    // ===== 瞄准镜槽（互斥）=====
    // 占用瞄准镜槽的配件：反射式瞄具等。同一把枪只能装一个瞄准镜。

    private static readonly HashSet<string> SightSlotIds = new(StringComparer.OrdinalIgnoreCase)
    {
        MrsItemSystem.ItemKey,
        Eotech553ItemSystem.ItemKey,
        Hhs1ItemSystem.ItemKey,
        SpecterDrItemSystem.ItemKey,
        Monstr2x32ItemSystem.ItemKey,
        Ta01nsnItemSystem.ItemKey,
        RazorHdItemSystem.ItemKey,
        Pm2ItemSystem.ItemKey,
        DeltaPointItemSystem.ItemKey,   // Leupold DeltaPoint 反射式瞄具
        AcroP1ItemSystem.ItemKey,       // Aimpoint ACRO P-1 反射式瞄具
    };

    /// <summary>判断是否为占用瞄准镜槽的配件。</summary>
    public static bool IsSightItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId) && SightSlotIds.Contains(attachmentId);

    /// <summary>枪上是否已安装瞄准镜配件（用于 AXMC 视野扩展判断）。</summary>
    public static bool IsSightItemInstalled(Item gunItem)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
            if (IsSightItem(id)) return true;
        return false;
    }

    /// <summary>
    /// 判断枪械原厂是否可安装瞄准镜（豁免 PDC 防尘盖前提）。
    /// 集中管理所有"原厂即可装瞄准镜"的枪械，避免逐个硬编码遗漏。
    /// 新枪原厂可装瞄准镜时，只需在此方法加一行。
    /// </summary>
    public static bool IsGunSightExempt(Item gunItem)
    {
        if (gunItem == null) return false;
        // M4A1：机匣顶部有皮卡汀尼导轨
        if (IsM4Gun(gunItem)) return true;
        // AXMC：机匣顶部有皮卡汀尼导轨
        if (IsAxmcGun(gunItem)) return true;
        // DVL-10：原厂即可安装所有瞄准镜
        if (IsDvl10Gun(gunItem)) return true;
        // UMP45：原厂可安装除 Razor HD / PM II 外的瞄准镜（具体拦截见 IsAttachmentBlockedForUmp）
        if (IsUmpGun(gunItem)) return true;
        // 沙鹰：原厂即可安装白名单瞄准镜（553/MRS/微型速瞄）
        if (IsDeagleGun(gunItem)) return true;
        return false;
    }

    /// <summary>带倍率的倍镜 ID（HHS-1/SpecterDR/Monstr/TA01NSN/RazorHD/PM II）。</summary>
    private static readonly HashSet<string> MagnifiedSightIds = new(StringComparer.OrdinalIgnoreCase)
    {
        Hhs1ItemSystem.ItemKey,
        SpecterDrItemSystem.ItemKey,
        Monstr2x32ItemSystem.ItemKey,
        Ta01nsnItemSystem.ItemKey,
        RazorHdItemSystem.ItemKey,
        Pm2ItemSystem.ItemKey,
    };

    /// <summary>枪上是否已安装带倍率的倍镜（用于 AXMC 视野扩展判断，1x 红点不放大）。</summary>
    public static bool IsMagnifiedSightInstalled(Item gunItem)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
            if (MagnifiedSightIds.Contains(id)) return true;
        return false;
    }

    /// <summary>
    /// 倍镜是否已放大（IsZoomed/Mode>0）。
    /// 用于 AXMC 视野扩展判断：1x 档位（倍镜未放大）不激活视野扩展。
    /// </summary>
    public static bool IsMagnifiedSightZoomed(Item gunItem)
    {
        if (gunItem == null) return false;

        int itemId = gunItem.GetInstanceID();
        if (itemId != _sightZoomCachedItemId)
        {
            _sightZoomCachedItemId = itemId;
            _sightZoomHhs = gunItem.GetComponent<Hhs1Controller>();
            _sightZoomSpec = gunItem.GetComponent<SpecterDrController>();
            _sightZoomMonstr = gunItem.GetComponent<Monstr2x32Controller>();
            _sightZoomTa01 = gunItem.GetComponent<Ta01nsnController>();
            _sightZoomRazor = gunItem.GetComponent<RazorHdController>();
            _sightZoomPm2 = gunItem.GetComponent<Pm2Controller>();
        }

        if (_sightZoomHhs != null && _sightZoomHhs.IsZoomed) return true;
        if (_sightZoomSpec != null && _sightZoomSpec.IsZoomed) return true;
        if (_sightZoomMonstr != null && _sightZoomMonstr.IsZoomed) return true;
        if (_sightZoomTa01 != null && _sightZoomTa01.IsZoomed) return true;
        if (_sightZoomRazor != null && _sightZoomRazor.Mode > 0) return true;
        if (_sightZoomPm2 != null && _sightZoomPm2.Mode > 0) return true;

        return false;
    }

    /// <summary>枪上是否已装有占用瞄准镜槽的配件（含同型号，防止重复安装）。</summary>
    public static bool HasOtherSight(Item gunItem, string attachmentId)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
        {
            if (SightSlotIds.Contains(id))
                return true;
        }
        return false;
    }

    /// <summary>判断是否为占用防尘盖槽的配件。</summary>
    public static bool IsDustCoverItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId) && DustCoverSlotIds.Contains(attachmentId);

    /// <summary>枪上是否已装有占用防尘盖槽的配件（含同型号，防止重复安装）。</summary>
    public static bool HasOtherDustCover(Item gunItem, string attachmentId)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
        {
            if (DustCoverSlotIds.Contains(id))
                return true;
        }
        return false;
    }

    /// <summary>判断是否为占用枪口槽的配件。</summary>
    public static bool IsMuzzleItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId) && MuzzleSlotIds.Contains(attachmentId);

    /// <summary>判断是否为占用枪管槽的配件。</summary>
    public static bool IsBarrelItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId) && BarrelSlotIds.Contains(attachmentId);

    /// <summary>枪上是否已装有占用枪管槽的配件（含同型号，防止重复安装）。</summary>
    public static bool HasOtherBarrel(Item gunItem, string attachmentId)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
        {
            if (BarrelSlotIds.Contains(id))
                return true;
        }
        return false;
    }

    /// <summary>枪上是否已装有占用枪口槽的配件（含同型号，防止重复安装）。</summary>
    public static bool HasOtherMuzzle(Item gunItem, string attachmentId)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
        {
            if (!MuzzleSlotIds.Contains(id)) continue;
            // WT0032-1 是螺纹转换器：DTK-1 等膛口装置依附于它，不算冲突
            if (string.Equals(id, Wt0032_1ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
                && IsMuzzleDeviceRequiresWt0032(attachmentId))
                continue;
            // TMB 338LM 是消焰器：TSM 338LM 声音抑制器依附于它，不算冲突
            if (string.Equals(id, Tmb338lmItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
                && string.Equals(attachmentId, Tsm338lmItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                continue;
            return true;
        }
        return false;
    }

    /// <summary>判断是否为战术设备（占用战术设备槽位）。</summary>
    public static bool IsTacticalDevice(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId) && TacticalDeviceIds.Contains(attachmentId);

    /// <summary>获取枪上当前已装的战术设备 ID；未装返回 null。</summary>
    public static string? GetInstalledTacticalDevice(Item gunItem)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return null;
        foreach (var id in holder.attachmentIds)
            if (TacticalDeviceIds.Contains(id)) return id;
        return null;
    }

    /// <summary>
    /// 枪上是否已装有战术设备（含同型号，防止重复安装）。
    /// 用于面板禁用按钮与安装拦截。
    /// </summary>
    public static bool HasOtherTacticalDevice(Item gunItem, string attachmentId)
    {
        var installed = GetInstalledTacticalDevice(gunItem);
        return installed != null;
    }

    // ===== 前握把槽（互斥）=====
    // 占用前握把槽的配件：垂直前握把等。同一把枪只能装一个前握把。

    private static readonly HashSet<string> ForegripSlotIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ShiftForegripItemSystem.ItemKey,
        Se5ForegripItemSystem.ItemKey,
        Rk0ForegripItemSystem.ItemKey,
        Rk2ForegripItemSystem.ItemKey,
        B25ur1ForegripItemSystem.ItemKey,
        CobraForegripItemSystem.ItemKey,
        P2ForegripItemSystem.ItemKey,
        AfgForegripItemSystem.ItemKey,
    };

    /// <summary>判断是否为占用前握把槽的配件。</summary>
    public static bool IsForegripItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId) && ForegripSlotIds.Contains(attachmentId);

    // ===== AKM 专属配件 =====
    // 以下配件（护木、后托、后握把、防尘盖）只为 AKM 设计，其他枪械无法安装。
    // 前握把、战术设备、瞄准镜、枪口等为通用配件，不在此列。

    private static readonly HashSet<string> AkmOnlySlotIds = new(StringComparer.OrdinalIgnoreCase);

    static SuppressorSystem()
    {
        foreach (var id in HandguardSlotIds)
            if (!IsM4OnlyItem(id) && !IsSksOnlyItem(id))
                AkmOnlySlotIds.Add(id); // M4 专属护木（MOE SL/Viper/长枪管专属）与 SKS 专属（UAS）不属于 AKM 专属
        foreach (var id in StockSlotIds)
            if (!IsM4OnlyItem(id))
                AkmOnlySlotIds.Add(id); // M4 专属后托（Viper Mod.1/CTR/DS150/ACS/MOE）不属于 AKM 专属
        foreach (var id in GripSlotIds)
            if (!IsM4OnlyItem(id) && !IsAxmcOnlyItem(id))
                AkmOnlySlotIds.Add(id); // M4 专属后握把（TD120001/Stark/MIAD/F1/Ergo）和 AXMC 专属握把垫不属于 AKM 专属
        foreach (var id in DustCoverSlotIds) AkmOnlySlotIds.Add(id);
        // 膛口制退器（Dynacomp / DTK-1 / SRVV / DTK-4M / DTKP）仅 AKM 可用
        AkmOnlySlotIds.Add(DynacompItemSystem.ItemKey);
        AkmOnlySlotIds.Add(Dtk1ItemSystem.ItemKey);
        AkmOnlySlotIds.Add(SrvvAkmItemSystem.ItemKey);
        AkmOnlySlotIds.Add(Dtk4mItemSystem.ItemKey);
        AkmOnlySlotIds.Add(DtkpItemSystem.ItemKey);
    }

    /// <summary>判断是否为 AKM 专属配件（护木/后托/后握把）。</summary>
    public static bool IsAkmOnlyItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId) && AkmOnlySlotIds.Contains(attachmentId);

    /// <summary>判断枪是否为 AKM 系。</summary>
    public static bool IsAkmGun(Item gunItem)
        => gunItem != null
           && gunItem.id.IndexOf("akm", StringComparison.OrdinalIgnoreCase) >= 0;

    /// <summary>判断枪是否为 M4A1。</summary>
    public static bool IsM4Gun(Item gunItem)
        => gunItem != null
           && string.Equals(gunItem.id, M4A1ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>判断枪是否为 SKS。</summary>
    public static bool IsSksGun(Item gunItem)
        => gunItem != null
           && string.Equals(gunItem.id, SKSItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>判断枪是否为 AXMC。</summary>
    public static bool IsAxmcGun(Item gunItem)
        => gunItem != null
           && string.Equals(gunItem.id, AXMCItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>判断枪是否为沙鹰（Desert Eagle）。</summary>
    public static bool IsDeagleGun(Item gunItem)
        => gunItem != null
           && string.Equals(gunItem.id, DeagleItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>判断枪是否为格洛克（Glock 17）。</summary>
    public static bool IsGlockGun(Item gunItem)
        => gunItem != null
           && string.Equals(gunItem.id, Glock17ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>判断枪是否为 P90。</summary>
    public static bool IsP90Gun(Item gunItem)
        => gunItem != null
           && string.Equals(gunItem.id, P90ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>判断枪是否为 UMP45。</summary>
    public static bool IsUmpGun(Item gunItem)
        => gunItem != null
           && string.Equals(gunItem.id, UMP45ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>判断是否为 P90 专属配件（Attenuator 消音器）。</summary>
    public static bool IsP90OnlyItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId)
           && string.Equals(attachmentId, P90AttenuatorItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// P90 限制：原厂只能改装枪口（Attenuator 消音器）。
    /// 其他配件（瞄准镜/护木/后托/握把/防尘盖/弹匣槽等）P90 不可装。
    /// </summary>
    public static bool IsAttachmentBlockedForP90(Item gun, string attachmentId)
    {
        if (!IsP90Gun(gun)) return false;
        // P90 专属枪口消音器直接放行
        if (IsP90OnlyItem(attachmentId)) return false;
        // 其他配件 P90 不可装
        return true;
    }

    /// <summary>判断是否为 UMP 专属配件（B&T OEM .45 ACP UMP 消音器）。</summary>
    public static bool IsUmpOnlyItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId)
           && string.Equals(attachmentId, UmpOemItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// UMP 限制：原厂可安装前握把、枪口（UMP OEM）、战术设备（手电/激光），
    /// 以及除 Razor HD / PM II 之外的瞄准镜。
    /// </summary>
    public static bool IsAttachmentBlockedForUmp(Item gun, string attachmentId)
    {
        if (!IsUmpGun(gun)) return false;
        // UMP 专属消音器直接放行
        if (IsUmpOnlyItem(attachmentId)) return false;
        // 前握把/战术设备：原厂即可安装
        if (IsForegripItem(attachmentId) || IsTacticalDevice(attachmentId)) return false;
        // 瞄准镜：除 Razor HD 和 PM II 外均可安装
        if (IsSightItem(attachmentId))
            return string.Equals(attachmentId, RazorHdItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(attachmentId, Pm2ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
        // 其他枪口/护木/后托/握把/防尘盖/弹匣槽等 UMP 不可装
        return true;
    }

    /// <summary>判断是否为格洛克专属配件（套筒/基座/枪管/枪口）。</summary>
    public static bool IsGlockOnlyItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId)
           && (string.Equals(attachmentId, GlockViperCutItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, GlockPs9ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, GlockUm3ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, GlockAwlwItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, GlockG3PortItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, GlockLw9ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, GlockOsprey9ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, GlockSrd9ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 格洛克限制：原厂只可安装战术手电（战术设备）。
    /// 套筒（Viper Cut/PS9）、基座（UM3）、枪管（AW螺纹）可直接安装。
    /// 枪口配件需要先装 AW螺纹枪管。
    /// 瞄准镜需要先装 UM3 基座，且仅白名单（553/MRS/微型速瞄）。
    /// </summary>
    public static bool IsAttachmentBlockedForGlock(Item gun, string attachmentId)
    {
        if (!IsGlockGun(gun)) return false;
        // 格洛克专属配件（套筒/基座/枪管）直接放行
        if (IsGlockOnlyItem(attachmentId))
        {
            // 枪口配件需要先装 AW螺纹枪管
            if (IsMuzzleItem(attachmentId))
                return !IsAttachmentInstalled(gun, GlockAwlwItemSystem.ItemKey);
            return false;
        }
        // 瞄准镜：需要先装 UM3 基座，且仅白名单（553/MRS/微型速瞄）
        if (IsSightItem(attachmentId))
        {
            if (!IsAttachmentInstalled(gun, GlockUm3ItemSystem.ItemKey)) return true;
            return !IsSightAllowedForUas(attachmentId);
        }
        // 战术设备：原厂即可安装（格洛克原厂只可安装战术手电）
        if (IsTacticalDevice(attachmentId))
            return false;
        // 其他配件（护木/后托/握把/防尘盖/弹匣槽等）格洛克不可装
        return true;
    }

    /// <summary>判断枪是否为 DVL-10。</summary>
    public static bool IsDvl10Gun(Item gunItem)
        => gunItem != null
           && string.Equals(gunItem.id, DVL10ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>判断是否为 DVL 专属配件（DVL-10 消音枪管枪口组合）。</summary>
    public static bool IsDvl10OnlyItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId)
           && string.Equals(attachmentId, Dvl10SilencedItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// DVL-10 限制：原厂即可安装所有瞄准镜和战术设备，也可安装 M4 系列后握把。
    /// 装了 DVL-10 消音套件后不可安装战术设备（互斥），且装消音套件时挤下已装的战术设备。
    /// </summary>
    public static bool IsAttachmentBlockedForDvl10(Item gun, string attachmentId)
    {
        if (!IsDvl10Gun(gun)) return false;
        // DVL-10 无前握把导轨：不可安装前握把
        if (IsForegripItem(attachmentId)) return true;
        // 装了消音套件后不可安装战术设备（互斥）
        if (IsAttachmentInstalled(gun, Dvl10SilencedItemSystem.ItemKey)
            && IsTacticalDevice(attachmentId))
            return true;
        return false;
    }

    /// <summary>
    /// 沙鹰限制：原厂即可安装瞄准镜和战术设备。
    /// 瞄准镜只能安装白名单（553全息 / MRS / 微型速瞄），与 UAS 白名单一致。
    /// </summary>
    public static bool IsAttachmentBlockedForDeagle(Item gun, string attachmentId)
    {
        if (!IsDeagleGun(gun)) return false;
        // 瞄准镜：仅白名单（553/MRS/微型速瞄）
        if (IsSightItem(attachmentId))
            return !IsSightAllowedForUas(attachmentId);
        return false;
    }

    /// <summary>判断是否为 AXMC 专属配件（AC-858 / Hekate DT / TMB 338LM / TSM 338LM / 握把垫）。</summary>
    public static bool IsAxmcOnlyItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId)
           && (string.Equals(attachmentId, Ac858ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, HekateDt338ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, Tmb338lmItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, Tsm338lmItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, AxmcGripItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// AXMC 限制：原厂即可安装瞄准镜、手电、握把垫、枪口。
    /// TSM 338LM 声音抑制器需要先装 TMB 338LM 膛口制退器（类似 SKS 转接器效果）。
    /// </summary>
    public static bool IsAttachmentBlockedForAxmc(Item gun, string attachmentId)
    {
        if (!IsAxmcGun(gun)) return false;
        // TSM 338LM 需要 TMB 338LM 前提
        if (string.Equals(attachmentId, Tsm338lmItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            return !IsAttachmentInstalled(gun, Tmb338lmItemSystem.ItemKey);
        return false;
    }

    /// <summary>判断是否为 SKS 专属配件（UAS / Tapco INTRAFUSE / Hexagon SKS / WT0032-1 / SKS MC / MTU017）。</summary>
    public static bool IsSksOnlyItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId)
           && (string.Equals(attachmentId, UasSksItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, TapcoIntrafuseItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, HexagonSksItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, Wt0032_1ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, SksMcItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, Mtu017ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, SksA5MagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, SksIntegralMagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// SKS 原厂限制：原厂 SKS 不可安装护木、瞄准镜、前后握把、手电、枪口装置。
    /// 装了 UAS 套件后可安装手电和瞄准镜（仅 553/MRS/微型速瞄），前握把仍不可装（UAS 无下导轨）。
    /// 装了 Tapco INTRAFUSE 套件后可安装前握把和 M4 系列后托，不可安装手电和瞄准镜。
    /// 装了 WT0032-1 螺纹转换器后可安装枪口装置（DTK-1 等）。
    /// 装了 SKS MC 枪托后无附加安装位（不可安装手电、握把、瞄准镜、护木、M4后托等
    /// 依附于枪托的配件），但枪口装置等不依附于枪托的配件仍可安装。
    /// </summary>
    public static bool IsAttachmentBlockedForSks(Item gun, string attachmentId)
    {
        if (!IsSksGun(gun)) return false;
        // 已装 UAS / Tapco / WT0032-1 / SKS MC 套件
        bool hasUas = IsAttachmentInstalled(gun, UasSksItemSystem.ItemKey);
        bool hasTapco = IsAttachmentInstalled(gun, TapcoIntrafuseItemSystem.ItemKey);
        bool hasWt0032 = IsAttachmentInstalled(gun, Wt0032_1ItemSystem.ItemKey);
        bool hasSksMc = IsAttachmentInstalled(gun, SksMcItemSystem.ItemKey);

        // SKS MC 枪托：无附加安装位，拦截依附于枪托的配件
        // （战术设备/前握把/瞄准镜/护木/M4后托），枪口装置等不依附于枪托的配件放行
        if (hasSksMc
            && !string.Equals(attachmentId, SksMcItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
            && (IsTacticalDevice(attachmentId)
                || IsForegripItem(attachmentId)
                || IsSightItem(attachmentId)
                || IsHandguardItem(attachmentId)
                || (IsStockItem(attachmentId) && IsM4OnlyItem(attachmentId))))
            return true;

        // MTU017 机匣基座：仅 SKS 原厂/SKS MC/Tapco 可装，UAS 不可装（互斥）
        if (string.Equals(attachmentId, Mtu017ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            return hasUas; // 装了 UAS 则不可装 MTU017
        // UAS 套件：装了 MTU017 则不可装 UAS（互斥）
        if (string.Equals(attachmentId, UasSksItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
            && IsAttachmentInstalled(gun, Mtu017ItemSystem.ItemKey))
            return true;

        // 瞄准镜：原厂不可装；装了 UAS 后仅白名单（553/MRS/微型速瞄）可装；Tapco 后不可装；
        // 装了 MTU017 后所有瞄准镜可装
        if (IsSightItem(attachmentId))
        {
            if (IsAttachmentInstalled(gun, Mtu017ItemSystem.ItemKey)) return false; // MTU017 解锁所有瞄准镜
            if (!hasUas) return true; // 原厂/Tapco 不可装瞄准镜
            return !IsSightAllowedForUas(attachmentId); // UAS 后仅白名单
        }
        // 手电（战术设备）：原厂不可装；装了 UAS 后可装；Tapco 后不可装
        if (IsTacticalDevice(attachmentId))
            return !hasUas;
        // 前握把：原厂不可装；UAS 无下导轨不可装；装了 Tapco 后可装
        if (IsForegripItem(attachmentId))
            return !hasTapco;
        // M4 系列后托：原厂不可装；装了 Tapco 后可装（Tapco 有缓冲管接口）
        if (IsStockItem(attachmentId) && IsM4OnlyItem(attachmentId))
            return !hasTapco;
        // 枪口装置：Hexagon SKS 直接可装（SKS 专属消音器，无需转换器）；
        // DTK-1 等 AKM 膛口装置需要 WT0032-1（WT0032-1 本身是转换器，安装时不受此限制）
        if (IsMuzzleItem(attachmentId)
            && !string.Equals(attachmentId, Wt0032_1ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(attachmentId, HexagonSksItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                return false; // Hexagon SKS 直接可装
            return !hasWt0032; // DTK-1 等需要 WT0032-1
        }
        // 护木：原厂不可装（UAS/Tapco 本身是护木，安装时不受此限制，见 AttachToGun 处理）
        if (IsHandguardItem(attachmentId)
            && !string.Equals(attachmentId, UasSksItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(attachmentId, TapcoIntrafuseItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    /// <summary>
    /// 判断枪口装置是否需要 WT0032-1 螺纹转换器（DTK-1 等 AKM 膛口装置）。
    /// 排除：WT0032-1 本身、Hexagon SKS（SKS 专属消音器，直接装枪口）。
    /// </summary>
    public static bool IsMuzzleDeviceRequiresWt0032(string attachmentId)
        => IsMuzzleItem(attachmentId)
           && !string.Equals(attachmentId, Wt0032_1ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
           && !string.Equals(attachmentId, HexagonSksItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>UAS 套件允许安装的瞄准镜白名单（553全息 / MRS / 微型速瞄）。</summary>
    public static bool IsSightAllowedForUas(string attachmentId)
    {
        foreach (var s in UasSksItemSystem.AllowedSights)
            if (string.Equals(s, attachmentId, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    /// <summary>
    /// M4A1 初始限制：无法安装战术设备和前握把（原厂护木无下导轨），
    /// 但可直接安装瞄准镜（机匣顶部有皮卡汀尼导轨）。
    /// 装了带 M-LOK 接口的护木（MOE SL/Viper/KAC RIS/SMR/2-15/LVOA-S）后可安装战术设备和前握把。
    /// 注意：2-15木制护木不可安装握把和战术设备（在 AttachToGun 单独拦截）。
    /// </summary>
    public static bool IsAttachmentBlockedForM4(Item gun, string attachmentId)
    {
        if (!IsM4Gun(gun)) return false;
        // 战术设备（手电/激光）与前握把：装了带 M-LOK 接口的护木后允许
        if (IsTacticalDevice(attachmentId) || IsForegripItem(attachmentId))
        {
            // 2-15木制护木不可安装握把和战术设备
            if (IsAttachmentInstalled(gun, AdarWoodItemSystem.ItemKey))
                return true;
            return !HasAnyHandguardExceptWasr(gun);
        }
        return false;
    }

    /// <summary>判断是否为 M4 专属配件（MOE SL / Viper / KAC RIS / 长枪管专属护木 / 加长枪管仅 M4 可装）。</summary>
    public static bool IsM4OnlyItem(string attachmentId)
        => !string.IsNullOrEmpty(attachmentId)
           && (string.Equals(attachmentId, MoeSlItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, ViperItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, KacRisItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, SmrMk16ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, AdarWoodItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, LvoaItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, M4LongBarrelItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, Rotor43ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, Nt4ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, SakerItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, Kx3ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, Vp09ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, Td120001ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, StarkArrgItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, MiadItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, F1st2pcItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, ErgoItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, Vipermod1ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, CtrItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, Ds150fdeItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, AcsItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, MoefgItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, MoefdeItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(attachmentId, MoesgItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase));

    /// <summary>枪上是否已装有占用前握把槽的配件（含同型号，防止重复安装）。</summary>
    public static bool HasOtherForegrip(Item gunItem, string attachmentId)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
        {
            if (ForegripSlotIds.Contains(id))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 前握把的安装前提（条件化）：仅 AKM 系枪需要先装改装护木（任一非 WASR 护木），
    /// 其他枪（如 M4A1 等原厂带下导轨）无前提。
    /// WASR 护木特殊：装 WASR 后不可装前握把，故不算满足前提。
    /// 返回缺失的前提配件 ID 列表（已满足返回空列表）。
    /// </summary>
    public static List<string> GetForegripMissingPrereq(Item gunItem, string attachmentId)
    {
        var missing = new List<string>();
        if (!IsForegripItem(attachmentId)) return missing;
        if (gunItem == null) return missing;
        // 仅 AKM 系枪需要改装护木
        if (gunItem.id.IndexOf("akm", StringComparison.OrdinalIgnoreCase) < 0) return missing;
        if (!HasAnyHandguardExceptWasr(gunItem))
        {
            // 列出所有可选 AKM 护木（与战术设备的 OR 前提组保持一致）
            missing.Add(MoeAkmItemSystem.ItemKey);
            missing.Add(HexagonAkHandguardItemSystem.ItemKey);
            missing.Add(AkmLItemSystem.ItemKey);
        }
        return missing;
    }

    /// <summary>
    /// 判断配件是否为"附属配件"（护木下的手电/激光/前握把）。
    /// 仅这些视为附属配件并分组标示；瞄准镜等虽依赖 PDC，但视为主配件。
    /// </summary>
    public static bool IsDependentAttachment(string attachmentId)
    {
        return IsForegripItem(attachmentId) || IsTacticalDevice(attachmentId);
    }

    /// <summary>
    /// 返回配件 attachmentId 依附的已装主配件 ID（如手电→护木、前握把→护木）。
    /// 若该配件是主配件（不依附任何已装配件）则返回 null。
    /// </summary>
    public static string? GetParentAttachmentId(Item gunItem, string attachmentId)
    {
        if (gunItem == null) return null;
        var holder = gunItem.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return null;

        // 前握把 / 战术设备：依附于已装的非 WASR 护木
        if (IsForegripItem(attachmentId) || IsTacticalDevice(attachmentId))
        {
            foreach (var id in holder.attachmentIds)
            {
                if (HandguardSlotIds.Contains(id)
                    && !string.Equals(id, WasrItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                    return id;
            }
            return null;
        }

        // SKS 装了 UAS：白名单瞄准镜依附于 UAS（UAS 自带导轨，替代 PDC 前提）
        if (IsSksGun(gunItem)
            && IsSightItem(attachmentId)
            && IsSightAllowedForUas(attachmentId)
            && holder.attachmentIds.Contains(UasSksItemSystem.ItemKey))
            return UasSksItemSystem.ItemKey;

        // SKS 装了 Tapco：M4 后托依附于 Tapco（Tapco 有缓冲管接口）
        if (IsSksGun(gunItem)
            && IsStockItem(attachmentId)
            && IsM4OnlyItem(attachmentId)
            && holder.attachmentIds.Contains(TapcoIntrafuseItemSystem.ItemKey))
            return TapcoIntrafuseItemSystem.ItemKey;

        // SKS 装了 WT0032-1：枪口装置（DTK-1 等）依附于 WT0032-1（螺纹转换器）
        // 排除 Hexagon SKS（SKS 专属消音器，直接装枪口，不依附于转换器）
        if (IsSksGun(gunItem)
            && IsMuzzleDeviceRequiresWt0032(attachmentId)
            && holder.attachmentIds.Contains(Wt0032_1ItemSystem.ItemKey))
            return Wt0032_1ItemSystem.ItemKey;

        // AXMC 装了 TMB 338LM：TSM 338LM 声音抑制器依附于 TMB（类似 SKS 转接器效果）
        if (string.Equals(attachmentId, Tsm338lmItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
            && holder.attachmentIds.Contains(Tmb338lmItemSystem.ItemKey))
            return Tmb338lmItemSystem.ItemKey;

        // AND 前提
        var prereqs = ToolSystem.GetPrerequisites(attachmentId);
        if (prereqs != null)
        {
            foreach (var p in prereqs)
                if (holder.attachmentIds.Contains(p)) return p;
        }

        // OR 前提组
        if (ToolSystem.OrPrerequisiteGroups.TryGetValue(attachmentId, out var alts))
        {
            foreach (var a in alts)
                if (holder.attachmentIds.Contains(a)) return a;
        }

        return null;
    }

    /// <summary>枪上是否已装任一护木（排除 WASR，因 WASR 不可装战术设备/前握把）。</summary>
    public static bool HasAnyHandguardExceptWasr(Item gunItem)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
        {
            if (!HandguardSlotIds.Contains(id)) continue;
            if (string.Equals(id, WasrItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)) continue;
            return true;
        }
        return false;
    }

    /// <summary>枪上是否已装任一护木。</summary>
    public static bool HasAnyHandguard(Item gunItem)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
            if (HandguardSlotIds.Contains(id)) return true;
        return false;
    }

    /// <summary>枪上是否已装任一后托（用于 Tapco INTRAFUSE 动态效果判断）。</summary>
    public static bool HasAnyStock(Item gunItem)
    {
        var holder = gunItem?.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
            if (StockSlotIds.Contains(id)) return true;
        return false;
    }

    // ===== 安装 / 卸下 =====

    /// <summary>把配件安装到枪上（追加到列表末尾）。成功返回 true。</summary>
    public static bool AttachToGun(Item gun, Item attachment, bool dryRun = false)
    {
        try
        {
            InvalidateSightZoomCache(gun);
            // SKS 专属配件（UAS/Tapco/SKS MC/Hexagon SKS/WT0032-1）：仅 SKS 可安装，其他枪械拦截
            if (IsSksOnlyItem(attachment.id) && !IsSksGun(gun))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing SKS-only item '{attachment.id}' on non-SKS gun.");
                return false;
            }

            // AXMC 专属配件（AC-858/Hekate/TMB/TSM/握把垫）：仅 AXMC 可安装，其他枪械拦截
            if (IsAxmcOnlyItem(attachment.id) && !IsAxmcGun(gun))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing AXMC-only item '{attachment.id}' on non-AXMC gun.");
                return false;
            }

            // DVL 专属配件（DVL-10 消音套件）：仅 DVL 可安装，其他枪械拦截
            if (IsDvl10OnlyItem(attachment.id) && !IsDvl10Gun(gun))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing DVL-only item '{attachment.id}' on non-DVL gun.");
                return false;
            }

            // 格洛克专属配件（套筒/基座/枪管/枪口）：仅格洛克可安装，其他枪械拦截
            if (IsGlockOnlyItem(attachment.id) && !IsGlockGun(gun))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing Glock-only item '{attachment.id}' on non-Glock gun.");
                return false;
            }

            // P90 专属配件（Attenuator 消音器）：仅 P90 可安装，其他枪械拦截
            if (IsP90OnlyItem(attachment.id) && !IsP90Gun(gun))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing P90-only item '{attachment.id}' on non-P90 gun.");
                return false;
            }

            // UMP 专属配件（B&T OEM .45 ACP UMP 消音器）：仅 UMP 可安装，其他枪械拦截
            if (IsUmpOnlyItem(attachment.id) && !IsUmpGun(gun))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing UMP-only item '{attachment.id}' on non-UMP gun.");
                return false;
            }

            // AKM 专属配件（护木/后托/后握把）：仅 AKM 系枪可安装，其他枪械拦截
            // 例外1：SKS 装了 WT0032-1 螺纹转换器后可安装 DTK-1 等膛口装置
            // 例外2：SKS 专属配件（UAS/Tapco/SKS MC/Hexagon SKS/WT0032-1）不受 AKM 专属限制
            if (IsAkmOnlyItem(attachment.id) && !IsAkmGun(gun))
            {
                bool sksWt0032Muzzle = IsSksGun(gun)
                    && IsMuzzleDeviceRequiresWt0032(attachment.id)
                    && IsAttachmentInstalled(gun, Wt0032_1ItemSystem.ItemKey);
                bool sksOnlyExempt = IsSksGun(gun) && IsSksOnlyItem(attachment.id);
                if (!sksWt0032Muzzle && !sksOnlyExempt)
                {
                    Plugin.Log.LogInfo($"[Attachment] Blocked installing AKM-only item '{attachment.id}' on non-AKM gun.");
                    return false;
                }
            }

            // M4 专属配件（MOE SL 护木）：仅 M4 可安装，其他枪械拦截
            // 例外：SKS 装了 Tapco INTRAFUSE 后可安装 M4 系列后托（Tapco 有缓冲管接口）
            if (IsM4OnlyItem(attachment.id) && !IsM4Gun(gun))
            {
                bool sksTapcoStock = IsSksGun(gun)
                    && IsStockItem(attachment.id)
                    && IsAttachmentInstalled(gun, TapcoIntrafuseItemSystem.ItemKey);
                if (!sksTapcoStock)
                {
                    Plugin.Log.LogInfo($"[Attachment] Blocked installing M4-only item '{attachment.id}' on non-M4 gun.");
                    return false;
                }
            }

            // M4A1 初始限制：无法安装战术设备和前握把（原厂护木无下导轨）
            if (IsAttachmentBlockedForM4(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing '{attachment.id}' on M4A1 (no under-rail on stock handguard).");
                return false;
            }

            // SKS 原厂限制：原厂 SKS 不可装护木/瞄准镜/前后握把/手电；装 UAS 后仅白名单瞄准镜+手电
            if (IsAttachmentBlockedForSks(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing '{attachment.id}' on SKS (stock or UAS restriction).");
                return false;
            }

            // AXMC 限制：TSM 338LM 需要先装 TMB 338LM
            if (IsAttachmentBlockedForAxmc(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing '{attachment.id}' on AXMC (requires TMB 338LM).");
                return false;
            }

            // 沙鹰限制：瞄准镜仅白名单（553/MRS/微型速瞄）
            if (IsAttachmentBlockedForDeagle(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing '{attachment.id}' on Deagle (sight whitelist).");
                return false;
            }

            // 格洛克限制：原厂只可装战术手电；枪口需 AW螺纹枪管；瞄准镜需 UM3 基座
            if (IsAttachmentBlockedForGlock(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing '{attachment.id}' on Glock (stock/UM3/AW螺纹 restriction).");
                return false;
            }

            // P90 限制：原厂只能改装枪口（Attenuator 消音器）
            if (IsAttachmentBlockedForP90(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing '{attachment.id}' on P90 (muzzle-only restriction).");
                return false;
            }

            // UMP 限制：原厂可装前握把/枪口/手电/瞄准镜（Razor HD、PM II 除外）
            if (IsAttachmentBlockedForUmp(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing '{attachment.id}' on UMP (stock UMP restriction).");
                return false;
            }

            // DVL 限制：装消音套件后不可装战术设备（互斥）
            if (IsAttachmentBlockedForDvl10(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing '{attachment.id}' on DVL (silenced kit tactical device conflict).");
                return false;
            }

            // DVL 装消音套件时：挤下已装的战术设备（消音枪管与战术设备互斥）
            // 用 DetachSingle 完整卸下（生成物品 + 销毁控制器），而非仅移除列表
            if (IsDvl10Gun(gun)
                && string.Equals(attachment.id, Dvl10SilencedItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                var dvlHolder = gun.GetComponent<GunAttachmentHolder>();
                if (dvlHolder != null && dvlHolder.attachmentIds != null)
                {
                    var tacticalToRemove = new List<string>();
                    foreach (var id in dvlHolder.attachmentIds)
                        if (IsTacticalDevice(id)) tacticalToRemove.Add(id);
                    if (!dryRun)
                    {
                        foreach (var id in tacticalToRemove)
                        {
                            DetachSingle(gun, id); // 完整卸下：生成物品 + 销毁控制器
                            Plugin.Log.LogInfo($"[Attachment] DVL silenced kit pushed off tactical device '{id}'.");
                        }
                    }
                }
            }

            // 战术设备互斥：每把枪同时只能装一个战术设备
            if (IsTacticalDevice(attachment.id) && HasOtherTacticalDevice(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing tactical device '{attachment.id}': gun already has one.");
                return false;
            }

            // 后握把槽互斥：一体式枪托与独立后握把不能同装
            if (IsGripSlotItem(attachment.id) && HasOtherGripSlotItem(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing grip-slot item '{attachment.id}': gun already has one.");
                return false;
            }

            // 护木槽互斥：一把枪只能装一个护木
            if (IsHandguardItem(attachment.id) && HasOtherHandguard(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing handguard '{attachment.id}': gun already has one.");
                return false;
            }

            // 后托槽互斥：一把枪只能装一个后托
            if (IsStockItem(attachment.id) && HasOtherStock(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing stock '{attachment.id}': gun already has one.");
                return false;
            }

            // 枪口槽互斥：一把枪只能装一个枪口装置
            if (IsMuzzleItem(attachment.id) && HasOtherMuzzle(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing muzzle '{attachment.id}': gun already has one.");
                return false;
            }

            // 枪管槽互斥：一把枪只能装一个枪管（普通枪管为默认，加长枪管占用此槽）
            if (IsBarrelItem(attachment.id) && HasOtherBarrel(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing barrel '{attachment.id}': gun already has one.");
                return false;
            }

            // 套筒槽互斥：一把枪只能装一个套筒（格洛克改装机匣）
            if (IsSlideItem(attachment.id) && HasOtherSlide(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing slide '{attachment.id}': gun already has one.");
                return false;
            }

            // 基座槽互斥：一把枪只能装一个基座（格洛克瞄具基座）
            if (IsBaseItem(attachment.id) && HasOtherBase(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing base '{attachment.id}': gun already has one.");
                return false;
            }

            // 弹匣槽互斥：弹仓改件与可拆卸弹匣互斥（一把枪只能装一个供弹方式）
            if (IsMagItem(attachment.id) && HasOtherMag(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing mag '{attachment.id}': gun already has a mag/feed device.");
                return false;
            }

            // SKS 特定守卫：SKS 仍装有 10 发弹仓改件时，禁止安装 SKS-A5 弹匣
            // （必须先卸下弹仓改件，枪才会变 Mag 模式，才能装弹匣）
            if (string.Equals(attachment.id, SksA5MagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
                && IsAttachmentInstalled(gun, SksIntegralMagItemSystem.ItemKey))
            {
                Plugin.Log.LogInfo($"[SKS-A5] Blocked installing mag: 10-round integral mag still installed.");
                return false;
            }

            // 更换枪管（安装加长枪管）时，护木和枪口装置会掉下来：
            // 加长枪管是单枪管（无护木），且枪口需重新适配，故级联卸下护木与枪口。
            if (IsBarrelItem(attachment.id) && IsM4Gun(gun))
            {
                var existingHolder = gun.GetComponent<GunAttachmentHolder>();
                if (existingHolder != null)
                {
                    var toDetach = new List<string>();
                    foreach (var id in existingHolder.attachmentIds)
                    {
                        // 普通护木（MOE SL/Viper）与长枪管冲突，掉落；
                        // 长枪管专属护木（SMR/2-15/LVOA-S）保留
                        if (IsHandguardItem(id) && !IsLongBarrelHandguardItem(id))
                            toDetach.Add(id);
                        // 枪口装置：更换枪管时掉落（需重新安装）
                        if (IsMuzzleItem(id))
                            toDetach.Add(id);
                    }
                    if (!dryRun)
                    {
                        foreach (var id in toDetach)
                            DetachCascade(gun, id);
                    }
                }
            }

            // 防尘盖槽互斥：一把枪只能装一个防尘盖
            if (IsDustCoverItem(attachment.id) && HasOtherDustCover(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing dust cover '{attachment.id}': gun already has one.");
                return false;
            }

            // 瞄准镜槽互斥：一把枪只能装一个瞄准镜
            if (IsSightItem(attachment.id) && HasOtherSight(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing sight '{attachment.id}': gun already has one.");
                return false;
            }

            // 前握把槽互斥：一把枪只能装一个前握把
            if (IsForegripItem(attachment.id) && HasOtherForegrip(gun, attachment.id))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing foregrip '{attachment.id}': gun already has one.");
                return false;
            }

            // WASR 护木特殊互斥：WASR 安装后不可装战术设备和前握把
            bool installingWasr = string.Equals(attachment.id, WasrItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            bool gunHasWasr = IsAttachmentInstalled(gun, WasrItemSystem.ItemKey);
            if (installingWasr && (HasOtherTacticalDevice(gun, attachment.id) || HasOtherForegrip(gun, attachment.id)))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing WASR: gun already has tactical device/foregrip.");
                return false;
            }
            if (gunHasWasr && (IsTacticalDevice(attachment.id) || IsForegripItem(attachment.id)))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing '{attachment.id}': WASR handguard installed (no tactical/foregrip).");
                return false;
            }

            // 长枪管专属护木（SMR Mk.16 / 2-15木制 / LVOA-S）：必须先装加长枪管
            if (IsLongBarrelHandguardItem(attachment.id))
            {
                if (!IsAttachmentInstalled(gun, M4LongBarrelItemSystem.ItemKey))
                {
                    Plugin.Log.LogInfo($"[Attachment] Blocked installing long-barrel handguard '{attachment.id}': long barrel not installed.");
                    return false;
                }
            }
            // 普通护木（MOE SL/Viper）与加长枪管互斥：装了加长枪管则不能装普通护木
            else if (IsHandguardItem(attachment.id) && IsAttachmentInstalled(gun, M4LongBarrelItemSystem.ItemKey))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing handguard '{attachment.id}': long barrel installed (use a long-barrel handguard).");
                return false;
            }
            // 2-15木制不可安装握把和战术设备（同 WASR）
            bool installingAdarWood = string.Equals(attachment.id, AdarWoodItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            if (installingAdarWood && (HasOtherTacticalDevice(gun, attachment.id) || HasOtherForegrip(gun, attachment.id)))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing 2-15木制: gun already has tactical device/foregrip.");
                return false;
            }
            bool gunHasAdarWood = IsAttachmentInstalled(gun, AdarWoodItemSystem.ItemKey);
            if (gunHasAdarWood && (IsTacticalDevice(attachment.id) || IsForegripItem(attachment.id)))
            {
                Plugin.Log.LogInfo($"[Attachment] Blocked installing '{attachment.id}': 2-15木制 handguard installed (no tactical/foregrip).");
                return false;
            }

            // SKS 特定守卫（必须在 holder.attachmentIds.Add 之前，否则弹仓会被加入导致"已安装"）：
            // - SKS 已装弹匣（hasMag）时，禁止安装弹仓改件（必须先卸下弹匣）
            if (string.Equals(attachment.id, SksIntegralMagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                var gs = gun.GetComponent<GunScript>();
                if (gs != null && gs.hasMag)
                {
                    Plugin.Log.LogInfo($"[SKS Integral] Blocked installing: magazine still loaded.");
                    return false;
                }
            }
            // - SKS 仍装有弹仓改件时，禁止安装 SKS-A5 弹匣（改枪面板路径）
            if (string.Equals(attachment.id, SksA5MagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
                && IsAttachmentInstalled(gun, SksIntegralMagItemSystem.ItemKey))
            {
                Plugin.Log.LogInfo($"[SKS-A5] Blocked installing mag: 10-round integral mag still installed.");
                return false;
            }
            if (dryRun) return true;


            var holder = gun.GetComponent<GunAttachmentHolder>();
            if (holder == null) holder = gun.gameObject.AddComponent<GunAttachmentHolder>();

            holder.attachmentIds.Add(attachment.id);

            // ===== SKS 供弹方式切换 =====
            // 核心规则：只要配件栏没有 10 发弹仓改件，枪械就是 Mag 模式。
            // 安装 SKS-A5 弹匣：直接切 Mag 模式（无需级联卸弹仓，卸弹仓本身已切 Mag）
            if (string.Equals(attachment.id, SksA5MagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                var gunScript = gun.GetComponent<GunScript>();
                if (gunScript != null)
                {
                    gunScript.feedType = GunScript.FeedType.Mag;
                    gunScript.magCapacity = SksA5MagItemSystem.MaxRounds;
                    gunScript.roundsInMag = 0;
                    Plugin.Log.LogInfo($"[SKS-A5] Installed: switched to Mag mode, magCapacity={SksA5MagItemSystem.MaxRounds}.");
                }
                SKSItemSystem.UpdateSksVisual(gun); // 刷新为 SKS-A5 弹匣贴图
            }
            // SKS 10 发弹仓改件：安装时切换为弹仓模式（Direct）
            // （守卫已在上方 holder.attachmentIds.Add 之前处理，此处只做模式切换）
            else if (string.Equals(attachment.id, SksIntegralMagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                var gunScript = gun.GetComponent<GunScript>();
                if (gunScript != null)
                {
                    gunScript.feedType = GunScript.FeedType.Direct;
                    gunScript.magCapacity = SKSItemSystem.MagCapacity;
                    gunScript.roundsInMag = 0;
                    Plugin.Log.LogInfo($"[SKS Integral] Installed: switched to Direct mode, magCapacity={SKSItemSystem.MagCapacity}.");
                }
                SKSItemSystem.UpdateSksVisual(gun); // 刷新为带弹仓贴图
            }

            // LAS/TAC 2 战术手电：物品销毁前把电量保存到 holder，附上控制器
            if (string.Equals(attachment.id, LasTac2ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                // flashlight 基类的 condition 可能为 0，新装手电统一满电
                // 检测配件有无装载电池：无电池则视为空电（无法供电）
                float charge = (attachment.battery == null) ? 0f : attachment.condition;
                if (attachment.battery == null) holder.noBatteryAttachments.Add(attachment.id);
                else holder.noBatteryAttachments.Remove(attachment.id);
                holder.lasTacCharge = charge;
                LasTac2Controller.Attach(gun, charge);
                Plugin.Log.LogInfo($"[LAS/TAC 2] Installed with charge={holder.lasTacCharge:F2}.");
            }
            // Klesch-2U 战术手电：同上，电量存 kleschCharge
            else if (string.Equals(attachment.id, Klesch2UItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                // 检测配件有无装载电池：无电池则视为空电（无法供电）
                float charge = (attachment.battery == null) ? 0f : attachment.condition;
                if (attachment.battery == null) holder.noBatteryAttachments.Add(attachment.id);
                else holder.noBatteryAttachments.Remove(attachment.id);
                holder.kleschCharge = charge;
                Klesch2UController.Attach(gun);
                Plugin.Log.LogInfo($"[Klesch-2U] Installed with charge={holder.kleschCharge:F2}.");
            }
            // Baldr Pro 战术手电激光组合：电量存 baldrCharge
            else if (string.Equals(attachment.id, BaldrProItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                // 检测配件有无装载电池：无电池则视为空电（无法供电）
                float charge = (attachment.battery == null) ? 0f : attachment.condition;
                if (attachment.battery == null) holder.noBatteryAttachments.Add(attachment.id);
                else holder.noBatteryAttachments.Remove(attachment.id);
                holder.baldrCharge = charge;
                BaldrProController.Attach(gun);
                Plugin.Log.LogInfo($"[Baldr Pro] Installed with charge={holder.baldrCharge:F2}.");
            }
            // TBL 战术激光指示器：电量存 tblCharge
            else if (string.Equals(attachment.id, TblItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                // 检测配件有无装载电池：无电池则视为空电（无法供电）
                float charge = (attachment.battery == null) ? 0f : attachment.condition;
                if (attachment.battery == null) holder.noBatteryAttachments.Add(attachment.id);
                else holder.noBatteryAttachments.Remove(attachment.id);
                holder.tblCharge = charge;
                TblController.Attach(gun);
                Plugin.Log.LogInfo($"[TBL] Installed with charge={holder.tblCharge:F2}.");
            }
            // MRS 反射式瞄具：无供电机制，仅记录安装
            else if (string.Equals(attachment.id, MrsItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                Plugin.Log.LogInfo($"[MRS] Installed (no battery).");
            }
            // EOTech 553 全息瞄具：无供电机制，仅记录安装
            else if (string.Equals(attachment.id, Eotech553ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                Plugin.Log.LogInfo($"[EOTech 553] Installed (no battery).");
            }
            // EOTech HHS-1 复合瞄具：无供电机制，附上倍率控制器
            else if (string.Equals(attachment.id, Hhs1ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                Hhs1Controller.Attach(gun);
                Plugin.Log.LogInfo($"[HHS-1] Installed (no battery).");
            }
            // ELCAN SpecterDR 变倍瞄具：无供电机制，附上倍率控制器
            else if (string.Equals(attachment.id, SpecterDrItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                SpecterDrController.Attach(gun);
                Plugin.Log.LogInfo($"[SpecterDR] Installed (no battery).");
            }
            // Monstrum 2x32 棱镜瞄具：无供电机制，附上缩放控制器
            else if (string.Equals(attachment.id, Monstr2x32ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                Monstr2x32Controller.Attach(gun);
                Plugin.Log.LogInfo($"[Monstr 2x32] Installed (no battery).");
            }
            // TA01NSN 4x 瞄具：无供电机制，附上缩放控制器
            else if (string.Equals(attachment.id, Ta01nsnItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                Ta01nsnController.Attach(gun);
                Plugin.Log.LogInfo($"[TA01NSN] Installed (no battery).");
            }
            // Razor HD 变倍瞄具：无供电机制，附上倍率控制器
            else if (string.Equals(attachment.id, RazorHdItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                RazorHdController.Attach(gun);
                Plugin.Log.LogInfo($"[Razor HD] Installed (no battery).");
            }
            // PM II 变倍瞄具：无供电机制，附上倍率控制器
            else if (string.Equals(attachment.id, Pm2ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                Pm2Controller.Attach(gun);
                Plugin.Log.LogInfo($"[PM II] Installed (no battery).");
            }

            Sound.Play("combine", gun.transform.position);
            Plugin.Log.LogInfo($"[Attachment] Attached '{attachment.id}' to '{gun.id}' ({holder.attachmentIds.Count} total).");

            var gsForSync = gun.GetComponent<GunScript>();
            int syncFt = gsForSync != null ? (int)gsForSync.feedType : 0;
            int syncMc = gsForSync != null ? gsForSync.magCapacity : 0;
            int syncRm = gsForSync != null ? gsForSync.roundsInMag : 0;

            // 多人客户端：上报服务器（服务器端枪镜像 attachmentIds.Add + 销毁配件物品镜像）。
            // attachmentIds 是自定义组件字段，KrokMP 不同步，必须显式上报。
            if (KrokMpHelper.IsMultiplayer && !KrokMpHelper.IsHost)
            {
                // 先清除 KrokMP 的本地槽位快照，否则把配件从槽位移出/销毁会被 KrokMP
                // 误判为“丢到地上”，服务器端会多生成一个配件在地上。
                WeaponMpSync.SuppressLocalInventoryDrop(attachment);
                WeaponMpSync.ReportAttachInstall(gun, attachment.id, attachment, syncFt, syncMc, syncRm);
            }
            // 多人主机：本地就是权威，但要主动把自定义 attachmentIds 广播给客户端。
            else if (KrokMpHelper.IsMultiplayer && KrokMpHelper.IsHost)
            {
                WeaponMpSync.BroadcastAttachInstall(gun, attachment.id, syncFt, syncMc, syncRm);
            }

            // 先把配件移出背包槽位（SetParent 立即生效，背包 UI 不再残留），再延迟销毁。
            // 若配件在玩家槽位中，setParent 到枪下后 childCount 立即清零。
            try { attachment.transform.SetParent(gun.transform, false); }
            catch (Exception ex) { Plugin.Log.LogWarning($"[Attachment] SetParent failed: {ex.Message}"); }
            UnityEngine.Object.Destroy(attachment.gameObject);

            // 失效瞄准时间缓存（配件变化影响瞄准速度）。
            // 瞄准驱动由 AimSystem.TickPlayerAim（PlayerInputLockPatch.Postfix）统一处理。
            AimSystem.InvalidateAimTimeCache(gun);

            UpdateSuppressorVisual(gun);
            UpdateHandguardVisual(gun);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[Attachment] AttachToGun failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从枪上卸下最后安装的配件（LIFO），生成到玩家面前。
    /// 会级联卸下依赖它的附属配件（如卸下护木时同时卸下 LAS/TAC 2）。
    /// 有配件被卸下返回 true；无配件返回 false（调用方走原版动作）。
    /// </summary>
    public static bool TryDetachFromGun(Item gun)
    {
        var holder = gun.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds.Count == 0) return false;

        var id = holder.attachmentIds[holder.attachmentIds.Count - 1];
        DetachCascade(gun, id);
        return true;
    }

    /// <summary>
    /// 级联卸下：先卸下所有依赖 attachmentId 的已装附属配件（如手电依赖护木），再卸下 attachmentId 本身。
    /// </summary>
    public static void DetachCascade(Item gun, string attachmentId)
    {
        try
        {
            var holder = gun.GetComponent<GunAttachmentHolder>();
            if (holder == null) return;

            // 收集依赖 attachmentId 且已装的配件（附属配件）
            var dependents = new List<string>();
            bool detachingBarrel = IsBarrelItem(attachmentId);
            // 卸下 UAS 时：SKS 上已装的白名单瞄准镜（依附于 UAS）一并卸下
            bool detachingUas = string.Equals(attachmentId, UasSksItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            // 卸下 Tapco 时：SKS 上已装的 M4 后托（依附于 Tapco 缓冲管）一并卸下
            bool detachingTapco = string.Equals(attachmentId, TapcoIntrafuseItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            // 卸下 WT0032-1 时：SKS 上已装的枪口装置（依附于 WT0032-1 螺纹转换器）一并卸下
            bool detachingWt0032 = string.Equals(attachmentId, Wt0032_1ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            // 卸下 TMB 338LM 时：AXMC 上已装的 TSM 338LM（依附于 TMB）一并卸下
            bool detachingTmb = string.Equals(attachmentId, Tmb338lmItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            foreach (var id in holder.attachmentIds)
            {
                if (string.Equals(id, attachmentId, StringComparison.OrdinalIgnoreCase)) continue;
                // 卸下 UAS：级联卸下白名单瞄准镜（UAS 自带导轨，瞄准镜依附于 UAS）
                if (detachingUas && IsSightItem(id) && IsSightAllowedForUas(id))
                {
                    dependents.Add(id);
                    continue;
                }
                // 卸下 Tapco：级联卸下 M4 后托（Tapco 有缓冲管接口，后托依附于 Tapco）
                if (detachingTapco && IsStockItem(id) && IsM4OnlyItem(id))
                {
                    dependents.Add(id);
                    continue;
                }
                // 卸下 WT0032-1：级联卸下枪口装置（DTK-1 等，依附于 WT0032-1 螺纹转换器）
                // 排除 Hexagon SKS（直接装枪口，不依附于转换器）
                if (detachingWt0032 && IsMuzzleDeviceRequiresWt0032(id))
                {
                    dependents.Add(id);
                    continue;
                }
                // 卸下 TMB 338LM：级联卸下 TSM 338LM（依附于 TMB）
                if (detachingTmb && string.Equals(id, Tsm338lmItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                {
                    dependents.Add(id);
                    continue;
                }
                // AND 前提：id 直接要求 attachmentId
                var prereqs = ToolSystem.GetPrerequisites(id);
                if (prereqs != null && prereqs.Contains(attachmentId))
                {
                    dependents.Add(id);
                    continue;
                }
                // OR 前提组：id 依赖的护木组中包含 attachmentId
                // （战术设备依赖 MOE/Hexagon）
                if (ToolSystem.OrPrerequisiteGroups.TryGetValue(id, out var alts)
                    && alts != null && alts.Contains(attachmentId))
                {
                    dependents.Add(id);
                    continue;
                }
                // 前握把条件前提：前握把依赖护木
                // （GetForegripMissingPrereq 是条件化的，但卸下护木时必须级联卸下前握把）
                bool isHandguard = IsHandguardItem(attachmentId);
                if (isHandguard && (IsForegripItem(id) || IsTacticalDevice(id)))
                {
                    dependents.Add(id);
                }
                // 卸下加长枪管时，护木（及护木上的配件）和枪口装置也应级联卸下（对称行为）
                if (detachingBarrel)
                {
                    // 加长护木（含普通护木，若存在）掉落
                    if (IsHandguardItem(id))
                    {
                        dependents.Add(id);
                        // 同时级联卸下该护木上的前握把和战术设备
                        foreach (var sub in holder.attachmentIds)
                        {
                            if (string.Equals(sub, id, StringComparison.OrdinalIgnoreCase)) continue;
                            if (IsForegripItem(sub) || IsTacticalDevice(sub))
                                if (!dependents.Contains(sub)) dependents.Add(sub);
                        }
                    }
                    // 枪口装置掉落
                    else if (IsMuzzleItem(id))
                    {
                        dependents.Add(id);
                    }
                }
            }

            // 先卸附属，再卸自身
            foreach (var dep in dependents)
                DetachSingle(gun, dep);
            DetachSingle(gun, attachmentId);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[Attachment] DetachCascade failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 生成卸下的配件物品。
    /// 多人客户端返回 null（不本地创建）：服务器创建后 KrokMP 会同步回来，
    /// 本地创建会产生重复物品（客户端 Utils.Create 不注册网络同步）。
    /// 调用方已有 if (spawned != null) 守卫，返回 null 时自动跳过电量写回等后续逻辑。
    /// </summary>
    private static GameObject SpawnDetachedAttachment(string id, Vector2 spawnPos, bool skipCreate)
    {
        if (skipCreate) return null;
        return Utils.Create(id, spawnPos, 0f);
    }

    /// <summary>
    /// 从枪上卸下指定配件（从列表移除该项），生成到玩家面前。
    /// 统一处理 LAS/TAC 2 的电量保存、灯光关闭、控制器移除。
    /// </summary>
    public static void DetachSingle(Item gun, string id)
    {
        try
        {
            InvalidateSightZoomCache(gun);
            var holder = gun.GetComponent<GunAttachmentHolder>();
            if (holder == null || !holder.attachmentIds.Remove(id)) return;

            var body = PlayerCamera.main?.body;
            var spawnPos = body != null
                ? (Vector2)body.transform.position + UnityEngine.Random.insideUnitCircle * 1.2f
                : (Vector2)gun.transform.position;

            // 电量快照（战术手电）：多人客户端需上报给服务器，让服务器创建物品时写回电量。
            // 必须在下方各分支把 holder.lasTacCharge 等重置为 0 之前捕获。
            float syncCharge = 0f;
            bool syncHadBattery = false;
            if (holder != null)
            {
                if (string.Equals(id, LasTac2ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                    syncCharge = holder.lasTacCharge;
                else if (string.Equals(id, Klesch2UItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                    syncCharge = holder.kleschCharge;
                else if (string.Equals(id, BaldrProItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                    syncCharge = holder.baldrCharge;
                else if (string.Equals(id, TblItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                    syncCharge = holder.tblCharge;
                syncHadBattery = !holder.noBatteryAttachments.Contains(id);
            }
            // 多人客户端：物品由服务器创建后同步回来，本地跳过创建（避免重复）。
            // 仅当枪已注册网络对象（有 syncId）时才走上报路径；否则退回本地创建，
            // 避免上报失败导致配件从 attachmentIds 移除后永久丢失。
            bool mpClient = KrokMpHelper.IsMultiplayer && !KrokMpHelper.IsHost
                            && WeaponMpSync.CanSync(gun);
            bool skipCreate = mpClient;

            // ===== SKS 供弹方式切换 =====
            // 核心规则：只要配件栏没有 10 发弹仓改件，枪械就是 Mag 模式。
            // 卸下 SKS-A5 弹匣：恢复默认 10 发弹仓改件，切回 Direct 模式
            if (string.Equals(id, SksA5MagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                var gunScript = gun.GetComponent<GunScript>();
                if (gunScript != null)
                {
                    gunScript.feedType = GunScript.FeedType.Direct;
                    gunScript.magCapacity = SKSItemSystem.MagCapacity;
                    gunScript.roundsInMag = 0;
                    Plugin.Log.LogInfo($"[SKS-A5] Detached: switched back to Direct mode, magCapacity={SKSItemSystem.MagCapacity}.");
                }
                // 恢复默认 10 发弹仓改件（重新加入 holder，改枪面板左栏重新显示）
                if (holder != null && !holder.attachmentIds.Contains(SksIntegralMagItemSystem.ItemKey))
                    holder.attachmentIds.Add(SksIntegralMagItemSystem.ItemKey);
                SKSItemSystem.UpdateSksVisual(gun); // 刷新为带弹仓贴图
            }
            // 卸下 10 发弹仓改件：枪械自动切换为 Mag 模式（此时可直接安装弹匣）
            else if (string.Equals(id, SksIntegralMagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                var gunScript = gun.GetComponent<GunScript>();
                if (gunScript != null)
                {
                    gunScript.feedType = GunScript.FeedType.Mag;
                    gunScript.magCapacity = SksA5MagItemSystem.MaxRounds;
                    gunScript.roundsInMag = 0;
                    Plugin.Log.LogInfo($"[SKS Integral] Detached: switched to Mag mode, magCapacity={SksA5MagItemSystem.MaxRounds}.");
                }
                SKSItemSystem.UpdateSksVisual(gun); // 刷新为无弹匣贴图（sksmagout）
            }

            // 战术手电（LAS/TAC 2 / Klesch-2U）：卸下时把 holder 中剩余电量写回生成的物品，
            // 关闭枪上的灯、移除控制器、重置电量（防止残留功能 / 不耗电问题）。
            if (string.Equals(id, LasTac2ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                float savedCharge = holder.lasTacCharge;
                if (savedCharge <= 0.01f) savedCharge = 0.01f; // 防止完全没电的不可用
                var spawned = SpawnDetachedAttachment(id, spawnPos, skipCreate);
                if (spawned != null)
                {
                    // 延迟写回电量：Utils.Create 内部流程（ConfigureSpawnedItem/Item.Start）会覆盖 condition
                    var setter = spawned.AddComponent<TacticalLightDetachedCharge>();
                    setter.lightId = LasTac2ItemSystem.ItemKey;
                    setter.charge = savedCharge;
                    setter.hadBattery = !holder.noBatteryAttachments.Contains(id);
                }
                var ctrl = gun.GetComponent<LasTac2Controller>();
                if (ctrl != null) ctrl.Shutdown();
                holder.lasTacCharge = 0f; // 重置：再次安装时走满电兜底
            }
            else if (string.Equals(id, Klesch2UItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                float savedCharge = holder.kleschCharge;
                if (savedCharge <= 0.01f) savedCharge = 0.01f;
                var spawned = SpawnDetachedAttachment(id, spawnPos, skipCreate);
                if (spawned != null)
                {
                    var setter = spawned.AddComponent<TacticalLightDetachedCharge>();
                    setter.lightId = Klesch2UItemSystem.ItemKey;
                    setter.charge = savedCharge;
                    setter.hadBattery = !holder.noBatteryAttachments.Contains(id);
                }
                var ctrl = gun.GetComponent<Klesch2UController>();
                if (ctrl != null) ctrl.Shutdown();
                holder.kleschCharge = 0f;
            }
            else if (string.Equals(id, BaldrProItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                float savedCharge = holder.baldrCharge;
                if (savedCharge <= 0.01f) savedCharge = 0.01f;
                var spawned = SpawnDetachedAttachment(id, spawnPos, skipCreate);
                if (spawned != null)
                {
                    var setter = spawned.AddComponent<TacticalLightDetachedCharge>();
                    setter.lightId = BaldrProItemSystem.ItemKey;
                    setter.charge = savedCharge;
                    setter.hadBattery = !holder.noBatteryAttachments.Contains(id);
                }
                var ctrl = gun.GetComponent<BaldrProController>();
                if (ctrl != null) ctrl.Shutdown();
                holder.baldrCharge = 0f;
            }
            else if (string.Equals(id, TblItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                float savedCharge = holder.tblCharge;
                if (savedCharge <= 0.01f) savedCharge = 0.01f;
                var spawned = SpawnDetachedAttachment(id, spawnPos, skipCreate);
                if (spawned != null)
                {
                    var setter = spawned.AddComponent<TacticalLightDetachedCharge>();
                    setter.lightId = TblItemSystem.ItemKey;
                    setter.charge = savedCharge;
                    setter.hadBattery = !holder.noBatteryAttachments.Contains(id);
                }
                var ctrl = gun.GetComponent<TblController>();
                if (ctrl != null) ctrl.Shutdown();
                holder.tblCharge = 0f;
            }
            else if (string.Equals(id, MrsItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                SpawnDetachedAttachment(id, spawnPos, skipCreate);
            }
            else if (string.Equals(id, Eotech553ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                SpawnDetachedAttachment(id, spawnPos, skipCreate);
            }
            else if (string.Equals(id, Hhs1ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                SpawnDetachedAttachment(id, spawnPos, skipCreate);
                var ctrl = gun.GetComponent<Hhs1Controller>();
                if (ctrl != null) UnityEngine.Object.Destroy(ctrl);
            }
            else if (string.Equals(id, SpecterDrItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                SpawnDetachedAttachment(id, spawnPos, skipCreate);
                var ctrl = gun.GetComponent<SpecterDrController>();
                if (ctrl != null) UnityEngine.Object.Destroy(ctrl);
            }
            else if (string.Equals(id, Monstr2x32ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                SpawnDetachedAttachment(id, spawnPos, skipCreate);
                var ctrl = gun.GetComponent<Monstr2x32Controller>();
                if (ctrl != null) UnityEngine.Object.Destroy(ctrl);
            }
            else if (string.Equals(id, Ta01nsnItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                SpawnDetachedAttachment(id, spawnPos, skipCreate);
                var ctrl = gun.GetComponent<Ta01nsnController>();
                if (ctrl != null) UnityEngine.Object.Destroy(ctrl);
            }
            else if (string.Equals(id, RazorHdItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                SpawnDetachedAttachment(id, spawnPos, skipCreate);
                var ctrl = gun.GetComponent<RazorHdController>();
                if (ctrl != null) UnityEngine.Object.Destroy(ctrl);
            }
            else if (string.Equals(id, Pm2ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                SpawnDetachedAttachment(id, spawnPos, skipCreate);
                var ctrl = gun.GetComponent<Pm2Controller>();
                if (ctrl != null) UnityEngine.Object.Destroy(ctrl);
            }
            else
            {
                SpawnDetachedAttachment(id, spawnPos, skipCreate);
            }

            // 多人客户端：上报服务器（服务器端移除 attachmentIds、创建配件物品、
            // 应用 SKS 供弹状态；物品通过 KrokMP 同步回到本客户端）。
            if (mpClient)
            {
                var gsForSync = gun.GetComponent<GunScript>();
                WeaponMpSync.ReportAttachDetach(
                    gun, id,
                    syncCharge, syncHadBattery,
                    gsForSync != null ? (int)gsForSync.feedType : 0,
                    gsForSync != null ? gsForSync.magCapacity : 0,
                    gsForSync != null ? gsForSync.roundsInMag : 0,
                    spawnPos);
            }
            // 多人主机：本地就是权威，把自定义 attachmentIds 变更广播给客户端。
            else if (KrokMpHelper.IsMultiplayer && KrokMpHelper.IsHost)
            {
                var gsForSync = gun.GetComponent<GunScript>();
                WeaponMpSync.BroadcastAttachDetach(
                    gun, id,
                    gsForSync != null ? (int)gsForSync.feedType : 0,
                    gsForSync != null ? gsForSync.magCapacity : 0,
                    gsForSync != null ? gsForSync.roundsInMag : 0);
            }
            Sound.Play("drop", gun.transform.position);
            Plugin.Log.LogInfo($"[Attachment] Detached '{id}' from '{gun.id}' ({holder.attachmentIds.Count} left).");
            // 失效瞄准时间缓存（配件变化影响瞄准速度）
            AimSystem.InvalidateAimTimeCache(gun);
            UpdateSuppressorVisual(gun);
            UpdateHandguardVisual(gun);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[Attachment] DetachSingle failed: {ex.Message}");
        }
    }

    // ===== 拖拽安装已废除 =====
    // 配件安装/卸下只能通过改枪面板（G 键）进行。
    // 若把配件拖到枪上，将走原版 CanCombine 逻辑（attachment tag 不会被原版识别为可组合项），
    // 不会发生安装，符合预期。

    // ===== 开火效果 =====

    [HarmonyPatch(typeof(GunScript), nameof(GunScript.Fire))]
    public static class FireEffectsPatch
    {
        /// <summary>
        /// 卡壳判定时机标志：仅在 Fire() 开火时判定卡壳。
        /// 原版 JamChance() 被 3 个时机调用（Fire 开火 / Update 抛壳 / Update 上膛），
        /// 每发子弹触发 3 次独立判定，导致实际卡壳率 ≈ 1-(1-p)^3，远高于单次概率。
        /// 此标志让 JamChancePatch 只在 Fire 时机返回真实概率，抛壳/上膛返回 0，
        /// 使每发子弹恰好判定一次，卡壳率符合预期。
        /// </summary>
        public static bool InFire;

        public sealed class State
        {
            public bool active;                 // 是否有任一配件效果生效
            public bool loudnessChanged;
            public float loudness;
            public bool knockBackChanged;
            public float knockBack;
            public bool conditionChanged;
            public float conditionLossPerShot;
            public bool fireSoundChanged;
            public AudioClip? fireSound;
            public ParticleSystem? muzzleParticle;
            public bool muzzleWasEmitting;
            public Vector3? muzzleOriginal;   // 加长枪管时火光位置临时偏移，用于恢复
            public Vector3? barrelOriginal;   // 装消音器/加长枪管时弹道起点临时偏移，用于恢复
            public bool spreadChanged;
            public float verticalSpread;
            public bool damageChanged;
            public float animalDamage;
            public float burstGunAngleDelta; // 连发枪口垂直偏移（Postfix 覆盖应用）
        }

        // ===== 连发偶尔向下（垂直方向）=====
        // 连续射击时枪口正常向上仰（原版后座），但连发一段时间后会**偶尔**向下抖一下，
        // 扰乱玩家的压枪节奏：玩家以为在向上压，枪却突然向下，压过头。
        // 规则：
        //   - 前 6 发完全正常（原版后座，无偏移）
        //   - 第 7 发起，每发有概率触发一次向下偏移（偶尔，不是每发）
        //   - 只作用于垂直方向（gunangle），不影响水平后坐力
        //   - 仅全自动（FiringMode.Auto）枪械生效，半自动/泵动不受影响
        //   - 松开扳机（超过重置间隔）后连发计数归零
        private const float BurstResetInterval = 0.4f;   // 超过 0.4s 未开火视为新的一轮
        private const float BurstDownRatio = 1.6f;       // 向下偏移幅度 = 枪械后坐力(knockBack) × 此比例
        private const int BurstStartCount = 6;           // 前 6 发正常后座
        private const float BurstDownChance = 0.46f;     // 第 7 发起每发 46% 概率触发一次向下

        private sealed class BurstState
        {
            public int count;
            public float lastFireTime;
        }

        private static readonly Dictionary<int, BurstState> BurstStates = new();
        private static int _burstPruneCounter;

        /// <summary>
        /// 计算当前连发枪口垂直偏移量（0 = 正常后座；负 = 向下抖动），
        /// 并更新连发计数。前 6 发正常，第 7 发起偶尔向下。
        /// 返回偏移量供 Prefix 存到 State。
        /// </summary>
        private static float GetBurstGunAngleDelta(Item gunItem, GunScript gun)
        {
            if (gunItem == null) return 0f;
            // 仅全自动生效（半自动/泵动不受影响）
            if (gun == null || gun.firingMode != GunScript.FiringMode.Auto) return 0f;

            int id = gunItem.GetInstanceID();
            float now = Time.time;

            // 定期清理超过 10 秒未开火的连发状态，避免实例 ID 积累
            if ((++_burstPruneCounter % 600) == 0 && BurstStates.Count > 0)
            {
                var dead = new List<int>();
                foreach (var kv in BurstStates)
                    if (now - kv.Value.lastFireTime > 10f) dead.Add(kv.Key);
                foreach (var key in dead) BurstStates.Remove(key);
            }

            if (!BurstStates.TryGetValue(id, out var st))
            {
                st = new BurstState();
                BurstStates[id] = st;
            }

            // 超过重置间隔：视为新的一轮，从第 1 发重新计
            if (now - st.lastFireTime > BurstResetInterval)
                st.count = 0;

            st.lastFireTime = now;
            st.count++;

            // 前 6 发正常后座，无偏移
            if (st.count <= BurstStartCount) return 0f;

            // 第 7 发起：每发 46% 概率触发一次向下抖动
            // 向下幅度与枪械后坐力数据关联：幅度 = knockBack × BurstDownRatio
            // （后坐力大的枪向下幅度也大，保持相对强度一致）
            if (UnityEngine.Random.value < BurstDownChance)
                return -gun.knockBack * BurstDownRatio;

            return 0f;
        }

        /// <summary>
        /// 获取 GunScript 对应的玩家 body（GunScript.body => PlayerCamera.main.body）。
        /// </summary>
        private static Body? GetGunBody(GunScript gun)
        {
            if (gun == null) return null;
            try { return PlayerCamera.main?.body; }
            catch { return null; }
        }

        [HarmonyPrefix]
        public static bool Prefix(GunScript __instance, out State __state)
        {
            __state = new State();
            // 标记当前处于 Fire() 时机：JamChancePatch 只在此时返回真实卡壳概率
            InFire = true;
            try
            {
                var gunItem = __instance.GetComponent<Item>();
                if (gunItem == null) return true;

                // 缓存 holder 一次：本方法有几十个 IsAttachmentInstalled 调用，
                // 每个都 GetComponent<GunAttachmentHolder>()。开火是热路径，统一改走 HasAttachment()。
                var holder = gunItem.GetComponent<GunAttachmentHolder>();

                bool HasAttachment(string id)
                    => holder != null && holder.attachmentIds != null && holder.attachmentIds.Contains(id);

                // ===== M4 加长枪管：未安装改装护木时无法开火 =====
                // 加长枪管初始为单枪管（无护木），必须安装 MOE SL 等改装护木才能正常开火。
                if (IsM4Gun(gunItem)
                    && HasAttachment(M4LongBarrelItemSystem.ItemKey)
                    && !IsHandguardInstalled(gunItem))
                {
                    Plugin.Log.LogInfo("[M4LongBarrel] Cannot fire: long barrel installed but no handguard.");
                    return false; // 跳过开火
                }

                // ===== 瞄准系统：根据瞄准进度调整散布 =====
                // 未瞄准(0)→×2.5，瞄准满(1)→×1.0（回到武器原版精准度）
                // 霰弹枪：瞄准系统仍生效（霰弹枪也要瞄准），但配件精度增益无效
                bool shotgun = IsShotgun(gunItem);
                float aimMult = AimSystem.GetEffectiveSpreadMult(gunItem);
                if (aimMult != 1f)
                {
                    __state.active = true;
                    __state.spreadChanged = true;
                    __state.verticalSpread = __instance.verticalSpread;
                    __instance.verticalSpread *= aimMult;
                }

                // ===== 效果应用（多配件可叠加）=====
                // 注意：所有 __state 保存必须在第一个改动该字段的分支进行，后续分支不得覆盖！
                // 否则保存的是"乘过前一个配件后的中间值"，Postfix 恢复中间值 → 每次开火数值递减趋近 0。

                // ===== 护木效果：后坐力 -1%、每发耐久损耗 -3% =====
                if (HasAttachment(MoeAkmItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.knockBack *= MoeAkmItemSystem.KnockBackMult;
                    __instance.conditionLossPerShot *= MoeAkmItemSystem.ConditionLossMult;
                }

                // ===== MOE SL 护木效果（M4）：后坐力 -0.3% =====
                if (HasAttachment(MoeSlItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= MoeSlItemSystem.KnockBackMult;
                }

                // ===== Viper 护木效果（M4）：后坐力 -0.3%、每发耐久损耗 -1.2% =====
                if (HasAttachment(ViperItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.knockBack *= ViperItemSystem.KnockBackMult;
                    __instance.conditionLossPerShot *= ViperItemSystem.ConditionLossMult;
                }

                // ===== UAS SKS 套件效果：后坐力 -30%、每发耐久损耗 -7% =====
                if (HasAttachment(UasSksItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.knockBack *= UasSksItemSystem.KnockBackMult;
                    __instance.conditionLossPerShot *= UasSksItemSystem.ConditionLossMult;
                }

                // ===== Tapco INTRAFUSE 套件效果（动态）：有后托 -5%，无后托 +26%；耐久损耗 -2% 恒定 =====
                if (HasAttachment(TapcoIntrafuseItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.knockBack *= HasAnyStock(gunItem)
                        ? TapcoIntrafuseItemSystem.KnockBackMultWithStock
                        : TapcoIntrafuseItemSystem.KnockBackMultNoStock;
                    __instance.conditionLossPerShot *= TapcoIntrafuseItemSystem.ConditionLossMult;
                }

                // ===== SKS ATI Monte Carlo 枪托效果：后坐力 -10% =====
                if (HasAttachment(SksMcItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= SksMcItemSystem.KnockBackMult;
                }

                // ===== 长枪管专属护木效果（M4）：每发耐久损耗 =====
                if (HasAttachment(SmrMk16ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.conditionLossPerShot *= SmrMk16ItemSystem.ConditionLossMult;
                }
                if (HasAttachment(AdarWoodItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.conditionLossPerShot *= AdarWoodItemSystem.ConditionLossMult;
                }
                if (HasAttachment(LvoaItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.conditionLossPerShot *= LvoaItemSystem.ConditionLossMult;
                }

                // ===== 枪托效果（OPFOR AA47）：后坐力 -25%、精准度 +3%（散布 ×0.97）=====
                if (HasAttachment(OpforAak7ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= OpforAak7ItemSystem.KnockBackMult;
                    if (!shotgun) __instance.verticalSpread *= OpforAak7ItemSystem.SpreadMult;
                }

                // ===== 枪托效果（Kocherga 烧火棍）：后坐力 -17%、精准度 +2%（散布 ×0.98）=====
                if (HasAttachment(KochergaItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= KochergaItemSystem.KnockBackMult;
                    if (!shotgun) __instance.verticalSpread *= KochergaItemSystem.SpreadMult;
                }

                // ===== 枪托效果（Zhukov-S）：后坐力 -19%、精准度 +6%（散布 ×0.94）=====
                if (HasAttachment(ZhukovSItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= ZhukovSItemSystem.KnockBackMult;
                    if (!shotgun) __instance.verticalSpread *= ZhukovSItemSystem.SpreadMult;
                }

                // ===== Hexagon AK 管状护木效果：后坐力 -0.5%、每发耐久损耗 -5% =====
                if (HasAttachment(HexagonAkHandguardItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.knockBack *= HexagonAkHandguardItemSystem.KnockBackMult;
                    __instance.conditionLossPerShot *= HexagonAkHandguardItemSystem.ConditionLossMult;
                }

                // ===== B-10M+B-19 护木效果：后坐力 -0.3%、每发耐久损耗 -5% =====
                if (HasAttachment(B10mB19ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.knockBack *= B10mB19ItemSystem.KnockBackMult;
                    __instance.conditionLossPerShot *= B10mB19ItemSystem.ConditionLossMult;
                }

                // ===== WASR 护木效果：后坐力 -2.3%、每发耐久损耗 +0.5% =====
                if (HasAttachment(WasrItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.knockBack *= WasrItemSystem.KnockBackMult;
                    __instance.conditionLossPerShot *= WasrItemSystem.ConditionLossMult;
                }

                // ===== 一体式枪托效果（CQR47）：后坐力 -25%、精准度 +2%（散布 ×0.98）=====
                if (HasAttachment(Cqr47ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= Cqr47ItemSystem.KnockBackMult;
                    if (!shotgun) __instance.verticalSpread *= Cqr47ItemSystem.SpreadMult;
                }

                // ===== M4 专属后托效果 =====
                // Viper Mod.1：后坐力 +10%（散布不变）
                if (HasAttachment(Vipermod1ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= Vipermod1ItemSystem.KnockBackMult;
                }
                // CTR：后坐力 -18%、精准度 +2.5%（散布 ×0.975）
                if (HasAttachment(CtrItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= CtrItemSystem.KnockBackMult;
                    if (!shotgun) __instance.verticalSpread *= CtrItemSystem.SpreadMult;
                }
                // DS150 FDE：后坐力 -16.5%、精准度 +3%（散布 ×0.97）
                if (HasAttachment(Ds150fdeItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= Ds150fdeItemSystem.KnockBackMult;
                    if (!shotgun) __instance.verticalSpread *= Ds150fdeItemSystem.SpreadMult;
                }
                // ACS：后坐力 -25%（散布不变）
                if (HasAttachment(AcsItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= AcsItemSystem.KnockBackMult;
                }
                // MOE FG：后坐力 -20%、精准度 +2.5%（散布 ×0.975）
                if (HasAttachment(MoefgItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= MoefgItemSystem.KnockBackMult;
                    if (!shotgun) __instance.verticalSpread *= MoefgItemSystem.SpreadMult;
                }
                // MOE FDE：后坐力 -20%、精准度 +2.5%（散布 ×0.975）
                if (HasAttachment(MoefdeItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= MoefdeItemSystem.KnockBackMult;
                    if (!shotgun) __instance.verticalSpread *= MoefdeItemSystem.SpreadMult;
                }
                // MOE SG：后坐力 -20%、精准度 +2.5%（散布 ×0.975）
                if (HasAttachment(MoesgItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= MoesgItemSystem.KnockBackMult;
                    if (!shotgun) __instance.verticalSpread *= MoesgItemSystem.SpreadMult;
                }

                // ===== RK-3 手枪式握把效果：后坐力 -2.5%、精准度 +1%（散布 ×0.99）=====
                if (HasAttachment(Rk3ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= Rk3ItemSystem.KnockBackMult;
                    if (!shotgun) __instance.verticalSpread *= Rk3ItemSystem.SpreadMult;
                }

                // ===== MG-47 手枪式握把效果：后坐力 -2%（散布不变）=====
                if (HasAttachment(Mg47ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= Mg47ItemSystem.KnockBackMult;
                }

                // ===== AGS-74 手枪式握把效果：后坐力 -3%、精准度 +3%（散布 ×0.97）=====
                if (HasAttachment(Ags74ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= Ags74ItemSystem.KnockBackMult;
                    if (!shotgun) __instance.verticalSpread *= Ags74ItemSystem.SpreadMult;
                }

                // ===== M4 专属后握把效果 =====
                // TD120001：后坐力 -1.5%
                if (HasAttachment(Td120001ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= Td120001ItemSystem.KnockBackMult;
                }
                // Stark AR RG：后坐力 -2%
                if (HasAttachment(StarkArrgItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= StarkArrgItemSystem.KnockBackMult;
                }
                // MIAD：后坐力 -1%
                if (HasAttachment(MiadItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= MiadItemSystem.KnockBackMult;
                }
                // F1 St2 PC：后坐力 -2%、精准度 +1%（散布 ×0.99）
                if (HasAttachment(F1st2pcItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= F1st2pcItemSystem.KnockBackMult;
                    if (!shotgun) __instance.verticalSpread *= F1st2pcItemSystem.SpreadMult;
                }
                // Ergo：后坐力 -2.6%、精准度 +2%（散布 ×0.98）
                if (HasAttachment(ErgoItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= ErgoItemSystem.KnockBackMult;
                    if (!shotgun) __instance.verticalSpread *= ErgoItemSystem.SpreadMult;
                }

                // ===== PDC 导轨防尘盖效果：每发耐久损耗 -0.5%（散布不变）=====
                if (HasAttachment(PdcItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.conditionLossPerShot *= PdcItemSystem.ConditionLossMult;
                }

                // ===== 前握把效果（后坐力，仅影响 knockBack）=====
                if (HasAttachment(ShiftForegripItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= ShiftForegripItemSystem.KnockBackMult;
                }
                if (HasAttachment(Se5ForegripItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= Se5ForegripItemSystem.KnockBackMult;
                }
                if (HasAttachment(Rk0ForegripItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= Rk0ForegripItemSystem.KnockBackMult;
                }
                if (HasAttachment(Rk2ForegripItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= Rk2ForegripItemSystem.KnockBackMult;
                }
                if (HasAttachment(B25ur1ForegripItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= B25ur1ForegripItemSystem.KnockBackMult;
                }
                if (HasAttachment(CobraForegripItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= CobraForegripItemSystem.KnockBackMult;
                }
                if (HasAttachment(P2ForegripItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= P2ForegripItemSystem.KnockBackMult;
                }
                if (HasAttachment(AfgForegripItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= AfgForegripItemSystem.KnockBackMult;
                }

                // ===== MRS 反射式瞄具效果：精准度 +10%（散布 ×0.90，永久生效，无供电机制）=====
                if (HasAttachment(MrsItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    if (!shotgun) __instance.verticalSpread *= MrsItemSystem.SpreadMult;
                }

                // ===== EOTech 553 全息瞄具效果：精准度 +16%（散布 ×0.84，永久生效，无供电机制）=====
                if (HasAttachment(Eotech553ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    if (!shotgun) __instance.verticalSpread *= Eotech553ItemSystem.SpreadMult;
                }

                // ===== Leupold DeltaPoint 反射式瞄具效果：精准度 +12%（散布 ×0.88，永久生效）=====
                if (HasAttachment(DeltaPointItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    if (!shotgun) __instance.verticalSpread *= DeltaPointItemSystem.SpreadMult;
                }

                // ===== Aimpoint ACRO P-1 反射式瞄具效果：精准度 +10%（散布 ×0.90，永久生效）=====
                if (HasAttachment(AcroP1ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    if (!shotgun) __instance.verticalSpread *= AcroP1ItemSystem.SpreadMult;
                }

                // ===== EOTech HHS-1 复合瞄具效果：精准度 +25%（散布 ×0.75，永久生效，无供电机制）=====
                if (HasAttachment(Hhs1ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    if (!shotgun) __instance.verticalSpread *= Hhs1ItemSystem.SpreadMult;
                }

                // ===== ELCAN SpecterDR 变倍瞄具效果：精准度 +25%（散布 ×0.75，永久生效）=====
                if (HasAttachment(SpecterDrItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    if (!shotgun) __instance.verticalSpread *= SpecterDrItemSystem.SpreadMult;
                }

                // ===== Monstr 2x32 棱镜瞄具效果：精准度 +15%（散布 ×0.85，永久生效）=====
                if (HasAttachment(Monstr2x32ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    if (!shotgun) __instance.verticalSpread *= Monstr2x32ItemSystem.SpreadMult;
                }

                // ===== TA01NSN 4x 瞄具效果：精准度 +28%（散布 ×0.72，永久生效）=====
                if (HasAttachment(Ta01nsnItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    if (!shotgun) __instance.verticalSpread *= Ta01nsnItemSystem.SpreadMult;
                }

                // ===== Razor HD 变倍瞄具效果：精准度 +35%（散布 ×0.65，永久生效）=====
                if (HasAttachment(RazorHdItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    if (!shotgun) __instance.verticalSpread *= RazorHdItemSystem.SpreadMult;
                }

                // ===== PM II 变倍瞄具效果：精准度 +35%（散布 ×0.65，永久生效）=====
                if (HasAttachment(Pm2ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    if (!shotgun) __instance.verticalSpread *= Pm2ItemSystem.SpreadMult;
                }

                // ===== Dynacomp 膛口制退器效果：后坐力 -8% =====
                if (HasAttachment(DynacompItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= DynacompItemSystem.KnockBackMult;
                }

                // ===== DTK-1 膛口制退器效果：后坐力 -12% + 无枪口火光 =====
                if (HasAttachment(Dtk1ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= Dtk1ItemSystem.KnockBackMult;

                    // 隐藏枪口火光（Play() 会无视 emission.enabled 强制发射，需禁用整个物体）
                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleWasEmitting = __instance.muzzleParticle.emission.enabled;
                        __state.muzzleParticle.gameObject.SetActive(false);
                    }
                }

                // ===== SRVV AKM 膛口制退器效果：后坐力 -11% =====
                if (HasAttachment(SrvvAkmItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= SrvvAkmItemSystem.KnockBackMult;
                }

                // ===== Zenit DTK-4M 膛口制退器效果：后坐力 -7.6%、听力 -45%、精准度 -2% + 无枪口火光 =====
                if (HasAttachment(Dtk4mItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.loudnessChanged) { __state.loudness = __instance.loudness; __state.loudnessChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= Dtk4mItemSystem.KnockBackMult;
                    __instance.loudness *= Dtk4mItemSystem.LoudnessMult;
                    if (!shotgun) __instance.verticalSpread *= Dtk4mItemSystem.SpreadMult;

                    // 消音开火音效
                    if (!__state.fireSoundChanged) { __state.fireSound = __instance.fireSound; __state.fireSoundChanged = true; }
                    var silenced = GetSilencedSoundForGun(gunItem);
                    if (silenced != null)
                        __instance.fireSound = silenced;

                    // 隐藏枪口火光
                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleWasEmitting = __instance.muzzleParticle.emission.enabled;
                        __state.muzzleParticle.gameObject.SetActive(false);
                    }
                }

                // ===== Hexagon DTKP 消音器效果：后坐力 -4%、听力 -50%、精准度 -4.5% + 无枪口火光 =====
                if (HasAttachment(DtkpItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.loudnessChanged) { __state.loudness = __instance.loudness; __state.loudnessChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= DtkpItemSystem.KnockBackMult;
                    __instance.loudness *= DtkpItemSystem.LoudnessMult;
                    if (!shotgun) __instance.verticalSpread *= DtkpItemSystem.SpreadMult;

                    // 消音开火音效
                    if (!__state.fireSoundChanged) { __state.fireSound = __instance.fireSound; __state.fireSoundChanged = true; }
                    var silenced = GetSilencedSoundForGun(gunItem);
                    if (silenced != null)
                        __instance.fireSound = silenced;

                    // 隐藏枪口火光
                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleWasEmitting = __instance.muzzleParticle.emission.enabled;
                        __state.muzzleParticle.gameObject.SetActive(false);
                    }
                }

                // ===== SilencerCo AC-858 膛口制退器效果：后坐力 -23% =====
                if (HasAttachment(Ac858ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= Ac858ItemSystem.KnockBackMult;
                }

                // ===== CGS Hekate DT 消音器效果：后坐力 -5%、听力 -65% + 无枪口火光 =====
                if (HasAttachment(HekateDt338ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.loudnessChanged) { __state.loudness = __instance.loudness; __state.loudnessChanged = true; }
                    __instance.knockBack *= HekateDt338ItemSystem.KnockBackMult;
                    __instance.loudness *= HekateDt338ItemSystem.LoudnessMult;

                    // 消音开火音效（AXMC 用专属 axmc_silenced.wav）
                    if (!__state.fireSoundChanged) { __state.fireSound = __instance.fireSound; __state.fireSoundChanged = true; }
                    var silenced = GetSilencedSoundForGun(gunItem);
                    if (silenced != null)
                        __instance.fireSound = silenced;

                    // 隐藏枪口火光
                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleWasEmitting = __instance.muzzleParticle.emission.enabled;
                        __state.muzzleParticle.gameObject.SetActive(false);
                    }
                }

                // ===== AI TMB 338LM 膛口制退器效果：后坐力 -20.5% =====
                if (HasAttachment(Tmb338lmItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= Tmb338lmItemSystem.KnockBackMult;
                }

                // ===== AI TSM 338LM 声音抑制器效果：后坐力 -5.5%、听力 -53% + 无枪口火光 =====
                if (HasAttachment(Tsm338lmItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.loudnessChanged) { __state.loudness = __instance.loudness; __state.loudnessChanged = true; }
                    __instance.knockBack *= Tsm338lmItemSystem.KnockBackMult;
                    __instance.loudness *= Tsm338lmItemSystem.LoudnessMult;

                    // 消音开火音效（AXMC 用专属 axmc_silenced.wav）
                    if (!__state.fireSoundChanged) { __state.fireSound = __instance.fireSound; __state.fireSoundChanged = true; }
                    var silenced = GetSilencedSoundForGun(gunItem);
                    if (silenced != null)
                        __instance.fireSound = silenced;

                    // 隐藏枪口火光
                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleWasEmitting = __instance.muzzleParticle.emission.enabled;
                        __state.muzzleParticle.gameObject.SetActive(false);
                    }
                }

                // ===== DVL-10 消音枪管枪口组合效果：后坐力 -15%、精准度 +5% + 消音枪声 =====
                if (HasAttachment(Dvl10SilencedItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= Dvl10SilencedItemSystem.KnockBackMult;
                    if (!shotgun) __instance.verticalSpread *= Dvl10SilencedItemSystem.SpreadMult;

                    // 更换枪声为消音版
                    if (!__state.fireSoundChanged) { __state.fireSound = __instance.fireSound; __state.fireSoundChanged = true; }
                    var dvlSilenced = Dvl10SilencedItemSystem.TryLoadSilencedSound();
                    if (dvlSilenced != null)
                        __instance.fireSound = dvlSilenced;
                }

                // ===== 格洛克套筒效果 =====
                // Glock Viper Cut 套筒：后坐力 -2%、每发耐久损耗 -5%
                if (HasAttachment(GlockViperCutItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.knockBack *= GlockViperCutItemSystem.KnockBackMult;
                    __instance.conditionLossPerShot *= GlockViperCutItemSystem.DurabilityMult;
                }
                // Polymer80 PS9 套筒：后坐力 -0.5%
                if (HasAttachment(GlockPs9ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= GlockPs9ItemSystem.KnockBackMult;
                }

                // ===== 格洛克枪口配件效果 =====
                // G 3 Port 补偿器：后坐力 -10%
                if (HasAttachment(GlockG3PortItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= GlockG3PortItemSystem.KnockBackMult;
                }
                // LW 9 补偿器：后坐力 -12%
                if (HasAttachment(GlockLw9ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= GlockLw9ItemSystem.KnockBackMult;
                }
                // Osprey 9 抑制器：后坐力 -7%、听力 -60%、每发耐久 +6.8% + 消音音效 + 取消火光
                if (HasAttachment(GlockOsprey9ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.loudnessChanged) { __state.loudness = __instance.loudness; __state.loudnessChanged = true; }
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.knockBack *= GlockOsprey9ItemSystem.KnockBackMult;
                    __instance.loudness *= GlockOsprey9ItemSystem.NoiseMult;
                    __instance.conditionLossPerShot *= GlockOsprey9ItemSystem.DurabilityMult;

                    if (!__state.fireSoundChanged) { __state.fireSound = __instance.fireSound; __state.fireSoundChanged = true; }
                    var ospreySilenced = GetSilencedSoundForGun(gunItem);
                    if (ospreySilenced != null)
                        __instance.fireSound = ospreySilenced;

                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleWasEmitting = __instance.muzzleParticle.emission.enabled;
                        __state.muzzleParticle.gameObject.SetActive(false);
                    }

                    if (__instance.barrel != null)
                    {
                        if (!__state.barrelOriginal.HasValue)
                            __state.barrelOriginal = __instance.barrel.localPosition;
                        __instance.barrel.localPosition -= new Vector3(SuppressorBarrelRetreat, 0f, 0f);
                    }
                }
                // SRD 9 抑制器：后坐力 -2%、听力 -30%、每发耐久 +0.8% + 消音音效 + 取消火光
                if (HasAttachment(GlockSrd9ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.loudnessChanged) { __state.loudness = __instance.loudness; __state.loudnessChanged = true; }
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.knockBack *= GlockSrd9ItemSystem.KnockBackMult;
                    __instance.loudness *= GlockSrd9ItemSystem.NoiseMult;
                    __instance.conditionLossPerShot *= GlockSrd9ItemSystem.DurabilityMult;

                    if (!__state.fireSoundChanged) { __state.fireSound = __instance.fireSound; __state.fireSoundChanged = true; }
                    var srdSilenced = GetSilencedSoundForGun(gunItem);
                    if (srdSilenced != null)
                        __instance.fireSound = srdSilenced;

                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleWasEmitting = __instance.muzzleParticle.emission.enabled;
                        __state.muzzleParticle.gameObject.SetActive(false);
                    }

                    if (__instance.barrel != null)
                    {
                        if (!__state.barrelOriginal.HasValue)
                            __state.barrelOriginal = __instance.barrel.localPosition;
                        __instance.barrel.localPosition -= new Vector3(SuppressorBarrelRetreat, 0f, 0f);
                    }
                }

                // ===== P90 Attenuator 消音器：后坐力 -10% + 消音音效 + 取消火光 =====
                if (HasAttachment(P90AttenuatorItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= P90AttenuatorItemSystem.KnockBackMult;

                    if (!__state.fireSoundChanged) { __state.fireSound = __instance.fireSound; __state.fireSoundChanged = true; }
                    var p90Silenced = P90AttenuatorItemSystem.TryLoadSilencedSound();
                    if (p90Silenced != null)
                        __instance.fireSound = p90Silenced;

                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleWasEmitting = __instance.muzzleParticle.emission.enabled;
                        __state.muzzleParticle.gameObject.SetActive(false);
                    }

                    if (__instance.barrel != null)
                    {
                        if (!__state.barrelOriginal.HasValue)
                            __state.barrelOriginal = __instance.barrel.localPosition;
                        __instance.barrel.localPosition -= new Vector3(SuppressorBarrelRetreat, 0f, 0f);
                    }
                }

                // ===== UMP OEM 消音器：噪音 -60%、后坐力 -7% + 消音音效 + 取消火光 =====
                if (HasAttachment(UmpOemItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.loudnessChanged) { __state.loudness = __instance.loudness; __state.loudnessChanged = true; }
                    __instance.knockBack *= UmpOemItemSystem.KnockBackMult;
                    __instance.loudness *= UmpOemItemSystem.LoudnessMult;

                    if (!__state.fireSoundChanged) { __state.fireSound = __instance.fireSound; __state.fireSoundChanged = true; }
                    var umpSilenced = UmpOemItemSystem.TryLoadSilencedSound();
                    if (umpSilenced != null)
                        __instance.fireSound = umpSilenced;

                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleWasEmitting = __instance.muzzleParticle.emission.enabled;
                        __state.muzzleParticle.gameObject.SetActive(false);
                    }

                    if (__instance.barrel != null)
                    {
                        if (!__state.barrelOriginal.HasValue)
                            __state.barrelOriginal = __instance.barrel.localPosition;
                        __instance.barrel.localPosition -= new Vector3(SuppressorBarrelRetreat, 0f, 0f);
                    }
                }

                // ===== AKM-L 护木效果：每发耐久损耗 -3% =====
                if (HasAttachment(AkmLItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.conditionLossPerShot *= AkmLItemSystem.ConditionLossMult;
                }

                // ===== M4 加长枪管效果：后坐力 -6%、精准度 +10%、伤害 +10 =====
                // 同时把火光和弹道起点往右挪（加长枪管枪口更靠右）
                if (HasAttachment(M4LongBarrelItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= M4LongBarrelItemSystem.KnockBackMult;
                    if (!shotgun) __instance.verticalSpread *= M4LongBarrelItemSystem.SpreadMult;
                    if (!__state.damageChanged) { __state.animalDamage = __instance.animalDamage; __state.damageChanged = true; }
                    __instance.animalDamage += M4LongBarrelItemSystem.DamageBonus;

                    // 火光位置往右挪（加长枪管枪口更靠右）
                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleOriginal = __instance.muzzleParticle.transform.localPosition;
                        __instance.muzzleParticle.transform.localPosition += new Vector3(LongBarrelMuzzleOffset, 0f, 0f);
                    }
                    // 弹道起点往右挪（GunScript.Fire 用 barrel.position 作为弹道起点）
                    if (__instance.barrel != null)
                    {
                        __state.barrelOriginal = __instance.barrel.localPosition;
                        __instance.barrel.localPosition += new Vector3(LongBarrelMuzzleOffset, 0f, 0f);
                    }
                }

                // ===== Rotor 43 消音器效果（M4）：后坐力 -2.5%、听力损伤 -40%、每发耐久 +12% =====
                // 同时使用消音开火音效 + 取消枪口火光 + 弹道起点后移（与通用消音器一致）
                if (HasAttachment(Rotor43ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.loudnessChanged) { __state.loudness = __instance.loudness; __state.loudnessChanged = true; }
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.knockBack *= Rotor43ItemSystem.KnockBackMult;
                    __instance.loudness *= Rotor43ItemSystem.LoudnessMult;
                    __instance.conditionLossPerShot *= Rotor43ItemSystem.ConditionLossMult;

                    // 消音开火音效
                    if (!__state.fireSoundChanged) { __state.fireSound = __instance.fireSound; __state.fireSoundChanged = true; }
                    var silenced = GetSilencedSoundForGun(gunItem);
                    if (silenced != null)
                        __instance.fireSound = silenced;

                    // 取消枪口火光（禁用整个物体）
                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleWasEmitting = __instance.muzzleParticle.emission.enabled;
                        __state.muzzleParticle.gameObject.SetActive(false);
                    }

                    // 弹道起点临时往后挪（消音器在枪口前，弹道从消音器口发出）
                    // 只在未保存时保存原位置，避免覆盖加长枪管已保存的最初位置
                    if (__instance.barrel != null)
                    {
                        if (!__state.barrelOriginal.HasValue)
                            __state.barrelOriginal = __instance.barrel.localPosition;
                        __instance.barrel.localPosition -= new Vector3(SuppressorBarrelRetreat, 0f, 0f);
                    }
                }

                // ===== KAC NT-4 消音器效果（M4）：后坐力 -6%、听力 -50%、精准度 -1%、每发耐久 +9% =====
                // 消音音效 + 取消火光
                if (HasAttachment(Nt4ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.loudnessChanged) { __state.loudness = __instance.loudness; __state.loudnessChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.knockBack *= Nt4ItemSystem.KnockBackMult;
                    __instance.loudness *= Nt4ItemSystem.LoudnessMult;
                    if (!shotgun) __instance.verticalSpread *= Nt4ItemSystem.SpreadMult;
                    __instance.conditionLossPerShot *= Nt4ItemSystem.ConditionLossMult;

                    if (!__state.fireSoundChanged) { __state.fireSound = __instance.fireSound; __state.fireSoundChanged = true; }
                    var silenced = GetSilencedSoundForGun(gunItem);
                    if (silenced != null)
                        __instance.fireSound = silenced;

                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleWasEmitting = __instance.muzzleParticle.emission.enabled;
                        __state.muzzleParticle.gameObject.SetActive(false);
                    }

                    if (__instance.barrel != null)
                    {
                        if (!__state.barrelOriginal.HasValue)
                            __state.barrelOriginal = __instance.barrel.localPosition;
                        __instance.barrel.localPosition -= new Vector3(SuppressorBarrelRetreat, 0f, 0f);
                    }
                }

                // ===== SilencerCo SAKER ASR 556 消音器效果（M4）：后坐力 -8.5%、听力 -48%、精准度 -2.2%、每发耐久 +7.5% =====
                // 消音音效 + 取消火光
                if (HasAttachment(SakerItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.loudnessChanged) { __state.loudness = __instance.loudness; __state.loudnessChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.knockBack *= SakerItemSystem.KnockBackMult;
                    __instance.loudness *= SakerItemSystem.LoudnessMult;
                    if (!shotgun) __instance.verticalSpread *= SakerItemSystem.SpreadMult;
                    __instance.conditionLossPerShot *= SakerItemSystem.ConditionLossMult;

                    if (!__state.fireSoundChanged) { __state.fireSound = __instance.fireSound; __state.fireSoundChanged = true; }
                    var silenced = GetSilencedSoundForGun(gunItem);
                    if (silenced != null)
                        __instance.fireSound = silenced;

                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleWasEmitting = __instance.muzzleParticle.emission.enabled;
                        __state.muzzleParticle.gameObject.SetActive(false);
                    }

                    if (__instance.barrel != null)
                    {
                        if (!__state.barrelOriginal.HasValue)
                            __state.barrelOriginal = __instance.barrel.localPosition;
                        __instance.barrel.localPosition -= new Vector3(SuppressorBarrelRetreat, 0f, 0f);
                    }
                }

                // ===== Noveske KX3 消焰器效果（M4）：后坐力 -5%、每发耐久 -5% =====
                if (HasAttachment(Kx3ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.knockBack *= Kx3ItemSystem.KnockBackMult;
                    __instance.conditionLossPerShot *= Kx3ItemSystem.ConditionLossMult;
                }

                // ===== Vendetta VP-09 膛口制退器效果（M4）：后坐力 -7.5% =====
                if (HasAttachment(Vp09ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    __instance.knockBack *= Vp09ItemSystem.KnockBackMult;
                }

                // ===== Rotor 43 7.62x39 消音器效果（AKM）：后坐力 -3%、听力 -50%、每发耐久 +15% =====
                // 消音音效 + 取消火光
                if (HasAttachment(Rotor43762ItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.loudnessChanged) { __state.loudness = __instance.loudness; __state.loudnessChanged = true; }
                    if (!__state.conditionChanged) { __state.conditionLossPerShot = __instance.conditionLossPerShot; __state.conditionChanged = true; }
                    __instance.knockBack *= Rotor43762ItemSystem.KnockBackMult;
                    __instance.loudness *= Rotor43762ItemSystem.LoudnessMult;
                    __instance.conditionLossPerShot *= Rotor43762ItemSystem.ConditionLossMult;

                    if (!__state.fireSoundChanged) { __state.fireSound = __instance.fireSound; __state.fireSoundChanged = true; }
                    var silenced = GetSilencedSoundForGun(gunItem);
                    if (silenced != null)
                        __instance.fireSound = silenced;

                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleWasEmitting = __instance.muzzleParticle.emission.enabled;
                        __state.muzzleParticle.gameObject.SetActive(false);
                    }

                    if (__instance.barrel != null)
                    {
                        if (!__state.barrelOriginal.HasValue)
                            __state.barrelOriginal = __instance.barrel.localPosition;
                        __instance.barrel.localPosition -= new Vector3(SuppressorBarrelRetreat, 0f, 0f);
                    }
                }

                // ===== Hexagon SKS 声音抑制器效果（SKS）：后坐力 -1.3%、听力 -65%、精准度 -5% =====
                // 消音音效 + 取消火光
                if (HasAttachment(HexagonSksItemSystem.ItemKey))
                {
                    __state.active = true;
                    if (!__state.knockBackChanged) { __state.knockBack = __instance.knockBack; __state.knockBackChanged = true; }
                    if (!__state.loudnessChanged) { __state.loudness = __instance.loudness; __state.loudnessChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.knockBack *= HexagonSksItemSystem.KnockBackMult;
                    __instance.loudness *= HexagonSksItemSystem.LoudnessMult;
                    if (!shotgun) __instance.verticalSpread *= HexagonSksItemSystem.SpreadMult;

                    if (!__state.fireSoundChanged) { __state.fireSound = __instance.fireSound; __state.fireSoundChanged = true; }
                    var silenced = GetSilencedSoundForGun(gunItem);
                    if (silenced != null)
                        __instance.fireSound = silenced;

                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleWasEmitting = __instance.muzzleParticle.emission.enabled;
                        __state.muzzleParticle.gameObject.SetActive(false);
                    }

                    if (__instance.barrel != null)
                    {
                        if (!__state.barrelOriginal.HasValue)
                            __state.barrelOriginal = __instance.barrel.localPosition;
                        __instance.barrel.localPosition -= new Vector3(SuppressorBarrelRetreat, 0f, 0f);
                    }
                }

                // ===== 消音器效果 =====
                if (IsSuppressorAttached(gunItem))
                {
                    __state.active = true;
                    if (!__state.loudnessChanged) { __state.loudness = __instance.loudness; __state.loudnessChanged = true; }
                    if (!__state.fireSoundChanged) { __state.fireSound = __instance.fireSound; __state.fireSoundChanged = true; }
                    if (!__state.spreadChanged) { __state.verticalSpread = __instance.verticalSpread; __state.spreadChanged = true; }
                    __instance.loudness *= LoudnessMult;
                    if (!shotgun) __instance.verticalSpread *= SuppressorSpreadMult; // 精准度 -5%
                    var silenced = GetSilencedSoundForGun(gunItem);
                    if (silenced != null)
                        __instance.fireSound = silenced;

                    // 消音器：隐藏枪口火光（Play() 会无视 emission.enabled 强制发射，需禁用整个物体）
                    if (__instance.muzzleParticle != null)
                    {
                        __state.muzzleParticle = __instance.muzzleParticle;
                        __state.muzzleWasEmitting = __instance.muzzleParticle.emission.enabled;
                        __state.muzzleParticle.gameObject.SetActive(false);
                    }

                    // 消音器：弹道起点临时往后挪（GunScript.Fire 用 barrel.position 作为弹道起点）
                    if (__instance.barrel != null)
                    {
                        __state.barrelOriginal = __instance.barrel.localPosition;
                        __instance.barrel.localPosition -= new Vector3(SuppressorBarrelRetreat, 0f, 0f);
                    }
                }

                // ===== 连发枪口上下交替（垂直方向）=====
                // 连发时枪口不只是向上仰，还会时不时向下（交替方向），扰乱压枪节奏。
                // 只计算偏移量存到 __state，由 Postfix 在 Fire 之后覆盖 gunangle，
                // 避免被原版 Fire 的 gunangle += knockBack*8 抵消。
                // 必须设置 active=true，否则 Postfix 会因 !active 提前返回，连发偏移不生效。
                __state.burstGunAngleDelta = GetBurstGunAngleDelta(gunItem, __instance);
                if (__state.burstGunAngleDelta != 0f)
                    __state.active = true;
            }
            catch { }
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(GunScript __instance, State __state)
        {
            // 清除 Fire 时机标志（必须在 early return 之前，确保每次 Fire 后都复位）
            InFire = false;
            if (__state == null || !__state.active) return;
            try
            {
                if (__state.loudnessChanged) __instance.loudness = __state.loudness;
                if (__state.knockBackChanged) __instance.knockBack = __state.knockBack;
                if (__state.conditionChanged) __instance.conditionLossPerShot = __state.conditionLossPerShot;
                if (__state.fireSoundChanged) __instance.fireSound = __state.fireSound;
                if (__state.spreadChanged) __instance.verticalSpread = __state.verticalSpread;
                if (__state.damageChanged) __instance.animalDamage = __state.animalDamage;

                // 恢复枪口火光
                if (__state.muzzleParticle != null)
                {
                    __state.muzzleParticle.gameObject.SetActive(true);
                    var emission = __state.muzzleParticle.emission;
                    emission.enabled = __state.muzzleWasEmitting;
                    // 恢复火光位置（加长枪管时被往右挪）
                    if (__state.muzzleOriginal.HasValue)
                        __state.muzzleParticle.transform.localPosition = __state.muzzleOriginal.Value;
                }

                // 恢复弹道起点
                if (__state.barrelOriginal.HasValue && __instance.barrel != null)
                    __instance.barrel.localPosition = __state.barrelOriginal.Value;

                // ===== 连发枪口上下交替（覆盖应用）=====
                // 在 Fire 之后覆盖 gunangle，让向下偏移真正生效（不被原版上抬抵消）。
                if (__state.burstGunAngleDelta != 0f)
                {
                    var gunBody = GetGunBody(__instance);
                    if (gunBody != null && gunBody.armsAnimator != null)
                    {
                        float current = gunBody.armsAnimator.GetFloat("gunangle");
                        gunBody.armsAnimator.SetFloat("gunangle", current + __state.burstGunAngleDelta);
                    }
                }
            }
            catch { }
        }
    }

    // ===== 面板打开时抑制玩家操作 =====
    // 改枪面板打开时，禁止开火/使用物品（左键点击面板按钮会触发 UseItemInHand）。

    [HarmonyPatch(typeof(Body), nameof(Body.UseItemInHand))]
    public static class UseItemInHandLockPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            // 面板打开或检查弹匣期间（含点击瞬间），抑制开火/使用物品
            return !GunsmithPanel.IsOpen && !Hhs1ZoomUiPatch.ShouldSuppressFire;
        }
    }

    [HarmonyPatch(typeof(Body), nameof(Body.UseItem))]
    public static class UseItemLockPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return !GunsmithPanel.IsOpen;
        }
    }

    // ===== 面板打开时锁定全部玩家输入 =====
    // 改枪面板是一次性构建的（BuildPanel 只在打开时执行一次），若玩家在面板打开期间
    // 行走/开关背包/捡东西，面板内容会过期（新捡的配件不显示、工具钳状态不刷新）。
    // 因此面板打开时完全禁止玩家操作：移动、跳跃、扔东西、切手、开关背包、拾取、交互等
    // 全部走 PlayerCamera.HandleInput，在此拦截即可。G 键开关面板在 Plugin.Update 中独立检测，不受影响。

    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.HandleInput))]
    public static class PlayerInputLockPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return !GunsmithPanel.IsOpen;
        }

        [HarmonyPostfix]
        public static void Postfix(PlayerCamera __instance)
        {
            // 驱动瞄准系统：检测手持枪并驱动 AimController
            AimSystem.TickPlayerAim(__instance);
        }
    }

    // ===== 枪械配件视觉（方案 A：运行时纹理合成）=====
    // 配件像素 alpha 合成进主枪械贴图，生成一张新贴图替换 GunScript 状态贴图。
    // 配件与枪械是同一张图，任何来源的抖动（帧动画/transform 旋转）都天然同步，
    // 彻底解决子物体 SpriteRenderer 叠加导致的"抖动延迟不一/时动时不动"。

    public static void UpdateSuppressorVisual(Item gunItem) => RebuildVisual(gunItem);
    public static void UpdateHandguardVisual(Item gunItem) => RebuildVisual(gunItem);
    public static void UpdateMagVisual(Item gunItem) => RebuildVisual(gunItem);

    /// <summary>根据枪械类型分发到对应的视觉合成器。</summary>
    private static void RebuildVisual(Item gunItem)
    {
        if (gunItem == null) return;
        if (string.Equals(gunItem.id, M4A1ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
        {
            M4VisualComposer.Rebuild(gunItem);
            return;
        }
        GunVisualComposer.Rebuild(gunItem);
    }

    // ===== 消音音效 =====

    public static AudioClip? TryLoadSilencedSound()
    {
        if (_cachedSilencedSound != null) return _cachedSilencedSound;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var soundPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "akm", "akm_silenced.wav");
            if (File.Exists(soundPath))
                _cachedSilencedSound = LoadWavSync(soundPath);
            if (_cachedSilencedSound != null)
                Plugin.Log.LogInfo("[Suppressor] Loaded silenced sound 'akm_silenced.wav'.");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Suppressor] Silenced sound: {ex.Message}"); }
        return _cachedSilencedSound;
    }

    // AXMC 专属消音音效（axmc_silenced.wav）
    private static AudioClip? _cachedAxmcSilencedSound;

    public static AudioClip? TryLoadAxmcSilencedSound()
    {
        if (_cachedAxmcSilencedSound != null) return _cachedAxmcSilencedSound;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var soundPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "ax", "axmc_silenced.wav");
            if (File.Exists(soundPath))
                _cachedAxmcSilencedSound = LoadWavSync(soundPath);
            if (_cachedAxmcSilencedSound != null)
                Plugin.Log.LogInfo("[Suppressor] Loaded AXMC silenced sound 'axmc_silenced.wav'.");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Suppressor] AXMC silenced sound: {ex.Message}"); }
        return _cachedAxmcSilencedSound;
    }

    // AKM 专属消音音效（akm_silenced.wav）
    private static AudioClip? _cachedAkmSilencedSound;

    public static AudioClip? TryLoadAkmSilencedSound()
    {
        if (_cachedAkmSilencedSound != null) return _cachedAkmSilencedSound;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var soundPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "akm", "akm_silenced.wav");
            if (File.Exists(soundPath))
                _cachedAkmSilencedSound = LoadWavSync(soundPath);
            if (_cachedAkmSilencedSound != null)
                Plugin.Log.LogInfo("[Suppressor] Loaded AKM silenced sound 'akm_silenced.wav'.");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Suppressor] AKM silenced sound: {ex.Message}"); }
        return _cachedAkmSilencedSound;
    }

    // M4A1 专属消音音效（m4a1_silenced.wav）
    private static AudioClip? _cachedM4SilencedSound;

    public static AudioClip? TryLoadM4SilencedSound()
    {
        if (_cachedM4SilencedSound != null) return _cachedM4SilencedSound;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var soundPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "m4", "m4a1_silenced.wav");
            if (File.Exists(soundPath))
                _cachedM4SilencedSound = LoadWavSync(soundPath);
            if (_cachedM4SilencedSound != null)
                Plugin.Log.LogInfo("[Suppressor] Loaded M4A1 silenced sound 'm4a1_silenced.wav'.");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Suppressor] M4A1 silenced sound: {ex.Message}"); }
        return _cachedM4SilencedSound;
    }

    // SKS 专属消音音效（sks_silenced.wav）
    private static AudioClip? _cachedSksSilencedSound;

    public static AudioClip? TryLoadSksSilencedSound()
    {
        if (_cachedSksSilencedSound != null) return _cachedSksSilencedSound;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var soundPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "sks", "sks_silenced.wav");
            if (File.Exists(soundPath))
                _cachedSksSilencedSound = LoadWavSync(soundPath);
            if (_cachedSksSilencedSound != null)
                Plugin.Log.LogInfo("[Suppressor] Loaded SKS silenced sound 'sks_silenced.wav'.");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Suppressor] SKS silenced sound: {ex.Message}"); }
        return _cachedSksSilencedSound;
    }

    // 格洛克专属消音音效（glock17_silenced.wav）
    private static AudioClip? _cachedGlockSilencedSound;

    public static AudioClip? TryLoadGlockSilencedSound()
    {
        if (_cachedGlockSilencedSound != null) return _cachedGlockSilencedSound;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var soundPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "glock", "glock17_silenced.wav");
            if (File.Exists(soundPath))
                _cachedGlockSilencedSound = LoadWavSync(soundPath);
            if (_cachedGlockSilencedSound != null)
                Plugin.Log.LogInfo("[Suppressor] Loaded Glock silenced sound 'glock17_silenced.wav'.");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Suppressor] Glock silenced sound: {ex.Message}"); }
        return _cachedGlockSilencedSound;
    }

    /// <summary>
    /// 按枪械选择消音开火音效（每把枪使用各自的消音枪声）：
    /// - AKM：akm_silenced.wav
    /// - M4A1：m4a1_silenced.wav
    /// - SKS：sks_silenced.wav
    /// - AXMC：axmc_silenced.wav
    /// - 格洛克：glock17_silenced.wav
    /// - DVL-10：dvl_silenced.wav（由 Dvl10SilencedItemSystem 处理）
    /// - 其他枪：akm_silenced.wav（通用消音音效）
    /// </summary>
    public static AudioClip? GetSilencedSoundForGun(Item gunItem)
    {
        if (gunItem == null) return TryLoadSilencedSound();
        if (IsAkmGun(gunItem)) return TryLoadAkmSilencedSound();
        if (IsM4Gun(gunItem)) return TryLoadM4SilencedSound();
        if (IsSksGun(gunItem)) return TryLoadSksSilencedSound();
        if (IsAxmcGun(gunItem)) return TryLoadAxmcSilencedSound();
        if (IsGlockGun(gunItem)) return TryLoadGlockSilencedSound();
        return TryLoadSilencedSound();
    }

    private static AudioClip? LoadWavSync(string path)
    {
        try
        {
            using var uwr = UnityWebRequestMultimedia.GetAudioClip("file:///" + path, AudioType.WAV);
            uwr.SendWebRequest();
            while (!uwr.isDone) { }
            if (uwr.result == UnityWebRequest.Result.Success)
                return DownloadHandlerAudioClip.GetContent(uwr);
        }
        catch { }
        return null;
    }
}