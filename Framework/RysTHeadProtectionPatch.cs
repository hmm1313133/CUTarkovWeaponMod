using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Rys-T 头盔头部保护效果：
/// - 45% 免疫下颚脱位 (Limb.Dislocate)
/// - 50% 免疫下颚缺失/毁容 (Body.Disfigure)
/// - 75% 免疫眼部失明 (Body.RemoveEye)
///
/// 大脑损伤由护甲值提供原版减免（brainHealth -= damage / armorReduction），无需额外 Patch。
/// </summary>
public static class RysTHeadProtectionPatch
{
    // ===== 下颚脱位免疫 =====

    [HarmonyPatch(typeof(Limb), nameof(Limb.Dislocate))]
    [HarmonyPrefix]
    public static bool DislocatePrefix(Limb __instance)
    {
        try
        {
            if (!__instance.isHead) return true;
            if (!__instance.body.HasWearable(RysTItemSystem.ItemKey)) return true;

            if (Random.value < 0.45f)
            {
                Plugin.Log.LogInfo("[RysT] Blocked jaw dislocation (45% immunity).");
                return false;
            }
        }
        catch { }
        return true;
    }

    // ===== 毁容免疫 =====

    [HarmonyPatch(typeof(Body), nameof(Body.Disfigure))]
    [HarmonyPrefix]
    public static bool DisfigurePrefix(Body __instance)
    {
        try
        {
            if (!__instance.HasWearable(RysTItemSystem.ItemKey)) return true;

            if (Random.value < 0.5f)
            {
                Plugin.Log.LogInfo("[RysT] Blocked jaw loss/disfigurement (50% immunity).");
                return false;
            }
        }
        catch { }
        return true;
    }

    // ===== 眼部失明免疫 =====

    [HarmonyPatch(typeof(Body), nameof(Body.RemoveEye))]
    [HarmonyPrefix]
    public static bool RemoveEyePrefix(Body __instance)
    {
        try
        {
            if (!__instance.HasWearable(RysTItemSystem.ItemKey)) return true;

            if (HeadInjuryProtectionHelper.TryBlockEyeRemoval(__instance, 0.75f))
            {
                Plugin.Log.LogInfo("[RysT] Blocked eye removal (75% immunity).");
                return false;
            }
        }
        catch { }
        return true;
    }
}
