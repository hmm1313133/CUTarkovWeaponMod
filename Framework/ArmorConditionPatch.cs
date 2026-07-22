using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 修复护甲耐久归零后仍提供减伤的原版行为。
///
/// 原版 Limb.GetArmorReduction() 遍历肢体穿戴物时只累加 wearableArmor，
/// 不检查 item.condition > 0。导致耐久归零但未销毁的护甲（destroyAtZeroCondition=false）
/// 仍提供完整减伤。
///
/// 修复：Postfix 中减去耐久归零的护甲的 wearableArmor 值。
/// </summary>
public static class ArmorConditionPatch
{
    [HarmonyPatch(typeof(Limb), nameof(Limb.GetArmorReduction))]
    public static class GetArmorReductionPostfix
    {
        [HarmonyPostfix]
        public static void Postfix(Limb __instance, ref float __result)
        {
            // Fast path: no armor reduction to correct
            if (__result <= 0f) return;

            var wearables = __instance.GetLimbWearables();
            foreach (var item in wearables)
            {
                if (item == null) continue;
                if (item.condition <= 0f && item.Stats.wearableArmor > 0f)
                {
                    __result -= item.Stats.wearableArmor;
                }
            }
        }
    }
}
