using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using CUTarkovMedicalMod.Framework;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 合并的物品悬停描述补丁 - 替代 83 个独立 Postfix 补丁。
/// 单次 HashSet 查找判断是否为自定义物品，然后统一处理 StripEffects，
/// 并追加装在枪上的 LAS/TAC 2 战术手电实时剩余电量。
/// </summary>
[HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
public static class UnifiedHoverPatch
{
    // ===== 缓存（降低每帧开销）=====
    // 说明：ItemHoverDescription 由游戏在悬停时每帧调用（这是游戏自身行为，无法避免），
    // 我们的 Postfix 跟随执行。真正的开销是每帧 GetComponent 和字符串拼接，
    // 因此缓存组件引用（同 item 不重复 GetComponent）+ 电量字符串（百分比不变则不重建）。
    private static int _cachedItemId = int.MinValue;
    private static GunAttachmentHolder? _cachedHolder;
    private static GunScript? _cachedGun;

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Low)]
    public static void Postfix(Item item, ref (string, string) __result)
    {
        if (item == null || string.IsNullOrEmpty(item.id)) return;
        if (!WeaponItemRegistration.WeaponItemIds.Contains(item.id)) return;
        if (!item.Stats.rec.recognizable) return;

        // 名称已由 I18nRefreshPatch.Prefix 通过 I18n.Tr() 设置
        // 只需处理特效裁剪
        HoverDescriptionHelper.StripEffectsWhenNotExpanded(ref __result);

        // ===== 弹药盒：像弹匣一样显示剩余弹药 =====
        TryAppendAmmoBoxRounds(item, ref __result);

        // ===== 战术手电（LAS/TAC 2 / Klesch-2U）：枪上电量实时显示 =====
        // 缓存 GunScript + holder：同一 item 不重复 GetComponent（Unity 原生调用有开销）
        int id = item.GetInstanceID();
        GunScript? gun;
        GunAttachmentHolder? holder;
        if (id == _cachedItemId)
        {
            gun = _cachedGun;
            holder = _cachedHolder;
        }
        else
        {
            gun = item.GetComponent<GunScript>();
            holder = item.GetComponent<GunAttachmentHolder>();
            _cachedItemId = id;
            _cachedGun = gun;
            _cachedHolder = holder;
        }
        if (gun == null) return;

        // ===== Shift 展开面板：枪械属性（后坐力/瞄准速度/精度/噪音损伤）=====
        // 注意：此分支必须在 holder 判断之前，原厂枪械没有 GunAttachmentHolder 组件
        if (Input.GetKey(KeyBinds.GetBind("expanddesc")))
        {
            Plugin.Log.LogInfo($"[HoverPanel] expanddesc pressed for {item.id}");
            try
            {
                var panel = BuildGunStatsPanel(item);
                Plugin.Log.LogInfo($"[HoverPanel] panel='{panel}'");
                if (!string.IsNullOrEmpty(panel))
                    __result.Item2 += panel;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[HoverPanel] BuildGunStatsPanel failed: {ex.Message}");
            }
        }

        if (holder == null || holder.attachmentIds == null) return;

        // 叠加显示所有带电配件（可同时装手电 + 瞄具，各显示一行）
        var lines = new List<string>();
        AddBatteryLine(lines, holder.attachmentIds.Contains(LasTac2ItemSystem.ItemKey), holder.lasTacCharge, WModLoc.Tr("wm.hover.battery.lastac2", "LAS/TAC 2 战术手电"));
        AddBatteryLine(lines, holder.attachmentIds.Contains(Klesch2UItemSystem.ItemKey), holder.kleschCharge, WModLoc.Tr("wm.hover.battery.klesch2u", "Klesch-2U 战术手电"));
        AddBatteryLine(lines, holder.attachmentIds.Contains(BaldrProItemSystem.ItemKey), holder.baldrCharge, WModLoc.Tr("wm.hover.battery.baldrpro", "Baldr Pro 战术手电"));
        AddBatteryLine(lines, holder.attachmentIds.Contains(TblItemSystem.ItemKey), holder.tblCharge, WModLoc.Tr("wm.hover.battery.tbl", "TBL 蓝色激光"));
        if (lines.Count == 0) return;
        __result.Item2 += "\n\n" + string.Join("\n", lines);
    }

    /// <summary>
    /// 旧版弹药盒（itemType=Round）兼容：像弹匣一样显示剩余弹药量。
    /// 新版弹药盒 itemType=Magazine，原版 ItemHoverDescription 会直接显示。
    /// </summary>
    private static void TryAppendAmmoBoxRounds(Item item, ref (string, string) __result)
    {
        var ammo = item.GetComponent<AmmoScript>();
        if (ammo == null) return;
        if (ammo.itemType != AmmoScript.AmmoItemType.Round) return;
        if (ammo.maxRounds <= 1) return;
        __result.Item2 += $"<color=#ff8fb0><sprite index=8 tint=1>{ammo.rounds}/{ammo.maxRounds} {Locale.GetOther("magazinerounds")}</color>\n";
    }

    /// <summary>
    /// 构建枪械属性面板（Shift 展开时显示）。
    /// 显示后坐力、瞄准速度、精度、噪音损伤（每发），配装后数值 + 红绿对比。
    /// </summary>
    private static string BuildGunStatsPanel(Item gunItem)
    {
        var gun = gunItem.GetComponent<GunScript>();
        if (gun == null) return "";

        // 原厂值（运行时 gun 未开火时是原厂值）
        float baseKnockBack = gun.knockBack;
        float baseSpread = gun.verticalSpread;
        float baseLoudness = gun.loudness;
        float hipSpread = gun.verticalSpread * AimSystem.GetUnaimedSpreadMult(gunItem);

        // 配装后倍率
        var (kbMult, spMult, ldMult, _) = SuppressorSystem.GetEffectiveStats(gunItem);

        // 配装后数值
        float effKnockBack = baseKnockBack * kbMult;
        float effSpread = baseSpread * spMult;
        float effLoudness = baseLoudness * ldMult;
        float aimTime = AimSystem.GetAimTime(gunItem);

        // 红绿对比（数值相比原厂：更高红、更低绿）
        // 后坐力/噪音：越低越好 → 降低=绿，升高=红
        // 精度（spread）：越低越好 → 降低=绿，升高=红
        // 瞄准速度：越低越好 → 降低=绿，升高=红
        string kbColor = effKnockBack < baseKnockBack ? "#90ee90" : (effKnockBack > baseKnockBack ? "#ff6b6b" : "#ffffff");
        string spColor = effSpread < baseSpread ? "#90ee90" : (effSpread > baseSpread ? "#ff6b6b" : "#ffffff");
        string ldColor = effLoudness < baseLoudness ? "#90ee90" : (effLoudness > baseLoudness ? "#ff6b6b" : "#ffffff");

        // 瞄准速度：配装后（配件影响瞄准时间）
        float baseAimTime = AimSystem.GetBaseAimTime(gunItem.id);
        string aimColor = aimTime < baseAimTime ? "#90ee90" : (aimTime > baseAimTime ? "#ff6b6b" : "#ffffff");

        // 相比原厂的增减值（配装后 - 原厂）
        float dKb = effKnockBack - baseKnockBack;
        float dSp = effSpread - baseSpread;
        float dLd = effLoudness - baseLoudness;
        float dAim = aimTime - baseAimTime;

        // 增减值文本（+/- 前缀，保留 2 位小数；接近 0 不显示）
        string DeltaText(float d, string fmt)
        {
            if (Math.Abs(d) < 0.005f) return "";
            return d > 0 ? $"(+{d.ToString(fmt)})" : $"({d.ToString(fmt)})";
        }

        return "\n<color=#4fc3f7>" + WModLoc.Tr("wm.hover.gun_stats_title", "—— 枪械属性 ——") + "</color>\n" +
               WModLoc.Tr("wm.hover.stat_recoil", "后坐力：") + $"<color={kbColor}>{effKnockBack:0.##}</color><color={kbColor}>{DeltaText(dKb, "0.##")}</color>\n" +
               WModLoc.Tr("wm.hover.stat_aim_time", "瞄准时间：") + $"<color={aimColor}>{aimTime:0.##}s</color><color={aimColor}>{DeltaText(dAim, "0.##")}</color>\n" +
               WModLoc.Tr("wm.hover.stat_spread", "精度：") + $"<color={spColor}>{effSpread:0.###}</color><color={spColor}>{DeltaText(dSp, "0.###")}</color>\n" +
               WModLoc.Tr("wm.hover.stat_hipfire", "腰射精度：") + $"<color=#ffffff>{hipSpread:0.###}</color>\n" +
               WModLoc.Tr("wm.hover.stat_loudness", "噪音损伤/发：") + $"<color={ldColor}>{effLoudness:0.#}</color><color={ldColor}>{DeltaText(dLd, "0.#")}</color>";
    }

    /// <summary>生成一行电量描述（配件未安装则跳过）。</summary>
    private static void AddBatteryLine(List<string> lines, bool installed, float charge, string name)
    {
        if (!installed) return;
        int pct = Mathf.Clamp(Mathf.RoundToInt(charge * 100f), 0, 100);
        string status = pct <= 0
            ? "<color=#ff4d4d>" + WModLoc.Tr("wm.hover.battery_empty", "电量耗尽") + "</color>"
            : WModLoc.Tr("wm.hover.battery_left", "剩余电量：") + $"<color=#4fc3f7>{pct}%</color>";
        lines.Add("<color=#ffcc4d>" + string.Format(WModLoc.Tr("wm.hover.battery_installed", "装有 {0}（{1}）"), name, status) + "</color>");
    }
}
