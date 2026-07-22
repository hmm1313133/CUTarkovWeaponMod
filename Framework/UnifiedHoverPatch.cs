using System;
using HarmonyLib;

using CUTarkovMedicalMod.Framework;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 合并的物品悬停描述补丁 - 替代 83 个独立 Postfix 补丁。
/// 单次 HashSet 查找判断是否为自定义物品，然后统一处理 StripEffects。
/// </summary>
[HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
public static class UnifiedHoverPatch
{
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
    }
}

/// <summary>
/// 移除 Item.fullName 原生的耐久百分比后缀 " (XX%)"。
/// 游戏在 get_fullName 中始终追加 condition 百分比，此补丁将其剥离。
/// </summary>
[HarmonyPatch(typeof(Item), nameof(Item.fullName), MethodType.Getter)]
public static class FullNameConditionPatch
{
    public static void Postfix(ref string __result)
    {
        if (string.IsNullOrEmpty(__result)) return;
        int idx = __result.LastIndexOf(" (", StringComparison.Ordinal);
        if (idx < 0) return;
        var suffix = __result.Substring(idx);
        if (suffix.Contains("%") && suffix.Contains("</color>") && suffix.EndsWith(")"))
            __result = __result.Substring(0, idx);
    }
}
