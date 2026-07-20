using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 在自定义枪械/近战/护甲/弹挂/弹挂甲的悬停名称后追加耐久度百分比，
/// 类似原版大型电池的名称后显示耐久度。
///
/// 以低优先级 Postfix 运行，确保在各物品自己的 HoverPatch 设置名称之后再追加。
/// </summary>
[HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
[HarmonyPriority(Priority.Low)]
public static class ConditionNamePatch
{
    /// <summary>需要显示耐久度的自定义物品 ID 集合（枪械/近战/防弹衣/弹挂/弹挂甲）</summary>
    private static readonly HashSet<string> ConditionDisplayIds = new(StringComparer.OrdinalIgnoreCase)
    {
        // 枪械
        MP133ItemSystem.ItemKey, MP153ItemSystem.ItemKey, SKSItemSystem.ItemKey,
        AXMCItemSystem.ItemKey, DVL10ItemSystem.ItemKey, AKMItemSystem.ItemKey,
        DeagleItemSystem.ItemKey, Glock17ItemSystem.ItemKey, M4A1ItemSystem.ItemKey,
        P90ItemSystem.ItemKey, UMP45ItemSystem.ItemKey, RPDItemSystem.ItemKey,
        USPItemSystem.ItemKey,
        // 近战武器
        RedRebelItemSystem.ItemKey, M2SwordItemSystem.ItemKey,
        // 防弹衣类
        PACAItemSystem.ItemKey, MFUNItemSystem.ItemKey, DRDItemSystem.ItemKey,
        ThorItemSystem.ItemKey, TrooperItemSystem.ItemKey, SixB13ItemSystem.ItemKey,
        HPCItemSystem.ItemKey, GzhelKItemSystem.ItemKey, RedutT5ItemSystem.ItemKey,
        SlickItemSystem.ItemKey, HGridItemSystem.ItemKey, SixB43ItemSystem.ItemKey,
        // 头盔
        RysTItemSystem.ItemKey,
        ExfilItemSystem.ItemKey,
        UlachItemSystem.ItemKey,
        B47ItemSystem.ItemKey,
        Ssh68ItemSystem.ItemKey,
        CalmanItemSystem.ItemKey,
        // 弹挂类
        IDEAItemSystem.ItemKey, BankRobberItemSystem.ItemKey, Type56ItemSystem.ItemKey,
        WTChestRigItemSystem.ItemKey, UmkaItemSystem.ItemKey, CommandoItemSystem.ItemKey,
        LBCRItemSystem.ItemKey, BlackRockItemSystem.ItemKey,
        // 弹挂甲类
        SixB516ItemSystem.ItemKey, MBSSItemSystem.ItemKey, TV115ItemSystem.ItemKey,
        SPPCV2ItemSystem.ItemKey, TV110ItemSystem.ItemKey, MK4AItemSystem.ItemKey,
        SixB45ItemSystem.ItemKey, SiegeRItemSystem.ItemKey, AVSTEItemSystem.ItemKey,
        TTSKItemSystem.ItemKey, LV119ItemSystem.ItemKey,
        // 背包
        LK3FItemSystem.ItemKey,
        ReadyPackItemSystem.ItemKey,
        PartizanItemSystem.ItemKey,
        DayPackItemSystem.ItemKey,
        BerkutItemSystem.ItemKey,
        ScavPackItemSystem.ItemKey,
        MysteryRanch2DayItemSystem.ItemKey,
        PilgrimItemSystem.ItemKey,
        SsoAttack2ItemSystem.ItemKey,
        SH118ItemSystem.ItemKey,
        LBT2670ItemSystem.ItemKey,
    };

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Low)]
    public static void Postfix(Item item, ref (string, string) __result)
    {
        try
        {
            if (item == null) return;
            if (string.IsNullOrEmpty(item.id)) return;
            if (!ConditionDisplayIds.Contains(item.id)) return;

            // 只在名称已被设置（非空、非 Unknown Object）时追加耐久度
            string name = __result.Item1;
            if (string.IsNullOrEmpty(name)) return;
            if (name.Contains("Unknown")) return;

            int percent = Mathf.RoundToInt(item.condition * 100f);
            __result.Item1 = $"{name} ({percent}%)";
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[ConditionName] Postfix failed for item '{item?.id}': {ex.Message}");
        }
    }
}
