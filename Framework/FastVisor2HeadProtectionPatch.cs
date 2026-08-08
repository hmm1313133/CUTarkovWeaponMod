using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Ops-Core FAST 多重打击防弹面罩头部保护效果：
/// - 40% 免疫眼部失明 (Body.RemoveEye)
/// - 25% 免疫下颚缺失/毁容 (Body.Disfigure)
/// </summary>
public static class FastVisor2HeadProtectionPatch
{
    // ===== 眼部失明免疫 =====

    [HarmonyPatch(typeof(Body), nameof(Body.RemoveEye))]
    [HarmonyPrefix]
    public static bool RemoveEyePrefix(Body __instance)
    {
        try
        {
            if (!__instance.HasWearable(FastVisor2ItemSystem.ItemKey)) return true;

            if (HeadInjuryProtectionHelper.TryBlockEyeRemoval(__instance, 0.4f))
            {
                Plugin.Log.LogInfo("[FastVisor2] Blocked eye removal (40% immunity).");
                return false;
            }
        }
        catch { }
        return true;
    }

    // ===== 下颚缺失/毁容免疫 =====

    [HarmonyPatch(typeof(Body), nameof(Body.Disfigure))]
    [HarmonyPrefix]
    public static bool DisfigurePrefix(Body __instance)
    {
        try
        {
            if (!__instance.HasWearable(FastVisor2ItemSystem.ItemKey)) return true;

            if (Random.value < 0.25f)
            {
                Plugin.Log.LogInfo("[FastVisor2] Blocked jaw loss/disfigurement (25% immunity).");
                return false;
            }
        }
        catch { }
        return true;
    }
}
