using System;
using System.Collections.Generic;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 工具系统：检测玩家背包/身上是否有指定工具物品（如 Leatherman 工具钳），
/// 并管理配件安装的前提条件（如护木是握把/战术手电/激光的前提）。
///
/// 用法：
///   - 制作需要工具钳的配件时调用 <see cref="RegisterAttachmentRequiringLeatherman"/>；
///     改枪面板（<see cref="GunsmithPanel"/>）会检查玩家是否有对应工具，没有则禁用安装按钮并显示提示。
///   - 制作有安装前提的配件时调用 <see cref="RegisterPrerequisite"/>（如握把依赖护木）；
///     面板会检查枪上是否已装有前提配件，未装则禁用并提示"需先安装 XX"。
///
/// 当前已注册工具：<see cref="LeathermanItemSystem.ItemKey"/>（"leatherman"）。
/// </summary>
public static class ToolSystem
{
    /// <summary>所有已注册的工具物品 ID（玩家持有任一即视为有工具）。</summary>
    public static readonly HashSet<string> ToolItemIds = new(StringComparer.OrdinalIgnoreCase)
    {
        LeathermanItemSystem.ItemKey,
    };

    /// <summary>需要 Leatherman 工具钳才能安装的配件 ID 集合。</summary>
    public static readonly HashSet<string> AttachmentsRequiringLeatherman = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>配件安装前提：配件 ID → 必须已安装在枪上的前提配件 ID 列表（如握把依赖护木）。</summary>
    public static readonly Dictionary<string, List<string>> AttachmentPrerequisites = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>OR 前提组：配件 ID → 可选前提配件 ID 列表（组内任一已装即满足，如战术设备依赖 MOE 或 Hexagon 护木）。</summary>
    public static readonly Dictionary<string, List<string>> OrPrerequisiteGroups = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 检测玩家 body.slots 顶层是否持有指定工具物品（或任一已注册工具物品）。
    /// 检查范围：仅顶层槽位（与 GetAvailableAttachments 一致）。
    /// </summary>
    public static bool HasTool(Body body, string toolItemId)
    {
        if (body == null) return false;
        try
        {
            foreach (var slot in body.slots)
            {
                if (slot == null) continue;
                var item = body.GetItem(slot.slot);
                if (item != null && string.Equals(item.id, toolItemId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[ToolSystem] HasTool failed: {ex.Message}"); }
        return false;
    }

    /// <summary>检测玩家是否持有任一已注册工具物品。</summary>
    public static bool HasAnyTool(Body body)
    {
        if (body == null) return false;
        try
        {
            foreach (var slot in body.slots)
            {
                if (slot == null) continue;
                var item = body.GetItem(slot.slot);
                if (item != null && ToolItemIds.Contains(item.id)) return true;
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[ToolSystem] HasAnyTool failed: {ex.Message}"); }
        return false;
    }

    /// <summary>注册一个需要 Leatherman 工具钳才能安装的配件。</summary>
    public static void RegisterAttachmentRequiringLeatherman(string attachmentId)
    {
        if (string.IsNullOrEmpty(attachmentId)) return;
        AttachmentsRequiringLeatherman.Add(attachmentId);
        Plugin.Log.LogInfo($"[ToolSystem] Registered attachment '{attachmentId}' as requiring Leatherman.");
    }

    /// <summary>查询配件是否需要工具钳安装。</summary>
    public static bool AttachmentRequiresLeatherman(string attachmentId)
    {
        if (string.IsNullOrEmpty(attachmentId)) return false;
        return AttachmentsRequiringLeatherman.Contains(attachmentId);
    }

    /// <summary>注册配件安装前提：安装 attachmentId 前必须先安装 prerequisiteId。</summary>
    public static void RegisterPrerequisite(string attachmentId, string prerequisiteId)
    {
        if (string.IsNullOrEmpty(attachmentId) || string.IsNullOrEmpty(prerequisiteId)) return;
        if (!AttachmentPrerequisites.TryGetValue(attachmentId, out var list))
        {
            list = new List<string>();
            AttachmentPrerequisites[attachmentId] = list;
        }
        if (!list.Contains(prerequisiteId))
            list.Add(prerequisiteId);
        Plugin.Log.LogInfo($"[ToolSystem] Registered prerequisite: '{attachmentId}' requires '{prerequisiteId}' installed.");
    }

    /// <summary>
    /// 注册 OR 前提组：安装 attachmentId 前，组内任一 alternativeId 已安装即满足。
    /// 例如战术设备（手电/激光）可装在 MOE 或 Hexagon 护木上。
    /// </summary>
    public static void RegisterOrPrerequisite(string attachmentId, params string[] alternativeIds)
    {
        if (string.IsNullOrEmpty(attachmentId) || alternativeIds == null || alternativeIds.Length == 0) return;
        var list = new List<string>();
        foreach (var a in alternativeIds)
            if (!string.IsNullOrEmpty(a) && !list.Contains(a)) list.Add(a);
        if (list.Count == 0) return;
        OrPrerequisiteGroups[attachmentId] = list;
        Plugin.Log.LogInfo($"[ToolSystem] Registered OR-prerequisite: '{attachmentId}' requires any of [{string.Join(", ", list)}] installed.");
    }

    /// <summary>获取指定配件的全部前提配件 ID（无前提返回 null）。</summary>
    public static List<string>? GetPrerequisites(string attachmentId)
    {
        if (string.IsNullOrEmpty(attachmentId)) return null;
        return AttachmentPrerequisites.TryGetValue(attachmentId, out var list) ? list : null;
    }

    /// <summary>
    /// 检查枪上是否已满足配件的全部安装前提。
    /// </summary>
    public static bool HasPrerequisites(Item gun, string attachmentId)
    {
        var prereqs = GetPrerequisites(attachmentId);
        if (prereqs != null && prereqs.Count > 0)
        {
            if (gun == null) return false;
            var holder = gun.GetComponent<GunAttachmentHolder>();
            if (holder == null) return false;
            foreach (var p in prereqs)
                if (!holder.attachmentIds.Contains(p)) return false;
        }
        // OR 前提组：组内任一已装即满足。
        // 仅对 AKM 系枪生效（战术设备/前握把需改装护木），非 AKM 枪无护木前提。
        if (OrPrerequisiteGroups.TryGetValue(attachmentId, out var alts))
        {
            bool isAkm = gun != null
                && gun.id.IndexOf("akm", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isAkm)
            {
                if (gun == null) return false;
                var holder = gun.GetComponent<GunAttachmentHolder>();
                if (holder == null) return false;
                bool any = false;
                foreach (var a in alts)
                    if (holder.attachmentIds.Contains(a)) { any = true; break; }
                // 已装任一非 WASR 护木（如 B10M+B19）也算满足护木前提
                if (!any && SuppressorSystem.HasAnyHandguardExceptWasr(gun)) any = true;
                if (!any) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 返回枪上缺失的前提配件 ID 列表（已满足返回空列表）。
    /// </summary>
    public static List<string> GetMissingPrerequisites(Item gun, string attachmentId)
    {
        var missing = new List<string>();
        // SKS 专属配件（UAS/Tapco/SKS MC/Hexagon SKS/WT0032-1）：仅 SKS 可安装，其他枪械视为缺前提（隐藏+拦截）
        if (SuppressorSystem.IsSksOnlyItem(attachmentId)
            && (gun == null || !SuppressorSystem.IsSksGun(gun)))
        {
            missing.Add("sks_only");
            return missing;
        }
        // AXMC 专属配件（AC-858/Hekate/TMB/TSM/握把垫）：仅 AXMC 可安装，其他枪械视为缺前提（隐藏+拦截）
        if (SuppressorSystem.IsAxmcOnlyItem(attachmentId)
            && (gun == null || !SuppressorSystem.IsAxmcGun(gun)))
        {
            missing.Add("axmc_only");
            return missing;
        }
        // DVL 专属配件（DVL-10 消音套件）：仅 DVL 可安装，其他枪械视为缺前提（隐藏+拦截）
        if (SuppressorSystem.IsDvl10OnlyItem(attachmentId)
            && (gun == null || !SuppressorSystem.IsDvl10Gun(gun)))
        {
            missing.Add("dvl10_only");
            return missing;
        }
        // DVL 限制：前握把不可装；装消音套件后不可装战术设备（互斥）
        if (SuppressorSystem.IsDvl10Gun(gun) && SuppressorSystem.IsForegripItem(attachmentId))
        {
            missing.Add("dvl10_no_foregrip");
            return missing;
        }
        // DVL 限制：装消音套件后不可装战术设备（互斥）
        if (SuppressorSystem.IsAttachmentBlockedForDvl10(gun, attachmentId))
        {
            missing.Add("dvl10_silenced_tactical_conflict");
            return missing;
        }
        // TSM 338LM 需要先装 TMB 338LM（类似 SKS 转接器效果）
        if (string.Equals(attachmentId, Tsm338lmItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
            && (gun == null || !SuppressorSystem.IsAttachmentInstalled(gun, Tmb338lmItemSystem.ItemKey)))
        {
            missing.Add("tmb338lm_required");
            return missing;
        }
        // M4 专属配件（MOE SL/Viper/KAC/长枪管护木/加长枪管/M4枪口/M4后托等）：仅 M4 可安装
        // 例外：SKS 装了 Tapco INTRAFUSE 后可安装 M4 系列后托（Tapco 有缓冲管接口）
        if (SuppressorSystem.IsM4OnlyItem(attachmentId) && !SuppressorSystem.IsM4Gun(gun))
        {
            bool sksTapcoStock = gun != null
                && SuppressorSystem.IsSksGun(gun)
                && SuppressorSystem.IsStockItem(attachmentId)
                && SuppressorSystem.IsAttachmentInstalled(gun, TapcoIntrafuseItemSystem.ItemKey);
            if (!sksTapcoStock)
            {
                missing.Add("m4_only");
                return missing;
            }
        }
        // AKM 专属配件（护木/后托/后握把）：仅 AKM 系枪可安装，其他枪械视为缺前提（隐藏+拦截）
        // 例外1：SKS 装了 WT0032-1 螺纹转换器后可安装 DTK-1 等膛口装置
        // 例外2：SKS 专属配件（UAS/Tapco/SKS MC/Hexagon SKS/WT0032-1）不受 AKM 专属限制
        if (SuppressorSystem.IsAkmOnlyItem(attachmentId) && !SuppressorSystem.IsAkmGun(gun))
        {
            bool sksWt0032Muzzle = gun != null
                && SuppressorSystem.IsSksGun(gun)
                && SuppressorSystem.IsMuzzleDeviceRequiresWt0032(attachmentId)
                && SuppressorSystem.IsAttachmentInstalled(gun, Wt0032_1ItemSystem.ItemKey);
            bool sksOnlyExempt = gun != null
                && SuppressorSystem.IsSksGun(gun)
                && SuppressorSystem.IsSksOnlyItem(attachmentId);
            if (!sksWt0032Muzzle && !sksOnlyExempt)
            {
                missing.Add("akm_only");
                return missing;
            }
        }
        // M4A1 初始限制：无法安装战术设备和前握把（原厂护木无下导轨），视为缺前提（隐藏+拦截）
        if (SuppressorSystem.IsAttachmentBlockedForM4(gun, attachmentId))
        {
            missing.Add("m4_no_rail");
            return missing;
        }
        // SKS 原厂限制：原厂 SKS 不可装护木/瞄准镜/前后握把/手电；装 UAS 后仅白名单瞄准镜+手电
        if (SuppressorSystem.IsAttachmentBlockedForSks(gun, attachmentId))
        {
            missing.Add("sks_restriction");
            return missing;
        }
        // 沙鹰限制：瞄准镜仅白名单（553/MRS/微型速瞄）
        if (SuppressorSystem.IsAttachmentBlockedForDeagle(gun, attachmentId))
        {
            missing.Add("deagle_sight_whitelist");
            return missing;
        }
        // P90 限制：原厂只能改装枪口（Attenuator 消音器）
        if (SuppressorSystem.IsAttachmentBlockedForP90(gun, attachmentId))
        {
            missing.Add("p90_restriction");
            return missing;
        }
        // UMP 专属配件（B&T OEM .45 ACP UMP 消音器）：仅 UMP 可安装
        if (SuppressorSystem.IsUmpOnlyItem(attachmentId)
            && (gun == null || !SuppressorSystem.IsUmpGun(gun)))
        {
            missing.Add("ump_only");
            return missing;
        }
        // UMP 瞄准镜限制：Razor HD / PM II 不可安装
        if (SuppressorSystem.IsUmpGun(gun)
            && SuppressorSystem.IsSightItem(attachmentId)
            && (string.Equals(attachmentId, RazorHdItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(attachmentId, Pm2ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)))
        {
            missing.Add("ump_sight_restriction");
            return missing;
        }
        // UMP 限制：原厂可装前握把/枪口/手电/瞄准镜（Razor HD、PM II 除外）
        if (SuppressorSystem.IsAttachmentBlockedForUmp(gun, attachmentId))
        {
            missing.Add("ump_restriction");
            return missing;
        }
        // 格洛克专属配件：非格洛克枪不可装；格洛克枪若因枪口未装 AW 螺纹则由下方 AND 前提提示
        if (SuppressorSystem.IsGlockOnlyItem(attachmentId)
            && (gun == null || !SuppressorSystem.IsGlockGun(gun)))
        {
            missing.Add("glock_only");
            return missing;
        }
        if (gun != null && SuppressorSystem.IsGlockGun(gun)
            && SuppressorSystem.IsAttachmentBlockedForGlock(gun, attachmentId))
        {
            // 格洛克瞄准镜：优先提示需要 UM3 基座
            if (SuppressorSystem.IsSightItem(attachmentId)
                && !SuppressorSystem.IsAttachmentInstalled(gun, GlockUm3ItemSystem.ItemKey))
            {
                missing.Add("um3_required");
                return missing;
            }
            bool muzzleNoAw = SuppressorSystem.IsMuzzleItem(attachmentId)
                && !SuppressorSystem.IsAttachmentInstalled(gun, GlockAwlwItemSystem.ItemKey);
            if (!muzzleNoAw)
            {
                missing.Add("glock_restriction");
                return missing;
            }
        }
        // M4 短护木（MOE SL/Viper/KAC RIS）与加长枪管互斥：装了加长枪管则短护木视为缺前提（隐藏+拦截）
        if (gun != null
            && SuppressorSystem.IsM4Gun(gun)
            && SuppressorSystem.IsHandguardItem(attachmentId)
            && !SuppressorSystem.IsLongBarrelHandguardItem(attachmentId)
            && SuppressorSystem.IsAttachmentInstalled(gun, M4LongBarrelItemSystem.ItemKey))
        {
            missing.Add("long_barrel_conflict");
            return missing;
        }
        // M4 长枪管专属护木：必须先装加长枪管，否则视为缺前提（隐藏+拦截）
        if (gun != null
            && SuppressorSystem.IsM4Gun(gun)
            && SuppressorSystem.IsLongBarrelHandguardItem(attachmentId)
            && !SuppressorSystem.IsAttachmentInstalled(gun, M4LongBarrelItemSystem.ItemKey))
        {
            missing.Add("long_barrel_required");
            return missing;
        }
        var prereqs = GetPrerequisites(attachmentId);
        if (prereqs != null && prereqs.Count > 0)
        {
            // 原厂即可装瞄准镜的枪械（M4/AXMC/DVL/沙鹰）豁免 PDC 前提（集中管理，见 IsGunSightExempt）
            bool gunSightExempt = gun != null
                && SuppressorSystem.IsSightItem(attachmentId)
                && SuppressorSystem.IsGunSightExempt(gun);
            // SKS 装了 UAS 后：UAS 自带导轨，白名单瞄准镜（553/MRS/微型速瞄）无需 PDC 前提。
            // 否则 SKS 永远无法装瞄准镜（PDC 是 AKM 专属，SKS 装不了）。
            bool sksUasSightExempt = gun != null
                && SuppressorSystem.IsSksGun(gun)
                && SuppressorSystem.IsAttachmentInstalled(gun, UasSksItemSystem.ItemKey)
                && SuppressorSystem.IsSightItem(attachmentId)
                && SuppressorSystem.IsSightAllowedForUas(attachmentId);
            // SKS 装了 MTU017 机匣基座后：可安装所有瞄准镜（无需 PDC 前提）
            bool sksMtu017SightExempt = gun != null
                && SuppressorSystem.IsSksGun(gun)
                && SuppressorSystem.IsAttachmentInstalled(gun, Mtu017ItemSystem.ItemKey)
                && SuppressorSystem.IsSightItem(attachmentId);
            if (gun == null) { missing.AddRange(prereqs); }
            else
            {
                var holder = gun.GetComponent<GunAttachmentHolder>();
                if (holder == null)
                {
                    // 原厂枪（无 GunAttachmentHolder，未装任何配件）：所有 AND 前提都缺失，
                    // 但原厂可装瞄准镜的枪械豁免（无需 PDC 前提）仍须生效。
                    foreach (var p in prereqs)
                        if (!gunSightExempt && !sksUasSightExempt && !sksMtu017SightExempt) missing.Add(p);
                }
                else
                {
                    foreach (var p in prereqs)
                        if (!gunSightExempt && !sksUasSightExempt && !sksMtu017SightExempt && !holder.attachmentIds.Contains(p)) missing.Add(p);
                }
            }
        }
        // 前握把条件前提：仅 AKM 系需要改装护木（MOE/Hexagon）
        var foregripMissing = SuppressorSystem.GetForegripMissingPrereq(gun, attachmentId);
        if (foregripMissing != null)
            foreach (var p in foregripMissing)
                if (!missing.Contains(p)) missing.Add(p);
        // OR 前提组：组内任一已装即满足，否则列出全部未装项作为提示。
        // 仅对 AKM 系枪生效（战术设备/前握把需改装护木），
        // 非 AKM 枪（M4/沙鹰等）无护木前提，可直接安装。
        if (OrPrerequisiteGroups.TryGetValue(attachmentId, out var alts))
        {
            bool isAkm = gun != null
                && gun.id.IndexOf("akm", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isAkm)
            {
                // 非 AKM：忽略护木前提
            }
            else if (gun == null) { foreach (var a in alts) if (!missing.Contains(a)) missing.Add(a); }
            else
            {
                var holder = gun.GetComponent<GunAttachmentHolder>();
                if (holder == null) { foreach (var a in alts) if (!missing.Contains(a)) missing.Add(a); }
                else
                {
                    bool any = false;
                    foreach (var a in alts)
                        if (holder.attachmentIds.Contains(a)) { any = true; break; }
                    // 已装任一非 WASR 护木（如 B10M+B19）也算满足护木前提
                    if (!any && SuppressorSystem.HasAnyHandguardExceptWasr(gun)) any = true;
                    if (!any)
                        foreach (var a in alts)
                            if (!missing.Contains(a)) missing.Add(a);
                }
            }
        }
        return missing;
    }
}